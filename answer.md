# Trả lời câu hỏi về Load Testing & Monitoring

## 1. Server specs này phục vụ được bao nhiêu concurrent user?

### Phân tích cấu hình

| Thành phần | Giá trị | Ghi chú |
|---|---|---|
| CPU | 2 cores | Bottleneck chính |
| RAM | 16 GB | SQL Server cần ~4–6 GB tối thiểu |
| Disk | 80 GB | Đủ cho dev/staging |
| Workload | API + SQL Server cùng máy | Chia sẻ tài nguyên |

### Ước lượng thực tế

| Kịch bản | Concurrent Users | Lý do |
|---|---|---|
| Read-only API (GET) nhẹ | ~1.000–2.000 | Kestrel xử lý I/O async tốt |
| Auth flow (login + token) | ~200–500 | Mỗi request cần DB query + JWT signing |
| Mixed read/write | ~300–800 | SQL Server là bottleneck |
| 1 triệu concurrent | **Không khả thi** trên cấu hình này | Cần scale-out |

### Kết luận

> **Thực tế:** Cấu hình 2 core + SQL Server chung máy chỉ phục vụ được khoảng **300–1.000 concurrent users** ổn định tùy workload.
>
> Để đạt 1 triệu concurrent users cần: horizontal scaling (nhiều instance API), database cluster (SQL Server Always On hoặc chuyển sang PostgreSQL), load balancer (Nginx/HAProxy), và caching layer (Redis).

---

## 2. Có nên tạo thêm table Product, Category, Order không?

**Có, nên tạo.** Đây là lý do:

- **Kịch bản thực tế hơn**: Auth chỉ là một phần nhỏ. Bài toán thực là sau khi login, user sẽ đọc sản phẩm, tạo đơn hàng — đây mới là điểm gây tải thật sự.
- **Mix read/write**: Order tạo write contention trên DB, dễ phát hiện deadlock, slow query.
- **Dữ liệu seed đủ lớn**: Tạo 100k+ sản phẩm, 1M+ orders để query thực sự phải dùng index.

### Schema gợi ý tối thiểu

```sql
-- Categories
CREATE TABLE Categories (Id INT PRIMARY KEY IDENTITY, Name NVARCHAR(100), CreatedAt DATETIME2);

-- Products  
CREATE TABLE Products (Id INT PRIMARY KEY IDENTITY, CategoryId INT FK, Name NVARCHAR(200), Price DECIMAL(18,2), Stock INT, CreatedAt DATETIME2);

-- Orders
CREATE TABLE Orders (Id BIGINT PRIMARY KEY IDENTITY, UserId NVARCHAR(450), Status TINYINT, TotalAmount DECIMAL(18,2), CreatedAt DATETIME2);

-- OrderItems
CREATE TABLE OrderItems (Id BIGINT PRIMARY KEY IDENTITY, OrderId BIGINT FK, ProductId INT FK, Quantity INT, Price DECIMAL(18,2));
```

---

## 3. Giả lập hàng triệu user login + thao tác đồng thời

### Công cụ khuyên dùng: **k6** (Grafana k6)

k6 là lựa chọn tốt nhất cho OAuth 2.0 flow vì:
- Script bằng JavaScript, dễ viết flow phức tạp
- Hỗ trợ HTTP/2, WebSocket
- Tích hợp native với Prometheus + Grafana
- Chạy được trên Docker

### Cài đặt k6

```bash
# Chạy k6 trong Docker
docker run --rm -i grafana/k6 run - <script.js
```

### Script k6 mẫu: Login → Lấy token → Gọi API

Tạo file `load-test.js`:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';

// Cấu hình load test
export const options = {
  stages: [
    { duration: '2m', target: 100 },   // Ramp up lên 100 users trong 2 phút
    { duration: '5m', target: 500 },   // Giữ 500 users trong 5 phút
    { duration: '2m', target: 1000 },  // Ramp up lên 1000 users
    { duration: '5m', target: 1000 },  // Stress test ở 1000 concurrent
    { duration: '2m', target: 0 },     // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% request dưới 2 giây
    http_req_failed: ['rate<0.01'],    // Tỷ lệ lỗi dưới 1%
  },
};

const BASE_URL = 'http://your-api-host:5000';

// Hàm login và lấy access token
function getAccessToken(username, password) {
  const res = http.post(`${BASE_URL}/connect/token`, {
    grant_type: 'password',
    client_id: 'your-client-id',
    client_secret: 'your-client-secret',
    username: username,
    password: password,
    scope: 'openid profile api',
  }, {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  });

  check(res, { 'login success': (r) => r.status === 200 });
  return res.json('access_token');
}

export default function () {
  // Mỗi VU (virtual user) sẽ chạy flow này
  const token = getAccessToken(`user${__VU}@test.com`, 'Password123!');
  
  if (!token) return;

  const headers = { Authorization: `Bearer ${token}` };

  // Gọi API lấy danh sách sản phẩm
  const productsRes = http.get(`${BASE_URL}/api/products`, { headers });
  check(productsRes, { 'get products 200': (r) => r.status === 200 });

  sleep(1);

  // Tạo đơn hàng
  const orderRes = http.post(`${BASE_URL}/api/orders`, JSON.stringify({
    productId: Math.floor(Math.random() * 1000) + 1,
    quantity: 1,
  }), {
    headers: { ...headers, 'Content-Type': 'application/json' },
  });
  check(orderRes, { 'create order 200 or 201': (r) => r.status === 200 || r.status === 201 });

  sleep(Math.random() * 3 + 1); // Nghỉ 1–4 giây giữa các request
}
```

### Chạy test với Docker Compose

```yaml
# docker-compose.loadtest.yml
version: '3.8'
services:
  k6:
    image: grafana/k6
    volumes:
      - ./load-test.js:/scripts/load-test.js
    command: run /scripts/load-test.js
    environment:
      - K6_OUT=influxdb=http://influxdb:8086/k6
    depends_on:
      - influxdb

  influxdb:
    image: influxdb:1.8
    environment:
      - INFLUXDB_DB=k6

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    depends_on:
      - influxdb
```

### Seed dữ liệu test users

Cần tạo sẵn user trong DB trước khi chạy k6:

```bash
# Script seed 10,000 users
dotnet run --project YourProject seed-users --count 10000
```

Hoặc dùng SQL trực tiếp với OpenIddict user table.

---

## 4. Monitoring hệ thống

### Stack monitoring khuyên dùng

```
[.NET API] → Prometheus metrics
[SQL Server] → SQL Exporter
[Docker] → cAdvisor
     ↓
[Prometheus] → scrape metrics
     ↓
[Grafana] → visualize dashboard
```

### Bước 1: Thêm Prometheus metrics vào .NET 8 API

```bash
dotnet add package prometheus-net.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddHealthChecks();

app.UseRouting();
app.UseHttpMetrics(); // Tự động track HTTP metrics
app.MapMetrics();     // Expose /metrics endpoint
app.MapHealthChecks("/health");
```

### Bước 2: Docker Compose monitoring stack

```yaml
# docker-compose.monitoring.yml
version: '3.8'
services:

  api:
    image: your-api:latest
    ports:
      - "5000:8080"

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana

  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    ports:
      - "8080:8080"
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker/:/var/lib/docker:ro

volumes:
  grafana-data:
```

### Bước 3: prometheus.yml

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'dotnet-api'
    static_configs:
      - targets: ['api:8080']
    metrics_path: '/metrics'

  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']

  - job_name: 'k6'
    static_configs:
      - targets: ['k6:5665']
```

### Bước 4: Metrics cần theo dõi trong Grafana

| Metric | Ý nghĩa | Alert khi |
|---|---|---|
| `http_request_duration_seconds` | Latency của API | p95 > 2s |
| `http_requests_total` | Throughput (req/s) | Drop đột ngột |
| `process_cpu_seconds_total` | CPU usage | > 80% |
| `dotnet_gc_collections_total` | GC pressure | Tăng liên tục |
| `container_memory_usage_bytes` | RAM dùng | > 14GB |
| `sqlserver_connections_current` | DB connections | > 100 |

### Bước 5: Xem kết quả k6 real-time

Sau khi k6 chạy xong sẽ in ra:

```
✓ login success............: 99.8% 
✓ get products 200.........: 98.2%
✓ create order 200 or 201..: 97.1%

http_req_duration: avg=234ms  min=12ms  med=189ms  max=8.2s  p(90)=512ms  p(95)=1.2s
http_reqs........: 125000  420/s
vus..............: 1000 (max)
```

---

## Tóm tắt roadmap thực hành

```
Bước 1: Tạo thêm table Product/Category/Order + seed data lớn
   ↓
Bước 2: Cài Docker Compose monitoring (Prometheus + Grafana)
   ↓
Bước 3: Chạy k6 với 100 users → quan sát metrics
   ↓
Bước 4: Tăng dần lên 500 → 1000 → ghi nhận điểm nghẽn cổ chai
   ↓
Bước 5: Tối ưu (thêm index, cache Redis, connection pooling)
   ↓
Bước 6: Lặp lại Bước 3–5
```

> **Lưu ý:** Với cấu hình 2 core hiện tại, mục tiêu thực tế để học là đạt **1.000–2.000 concurrent users ổn định**, sau đó hiểu rõ tại sao hệ thống fail khi vượt ngưỡng đó. Đây là kinh nghiệm quý giá hơn là cố đạt 1 triệu trên single server.

---

---

# PHẦN 2 — Thực hành: Chạy và Đọc Monitoring (Step by Step)

> Tất cả code và cấu hình đã có sẵn. Phần này hướng dẫn **cách chạy**, **cách dùng từng tool**, và **cách đọc kết quả**.

---

## Kiến trúc tổng quan — Các thành phần liên kết với nhau như thế nào

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          MÁY CỦA BẠN (host)                            │
│                                                                         │
│   ┌─────────────────┐          ┌──────────────────────────────────┐    │
│   │  dotnet run     │          │         Docker Compose           │    │
│   │  AuthDemo.Api   │◄─scrape──│  Prometheus (9090)               │    │
│   │  :5000          │          │  Grafana    (3000) ◄─── bạn xem  │    │
│   │  GET /metrics   │          │  InfluxDB   (8086) ◄─── k6 ghi   │    │
│   │  GET /health    │          │  cAdvisor   (8080) ← monitor Docker│   │
│   └─────────────────┘          └──────────────────────────────────┘    │
│                                                                         │
│   ┌─────────────────┐                                                   │
│   │  k6 run         │──────────► POST /connect/token  (login)           │
│   │  (load test)    │──────────► GET  /api/products   (đọc)             │
│   │                 │──────────► POST /api/orders      (ghi)            │
│   │                 │──ghi──────► InfluxDB :8086                        │
│   └─────────────────┘                                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

**Vai trò từng thành phần:**
- **k6**: giả lập hàng trăm/nghìn user thật — login, đọc sản phẩm, tạo đơn hàng
- **Prometheus**: cứ 5 giây lại "hỏi" API endpoint `/metrics` để lấy số liệu về CPU, request count, latency
- **InfluxDB**: k6 đẩy kết quả test vào đây theo thời gian thực (time-series database)
- **Grafana**: đọc cả Prometheus lẫn InfluxDB rồi vẽ đồ thị — bạn nhìn vào đây để hiểu hệ thống đang làm gì
- **cAdvisor**: theo dõi Docker container (CPU/RAM của từng container, không phải của API)

---

## Bước 1 — Khởi động Monitoring Stack

### 1.1 Chạy lệnh

```bash
# Đứng ở thư mục gốc project (e:\TECHLEAD_PROJECT\OpenIdDict)
cd docker
docker compose -f docker-compose.monitoring.yml up -d
```

> **`-d`** = detached mode, chạy ngầm, không chiếm terminal.

### 1.2 Kiểm tra tất cả container đang sống

```bash
docker compose -f docker-compose.monitoring.yml ps
```

Kết quả phải thấy:
```
NAME         STATUS
prometheus   running
grafana      running
influxdb     running
cadvisor     running
```

Nếu có container `Exited` → xem log để debug:
```bash
docker logs prometheus   # thay tên container tương ứng
```

### 1.3 Verify từng service

| Service | URL | Kỳ vọng |
|---|---|---|
| Prometheus | `http://localhost:9090` | Trang web UI hiện lên |
| Grafana | `http://localhost:3000` | Login page (admin / admin123) |
| InfluxDB | `http://localhost:8086/ping` | Trả về HTTP 204 (không có body) |
| cAdvisor | `http://localhost:8080` | Dashboard container metrics |

---

## Bước 2 — Khởi động API và Verify

### 2.1 Chạy API

```bash
# Mở terminal mới, đứng ở thư mục gốc
cd AuthDemo.Api
dotnet run
```

Chờ thấy 2 dòng log quan trọng:
```
[INF] Đã tạo 100 test users cho k6 load testing   ← WorkerService seed xong
[INF] Application started. Press Ctrl+C to shut down.
```

> WorkerService tự động seed 100 user vào DB khi app khởi động. Không cần chạy script SQL thủ công.

### 2.2 Verify 3 endpoint cần thiết

```bash
# 1. Health check — k6 kiểm tra trước khi test
curl http://localhost:5000/health
# Kỳ vọng: {"status":"Healthy"}

# 2. Metrics endpoint — Prometheus scrape endpoint này
curl http://localhost:5000/metrics
# Kỳ vọng: thấy hàng trăm dòng bắt đầu bằng "# HELP" và số liệu

# 3. Thử login với 1 test user (xác nhận seed thành công)
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile email roles"
# Kỳ vọng: {"access_token":"eyJ...","token_type":"Bearer","expires_in":3600}
```

### 2.3 Verify Prometheus đang scrape API

1. Mở `http://localhost:9090`
2. Vào **Status → Targets**
3. Target `dotnet-api` phải có `State = UP` màu xanh

> Nếu `DOWN`: API chưa chạy, hoặc port sai. Prometheus dùng `host.docker.internal:5000` để trỏ vào API trên host machine.

---

## Bước 3 — Cấu hình Grafana (làm 1 lần duy nhất)

### 3.1 Thêm datasource InfluxDB (cho k6)

> InfluxDB lưu kết quả k6 — cần add để Grafana đọc được.

1. Mở `http://localhost:3000`, login `admin / admin123`
2. Sidebar trái → **Connections → Data Sources → Add new data source**
3. Chọn **InfluxDB**
4. Điền:
   ```
   URL:      http://influxdb:8086    ← dùng tên service Docker, không dùng localhost
   Database: k6
   ```
5. Bấm **Save & Test** → phải hiện "datasource is working"

### 3.2 Thêm datasource Prometheus (cho .NET API metrics)

1. **Add new data source → Prometheus**
2. URL: `http://prometheus:9090`
3. **Save & Test**

### 3.3 Import 3 dashboard có sẵn

Vào **Dashboards → Import** và import lần lượt:

| Dashboard ID | Tên | Datasource | Dùng để xem |
|---|---|---|---|
| `2587` | k6 Load Testing Results | InfluxDB | Kết quả k6 real-time |
| `10915` | ASP.NET Core & Controllers | Prometheus | .NET API metrics |
| `893` | Docker and OS metrics | Prometheus | CPU/RAM container |

> Cách import: nhập ID → **Load** → chọn datasource tương ứng → **Import**

---

## Bước 4 — Chạy k6 từng giai đoạn

> Luôn chạy theo thứ tự: **Smoke → Load → Stress → Spike**. Không nhảy thẳng lên Stress.

### 4.1 Smoke Test — 2 phút, 2 VU (luôn chạy đầu tiên)

**Mục đích:** Xác nhận script không lỗi, flow login → đọc sản phẩm → tạo đơn hàng hoạt động đúng. Không đánh giá performance.

```bash
# Từ thư mục gốc project
k6 run k6/smoke-test.js
```

Nếu không cài k6 local, dùng Docker:
```bash
docker run --rm -i \
  -v ${PWD}/k6:/scripts \
  --add-host=host.docker.internal:host-gateway \
  grafana/k6 run \
    --env BASE_URL=http://host.docker.internal:5000 \
    /scripts/smoke-test.js
```

> `--add-host=host.docker.internal:host-gateway` = cho phép container k6 trỏ vào API trên host machine (Windows/Linux). Trên Mac thì `host.docker.internal` tự động có sẵn.

**Smoke test PASS khi:**
```
✓ [smoke] login 200        : 100%
✓ [smoke] has access_token : 100%
✓ [smoke] products 200     : 100%
✓ [smoke] order not 500    : 100%

http_req_failed: 0.00%   ← Phải là 0
```

Nếu có lỗi ở smoke test → **dừng lại, sửa trước** — đừng chạy load test.

---

### 4.2 Load Test — Tải bình thường (~13 phút)

**Mục đích:** Đo performance ở tải bình thường (lên tới 1000 VU). Xem hệ thống chịu được không.

```bash
# Gửi kết quả vào InfluxDB để xem real-time trên Grafana
k6 run \
  --out influxdb=http://localhost:8086/k6 \
  --env BASE_URL=http://localhost:5000 \
  k6/load-test.js
```

Docker:
```bash
docker run --rm -i \
  -v ${PWD}/k6:/scripts \
  --network host \
  grafana/k6 run \
    --out influxdb=http://localhost:8086/k6 \
    --env BASE_URL=http://localhost:5000 \
    /scripts/load-test.js
```

> `--network host` = container dùng thẳng network của host → có thể kết nối tới InfluxDB và API qua localhost. Chỉ dùng được trên Linux; trên Windows/Mac dùng `host.docker.internal`.

Trong lúc k6 chạy, **mở Grafana dashboard ID 2587** để xem real-time.

---

### 4.3 Stress Test — Tìm giới hạn (manual)

**Mục đích:** Tăng VU vượt ngưỡng bình thường để tìm điểm hệ thống bắt đầu fail.

Mở [k6/load-test.js](k6/load-test.js) và tạm thời đổi `stages` thành:

```javascript
// Stress test — thêm giai đoạn 2000 VU
export const options = {
  stages: [
    { duration: '2m', target: 200  },
    { duration: '5m', target: 500  },
    { duration: '5m', target: 1000 },
    { duration: '5m', target: 2000 },  // ← Mức này sẽ thấy hệ thống bắt đầu fail
    { duration: '3m', target: 0    },
  ],
};
```

> Với 2 core + SQL Server chung máy, hệ thống thường fail ở khoảng 500–1000 VU. Đây là kiến thức thực tế quan trọng.

---

### 4.4 Spike Test — Flash sale simulation (manual)

**Mục đích:** Giả lập đột ngột tăng tải (Flash sale, event) — xem hệ thống có recover được không sau khi tải drop xuống.

```javascript
export const options = {
  stages: [
    { duration: '30s', target: 10   },  // Tải thấp bình thường
    { duration: '10s', target: 1000 },  // ← Spike: 10 giây từ 10 → 1000 VU
    { duration: '3m',  target: 1000 },  // Giữ tải cao
    { duration: '10s', target: 10   },  // Drop về bình thường
    { duration: '30s', target: 0    },
  ],
};
```

Sau khi tải drop về 10 VU, nếu latency về mức bình thường → hệ thống **self-recover được**.
Nếu latency vẫn cao → hệ thống đang bị treo (connection pool bị cạn, GC đang chạy, v.v.).

---

## Bước 5 — Đọc Output k6 Terminal

Trong lúc k6 chạy, terminal hiển thị progress:

```
running (05m30.0s), 500/1000 VUs, 12450 complete iterations
default ↓ [===============>------] 500/1000 VUs  05m30.0s/13m00.0s
          ↑ số VU hiện tại         ↑ tiến trình
```

Sau khi xong, k6 in summary:

```
     ✓ login 200.............: 99.82% ✓ 24560  ✗ 44
     ✓ products 200..........: 98.41% ✓ 24173  ✗ 387
     ✓ create order 201......: 94.20% ✓ 11610  ✗ 714   ← Nhiều fail → đây là bottleneck
     ✓ create order not 500..: 97.30% ✓ 11980  ✗ 324

     http_req_duration..........: avg=1.23s  p(95)=4.1s
     http_req_failed............: 3.20%
     http_req_waiting...........: avg=1.08s  p(95)=3.9s  ← Thời gian server xử lý
     http_req_blocked...........: avg=1.2ms  p(95)=7µs   ← Thời gian chờ TCP connect
     http_reqs..................: 61603  78.98/s          ← Throughput
     vus........................: 1000   max=1000
     
     login_duration.............: avg=420ms   p(95)=1.2s
     product_list_duration......: avg=180ms   p(95)=450ms
     create_order_duration......: avg=2.1s    p(95)=5.8s  ← Bottleneck rõ ràng
```

### Đọc từng metric

| Metric | Đọc thế nào | Ngưỡng quan tâm |
|---|---|---|
| `http_req_duration` p95 | 95% request hoàn thành trong bao lâu — **dùng cái này để đánh giá** | < 2s tốt, > 5s xấu |
| `http_req_waiting` | Thời gian server xử lý (bỏ qua network) — tìm bottleneck server | Nếu cao → server chậm |
| `http_req_blocked` | Chờ TCP connection — nếu cao là connection pool bị cạn | > 100ms → vấn đề |
| `http_req_failed` | Tỷ lệ lỗi 4xx/5xx/timeout | < 1% tốt, > 5% xấu |
| `create_order_duration` | Custom metric — riêng cho endpoint tạo đơn hàng | Thường cao nhất vì có DB write |
| `vus` | Số VU thực tế đang chạy | Nếu < target → VU bị timeout/treo |

### Hiểu Percentile (tại sao không dùng avg)

```
avg=1.23s   ← Bị kéo lên bởi vài request rất chậm (outlier) — không đại diện
p(50)=890ms ← Một nửa user thấy tốc độ này — median, đáng tin hơn avg
p(95)=4.1s  ← 95% user thấy dưới 4.1s, 5% thấy chậm hơn — DÙNG CÁI NÀY
p(99)=12s   ← 1% user chậm nhất (thường do GC pause hoặc cold start)
max=28.3s   ← 1 request chậm nhất — đừng lo nếu chỉ xuất hiện 1–2 lần
```

### Đọc PASS/FAIL của threshold

```
✓ http_req_duration: p(95)=1.8s    → PASS (threshold p95<3000ms)
✗ http_req_failed:   3.20%         → FAIL (threshold rate<0.05)
```

k6 trả về **exit code 99** nếu có threshold FAIL → CI pipeline sẽ fail theo.

---

## Bước 6 — Đọc Grafana Dashboard

### Dashboard k6 (ID 2587) — Xem trong lúc test chạy

Mở `http://localhost:3000`, vào dashboard **k6 Load Testing Results**.

```
┌─────────────────────────────────────────────────────────┐
│  Virtual Users          │  Request Rate (req/s)          │
│  [tăng theo stages]     │  [số request API nhận/giây]    │
├─────────────────────────────────────────────────────────┤
│  Response Time (ms)                                     │
│  ── p50 (xanh) ── p90 (vàng) ── p95 (cam) ── p99 (đỏ) │
│                                                         │
│  Lý tưởng: đường thẳng ngang khi VU tăng               │
│  Nguy hiểm: đường đi lên tỷ lệ thuận với VU            │
├─────────────────────────────────────────────────────────┤
│  Error Rate (%)         │  Check Pass Rate (%)           │
│  < 1% = bình thường     │  > 99% = tốt                  │
└─────────────────────────────────────────────────────────┘
```

**Cách đọc Response Time graph — tìm điểm bão hòa:**

```
ms
4000 |                                   ╭─────
3000 |                          ╭────────╯       ← p95 vượt 3s ở ~800 VU
2000 |               ╭──────────╯                  → đây là điểm hệ thống bão hòa
1000 |───────────────╯
   0 └───────────────────────────────────────────→ số VU
      50  100  200  300  500  800  1000
```

Điểm mà p95 bắt đầu tăng mạnh = ngưỡng hệ thống chịu được. Với 2 core thường ở khoảng 300–800 VU tùy workload.

---

### Dashboard .NET API (ID 10915) — Xem sức khoẻ API

| Panel | Đọc thế nào | Dấu hiệu xấu |
|---|---|---|
| **HTTP Request Rate** | Request/giây API đang xử lý | Thấp hơn k6 gửi → API đang queue |
| **HTTP Request Duration** | Latency phân theo từng route | Route nào bar cao nhất → đó là bottleneck |
| **Active Requests** | Số request đang chờ xử lý | > 50 → Kestrel thread pool đang đầy |
| **GC Collections Gen0/1/2** | Tần suất Garbage Collection | Gen2 tăng liên tục → có memory leak |
| **Heap Size** | RAM .NET process đang dùng | Tăng không ngừng → memory leak |
| **Thread Pool Queue** | Task đang chờ thread | > 0 → CPU là bottleneck |
| **Exception Rate** | Lỗi unhandled/giây | > 0 → bug hoặc hệ thống quá tải |

**Diễn giải bottleneck:**

```
Thread Pool Queue > 0  +  CPU ~100%
→ CPU là cổ chai — cần scale out hoặc tối ưu code

HTTP Duration cao (>2s)  +  CPU .NET thấp (<30%)
→ App đang ngồi chờ SQL Server — cần tối ưu query hoặc thêm index

GC Gen2 tăng liên tục  +  Heap Size không giảm
→ Memory leak — cần dùng dotnet-dump hoặc dotMemory để điều tra
```

---

### Dashboard cAdvisor (ID 893) — CPU/RAM của Docker containers

Xem panel **CPU Usage** và **Memory Usage** của các container:

```bash
# Xem nhanh không cần Grafana
docker stats --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"

# Output mẫu khi đang load test:
# NAME         CPU %     MEM USAGE / LIMIT
# prometheus   2.1%      512MiB / 16GiB
# grafana      1.3%      256MiB / 16GiB
# influxdb     8.4%      1.2GiB / 16GiB
# cadvisor     3.2%      128MiB / 16GiB
```

> cAdvisor chỉ theo dõi các container trong Docker. API đang chạy trên host (`dotnet run`) nên không thấy ở đây — xem qua Grafana dashboard .NET (ID 10915) hoặc `process_cpu_seconds_total` trong Prometheus.

---

## Bước 7 — Theo dõi SQL Server trong lúc test

Mở **SSMS** hoặc **Azure Data Studio**, kết nối vào SQL Server, chạy các query sau **trong lúc k6 đang test** để xem SQL đang "chịu đựng" như thế nào.

### Query 1: Top query chậm đang chạy

```sql
-- Chạy lại nhiều lần để xem query nào liên tục xuất hiện
SELECT TOP 10
    r.session_id,
    r.status,
    r.wait_type,                                  -- Đang chờ gì?
    r.wait_time / 1000.0       AS wait_sec,       -- Chờ bao lâu rồi
    r.total_elapsed_time / 1000.0 AS elapsed_sec, -- Tổng thời gian từ khi bắt đầu
    r.logical_reads,                               -- Số page đọc từ buffer — cao = thiếu index
    SUBSTRING(t.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS query_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50         -- Bỏ qua system sessions
ORDER BY r.total_elapsed_time DESC;
```

### Query 2: Số connection đang mở

```sql
-- Bình thường: 10–30 | Nguy hiểm: > 100
SELECT DB_NAME(dbid) AS db, COUNT(*) AS connections
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid
ORDER BY connections DESC;
```

> Nếu connection > 100 → connection pool của .NET đang bị rò rỉ hoặc `Max Pool Size` quá nhỏ.

### Query 3: Wait statistics — SQL đang chờ gì nhiều nhất

```sql
-- Chạy khi thấy latency cao nhưng không biết tại sao
SELECT TOP 10
    wait_type,
    wait_time_ms / 1000.0 AS wait_sec,
    waiting_tasks_count
FROM sys.dm_os_wait_stats
WHERE wait_type NOT IN (
    'SLEEP_TASK','LAZYWRITER_SLEEP','SQLTRACE_BUFFER_FLUSH',
    'WAITFOR','XE_DISPATCHER_WAIT','XE_TIMER_EVENT',
    'REQUEST_FOR_DEADLOCK_SEARCH','RESOURCE_QUEUE'
)
ORDER BY wait_time_ms DESC;
```

**Đọc kết quả:**

| Wait Type | Nghĩa là | Giải pháp |
|---|---|---|
| `LCK_M_X` | Row/table lock — nhiều transaction tranh nhau ghi cùng 1 row | Tối ưu transaction, dùng optimistic lock |
| `PAGEIOLATCH_SH` | Đọc từ disk (buffer cache miss) — thiếu RAM hoặc thiếu index | Thêm index, tăng RAM |
| `ASYNC_NETWORK_IO` | SQL ghi nhanh hơn app đọc — thường do N+1 query | Dùng `.Include()`, batch query |
| `CXPACKET` | Parallel query chiếm nhiều CPU | Giới hạn `MAXDOP` hoặc tối ưu query |

---

## Bước 8 — Quy trình phân tích khi thấy vấn đề

```
k6 báo lỗi hoặc latency cao
         │
         ▼
┌────────────────────────────┐
│ Error rate > 5%?           │──Có──► Xem log API: dotnet run terminal
│                            │         Tìm exception, 500 error
└────────────────────────────┘
         │ Không
         ▼
┌────────────────────────────┐
│ p95 > 3s?                  │──Có──► Xem Grafana ID 10915:
│                            │         Route nào chậm nhất?
└────────────────────────────┘
         │ Không
         ▼
┌────────────────────────────┐
│ CPU .NET > 90%?            │──Có──► CPU bottleneck
│ (Thread Pool Queue > 0)    │         → Scale out hoặc tối ưu code
└────────────────────────────┘
         │ Không
         ▼
┌────────────────────────────┐
│ CPU thấp mà latency cao?   │──Có──► SQL bottleneck
│                            │         → Chạy Query 1 & 3 trong SSMS
└────────────────────────────┘
         │ Không
         ▼
┌────────────────────────────┐
│ GC Gen2 tăng liên tục?     │──Có──► Memory leak
│ Heap không giảm?           │         → Cần profiler (dotnet-dump)
└────────────────────────────┘
```

### Các tối ưu theo thứ tự ưu tiên (từ dễ đến khó)

| Vấn đề | Phát hiện qua | Giải pháp | Effort |
|---|---|---|---|
| Thiếu DB index | `logical_reads` cao, `PAGEIOLATCH` | Thêm index trên `UserId`, `CategoryId`, `CreatedAt` | Thấp |
| N+1 query | CPU thấp nhưng latency cao, `ASYNC_NETWORK_IO` | Dùng `.Include()` trong EF Core | Thấp |
| Connection pool nhỏ | `http_req_blocked` cao, connections > 100 | Thêm `Max Pool Size=200` vào connection string | Thấp |
| Không cache response | GET /api/products bị query DB mỗi lần | Thêm Redis cache với expiry 30s | Trung bình |
| Transaction quá dài | `LCK_M_X` wait cao | Tách transaction, dùng optimistic concurrency | Trung bình |
| CPU bão hòa | Thread Pool Queue > 0 liên tục | Scale out (thêm instance), load balancer | Cao |

---

## Checklist trước mỗi lần chạy test

```
[ ] docker compose ps  → 4 container đang running
[ ] curl localhost:5000/health  → {"status":"Healthy"}
[ ] curl localhost:5000/metrics  → thấy số liệu (không phải 404)
[ ] docker compose ps  → 4 container đang running (healthy)
[ ] curl localhost:5000/health  → {"status":"Healthy"}
[ ] curl localhost:5000/metrics  → thấy số liệu (không phải 404)
[ ] curl localhost:8086/ping  → HTTP 204 (InfluxDB healthy, KHÔNG có body là đúng)
[ ] localhost:9090 → Status → Targets → dotnet-api = UP
[ ] localhost:3000  → Grafana login được, datasource hoạt động
[ ] Smoke test PASS (2 VU, 0% error)
[ ] Mở SSMS sẵn với 3 query monitor ở trên
[ ] Ghi lại thời điểm bắt đầu test để correlate với Grafana timeline
```

---

---

# PHẦN 3 — Hiểu Từng Thành Phần Trong Hệ Thống

---

## Tại sao lại cần nhiều tool như vậy?

Câu hỏi hợp lý. Câu trả lời ngắn: **mỗi tool giải quyết 1 bài toán khác nhau mà tool kia không làm được.**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Bài toán               │ Tool giải quyết  │ Tại sao không dùng tool khác?  │
│────────────────────────│──────────────────│────────────────────────────────│
│ Giả lập user thật      │ k6               │ Viết script JS, dễ test OAuth  │
│ Lưu số liệu k6 theo thời gian │ InfluxDB │ Time-series DB, ghi nhanh      │
│ Thu thập số liệu API   │ Prometheus       │ Pull model, giữ lịch sử        │
│ Vẽ đồ thị tất cả       │ Grafana          │ Đọc được cả 2 nguồn trên       │
│ Theo dõi container     │ cAdvisor         │ Tự động, không cần config      │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. k6 — Công cụ giả lập user

### k6 là gì?

k6 là **load testing tool** — nó giả lập hàng nghìn user thật cùng lúc gửi request đến API của bạn. Mỗi "user giả" gọi là **VU (Virtual User)**.

```
k6
 ├── Đọc script load-test.js
 ├── Tạo 1000 VU (goroutine), mỗi VU chạy vòng lặp:
 │     1. Login → lấy token
 │     2. GET /api/products
 │     3. POST /api/orders
 │     4. Nghỉ 2–5 giây
 │     5. Lặp lại
 └── Ghi kết quả vào terminal + InfluxDB
```

### Tại sao dùng k6 thay vì JMeter, Locust, ab?

| Tool | Ngôn ngữ script | OAuth 2.0 support | Docker-friendly | Tích hợp Grafana |
|---|---|---|---|---|
| **k6** | JavaScript | Tốt (viết flow tùy ý) | Tốt | Native |
| JMeter | XML (GUI) | Phức tạp | Nặng | Cần plugin |
| Locust | Python | Tốt | Tốt | Cần plugin |
| `ab` / `wrk` | Command line | Không có | Tốt | Không |

> k6 phù hợp với dự án này vì: OAuth 2.0 flow phức tạp (login → lấy token → gắn header → gọi API) — chỉ cần viết JS bình thường.

### Nhược điểm k6

- **Single machine**: 1 instance k6 chỉ tạo được ~10.000 VU (tùy RAM). Muốn test 1 triệu VU cần dùng **k6 Cloud** hoặc distributed mode.
- **Stateless by default**: Mỗi iteration mặc định login lại — tốn tài nguyên nếu token sống lâu. Cần tự cache token nếu muốn tối ưu.
- **Không có browser**: k6 gửi HTTP thuần, không render JavaScript. Để test SPA (Angular) cần dùng **k6 Browser extension**.

---

## 2. InfluxDB — Nơi k6 lưu kết quả

### InfluxDB là gì?

InfluxDB là **time-series database** — database chuyên lưu dữ liệu dạng `(timestamp, metric_name, value)`. Khác với SQL Server lưu "bảng thực thể", InfluxDB lưu "sự kiện theo thời gian".

```
SQL Server:
  Orders(Id, UserId, Amount, CreatedAt)  ← cấu trúc thực thể

InfluxDB:
  http_req_duration, timestamp=14:30:01.234, value=234ms, tags={route="/api/orders"}
  http_req_duration, timestamp=14:30:01.456, value=189ms, tags={route="/api/products"}
  http_req_duration, timestamp=14:30:01.789, value=5200ms, tags={route="/api/orders"}
  ← mỗi request là 1 điểm dữ liệu, không có schema cứng
```

### Tại sao không dùng SQL Server để lưu kết quả k6?

Trong 1 load test với 1000 VU chạy 13 phút:
- k6 ghi **~80.000 data points/giây** vào InfluxDB
- SQL Server không thể handle INSERT liên tục như vậy mà không bị lock
- InfluxDB được tối ưu cho write liên tục, tự tổng hợp theo thời gian (downsampling)

### Tại sao dùng InfluxDB 1.8 mà không phải 2.x?

InfluxDB 2.x thay đổi API hoàn toàn (dùng Flux query language thay SQL-like). k6 hỗ trợ InfluxDB 1.x natively qua `--out influxdb=...`. InfluxDB 2.x cần cấu hình thêm.

### InfluxDB 1.8 KHÔNG có Web UI

> Đây là điểm gây nhầm lẫn phổ biến nhất.

```
http://localhost:8086/        → 404 page not found  ← ĐÚNG, không có UI
http://localhost:8086/ping    → HTTP 204 (không có body)  ← verify healthy
http://localhost:8086/query   → API endpoint để query data
```

InfluxDB 2.x mới có UI tại port 8086. Để xem data từ InfluxDB 1.8 → dùng **Grafana** (datasource InfluxDB).

### Nhược điểm InfluxDB

- **Không phải general-purpose DB**: Không dùng để lưu user, order. Chỉ dùng cho metrics/events.
- **InfluxDB 1.8 đã EOL** (end of life): Vẫn dùng được nhưng không còn nhận bản vá bảo mật. Dùng được cho lab/học tập.
- **Disk tăng nhanh**: 1 load test 13 phút có thể ghi vài trăm MB. Cần cấu hình retention policy.

---

## 3. Prometheus — Thu thập số liệu từ API

### Prometheus là gì?

Prometheus là **monitoring system** hoạt động theo mô hình **pull**: cứ mỗi N giây (cấu hình 5s trong project này), nó gửi `GET /metrics` đến API và lưu lại số liệu.

```
Mỗi 5 giây:
  Prometheus → GET http://host.docker.internal:5000/metrics
  API trả về:
    http_requests_received_total{code="200",method="GET"} 12450
    http_request_duration_seconds_bucket{le="0.1"} 8230
    process_cpu_seconds_total 45.23
    dotnet_gc_collections_total{generation="0"} 234
    ...
  Prometheus lưu vào local storage (TSDB)
```

### Khác gì InfluxDB?

| | Prometheus | InfluxDB (với k6) |
|---|---|---|
| **Ai ghi?** | Prometheus tự đi lấy (pull) | k6 chủ động ghi vào (push) |
| **Lưu gì?** | Số liệu hệ thống: CPU, request count, GC | Kết quả từng request của k6 |
| **Query language** | PromQL | InfluxQL (SQL-like) |
| **Retention** | Mặc định 15 ngày | Cấu hình theo database |

### Tại sao dùng pull thay vì push?

Mô hình **pull** có lợi thế: nếu service chết, Prometheus biết ngay (target = DOWN). Nếu service push thì Prometheus không biết khi nào service ngừng push vì timeout hay vì đang idle.

### Prometheus Web UI — dùng để làm gì?

Vào `http://localhost:9090`:

```
Status → Targets:    Xem target nào đang UP/DOWN
Graph:               Gõ PromQL để xem metric bất kỳ
  Ví dụ: rate(http_requests_received_total[1m])  → req/s trong 1 phút qua
         process_cpu_seconds_total               → tổng CPU đã dùng
         go_memstats_heap_inuse_bytes            → heap đang dùng
```

> Prometheus UI chỉ dùng để debug/kiểm tra nhanh. Để xem đẹp → dùng Grafana.

### Nhược điểm Prometheus

- **Long-term storage kém**: Mặc định giữ 15 ngày, sau đó xóa. Cần Thanos/Cortex cho production dài hạn.
- **Pull model**: Prometheus phải reach được target. Nếu API ở trong private network không expose `/metrics` ra ngoài → không scrape được.
- **Không alert real-time**: AlertManager tách riêng, cần cấu hình thêm.

---

## 4. Grafana — Trung tâm quan sát

### Grafana là gì?

Grafana là **visualization platform** — nó **không lưu data**, chỉ đọc từ nhiều nguồn rồi vẽ đồ thị. Trong project này nó đọc từ 2 nguồn:

```
Grafana
  ├── Datasource: Prometheus  → vẽ dashboard .NET API (CPU, latency, GC)
  └── Datasource: InfluxDB    → vẽ dashboard k6 (VU, error rate, response time)
```

### Tại sao cần Grafana khi Prometheus đã có UI?

Prometheus UI chỉ vẽ được 1 metric mỗi lần, không lưu layout, không alert đẹp. Grafana:
- Vẽ **nhiều metric cùng lúc** trên 1 dashboard
- Lưu dashboard để dùng lại
- Đọc được cả Prometheus lẫn InfluxDB trên **cùng 1 màn hình**
- Dashboard ID 2587 (k6) + 10915 (.NET) cho phép thấy: "lúc k6 tăng lên 500 VU thì GC của .NET tăng bao nhiêu" — correlation giữa 2 datasource

### Nhược điểm Grafana

- **Không lưu data**: Mất Prometheus hoặc InfluxDB → mất data, Grafana chỉ là UI.
- **Dashboard community chất lượng không đều**: Dashboard ID trên Grafana.com do cộng đồng đóng góp, có thể không match chính xác metric name của version bạn dùng. Khi import bị trống → cần chỉnh PromQL query trong panel.

---

## 5. cAdvisor — Giám sát Docker container

### cAdvisor là gì?

cAdvisor (Container Advisor) là tool của Google, chạy trong container, **tự động phát hiện và thu thập metrics của tất cả container Docker** trên cùng host.

```
cAdvisor (đang chạy trong container)
  ├── Đọc /proc, /sys của host → lấy CPU, RAM, network I/O
  ├── Đọc Docker API → biết tên container, image, labels
  └── Expose tại :8080/metrics → Prometheus scrape
```

### Tại sao không dùng `docker stats`?

`docker stats` chỉ xem real-time trên terminal, không lưu lịch sử, không vẽ đồ thị. cAdvisor:
- Tự động expose Prometheus metrics
- Lưu lịch sử qua Prometheus
- Hiện thị trong Grafana dashboard ID 893

### Hạn chế của cAdvisor trong project này

> **Quan trọng:** cAdvisor chỉ theo dõi **container**, không theo dõi process chạy trực tiếp trên host.

Vì API (`dotnet run`) chạy **ngoài Docker**, cAdvisor **không thấy** nó. Để xem CPU/RAM của .NET API:
- Dùng Grafana dashboard ID 10915 (đọc từ Prometheus → `process_cpu_seconds_total`)
- Hoặc `docker stats` nếu API chạy trong container

### Nhược điểm cAdvisor

- **Chỉ có ý nghĩa khi app chạy trong Docker**: Trong setup hiện tại (API chạy `dotnet run` ngoài Docker), cAdvisor chỉ theo dõi Prometheus, Grafana, InfluxDB — những thứ nhẹ, không phải điểm cần quan tâm.
- **Privileged access**: Cần mount `/`, `/sys`, `/var/run` → security concern trong production.

---

## 6. Tổng hợp — Khi nào dùng cái nào?

```
Câu hỏi                                      │ Tool
─────────────────────────────────────────────│──────────────────────────────
"API đang xử lý bao nhiêu req/s?"            │ Grafana (ID 10915) ← Prometheus
"p95 latency của /api/orders là bao nhiêu?"  │ Grafana (ID 10915) ← Prometheus
"k6 đang có bao nhiêu VU, error rate?"      │ Grafana (ID 2587)  ← InfluxDB
"Container nào đang ăn nhiều RAM nhất?"      │ Grafana (ID 893)   ← cAdvisor
"Prometheus có đang scrape API không?"       │ localhost:9090 → Status → Targets
"InfluxDB có đang sống không?"               │ curl localhost:8086/ping → 204
"Tôi muốn test với 500 user trong 5 phút"   │ k6 (chỉnh stages trong load-test.js)
"Tôi muốn xem query SQL nào đang chậm"      │ SSMS → Query 1 (sys.dm_exec_requests)
```

### Sơ đồ data flow đầy đủ

```
[k6]──────push──────► [InfluxDB :8086]
                              │
                              ▼
[.NET API :5000]     [Grafana :3000]◄──query──[Prometheus :9090]
    │   GET /metrics          │                       │
    │◄────────────────────────│            scrape mỗi 5s
    │                         │                       │
    └─────────────────────────┘         [cAdvisor :8080]
                                               │
                                        expose /metrics
                                        cho Prometheus
```

### Có thể bỏ bớt tool không?

| Bỏ tool | Hậu quả |
|---|---|
| Bỏ InfluxDB | Không xem kết quả k6 real-time trên Grafana. Vẫn xem được trên terminal. |
| Bỏ Prometheus | Không theo dõi .NET API metrics. Chỉ xem kết quả k6. |
| Bỏ cAdvisor | Không xem metrics Docker container. Vẫn xem được API qua Prometheus. |
| Bỏ Grafana | Phải đọc terminal k6 + Prometheus UI riêng lẻ, không có dashboard tổng hợp. |
| **Tối thiểu để học** | k6 + terminal là đủ để thấy system fail. Các tool còn lại giúp hiểu *tại sao* fail. |
