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

# PHẦN 2 — Triển khai, Đọc Log và Monitoring (Step by Step)

---

## Bước 1 — Chuẩn bị Monitoring Stack (Prometheus + Grafana + InfluxDB)

### 1.1 Tạo cấu trúc thư mục

```
OpenIdDict/
├── docker/
│   ├── docker-compose.monitoring.yml
│   ├── prometheus/
│   │   └── prometheus.yml
│   └── grafana/
│       └── provisioning/     ← Grafana tự load datasource/dashboard
```

```bash
mkdir -p docker/prometheus docker/grafana/provisioning/datasources
```

### 1.2 Tạo `docker/prometheus/prometheus.yml`

```yaml
global:
  scrape_interval: 5s        # Thu thập metrics mỗi 5 giây (mặc định 15s, giảm để xem real-time)
  evaluation_interval: 5s

scrape_configs:

  # .NET API — expose qua endpoint /metrics (prometheus-net.AspNetCore)
  - job_name: 'dotnet-api'
    static_configs:
      - targets: ['host.docker.internal:5000']   # Nếu API chạy trên host (không phải Docker)
        # targets: ['api:8080']                   # Nếu API chạy trong Docker cùng network
    metrics_path: '/metrics'

  # cAdvisor — theo dõi CPU/RAM/disk của từng Docker container
  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']

  # k6 — k6 tự expose metrics qua port 5665 khi dùng --out statsd hoặc xDash
  # (dùng InfluxDB thay thế, xem phần k6 output bên dưới)
```

### 1.3 Tạo `docker/docker-compose.monitoring.yml`

```yaml
version: '3.8'

services:

  # ── Prometheus: Thu thập và lưu metrics theo dạng time-series ──────────────
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"           # Web UI: http://localhost:9090
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.retention.time=7d'   # Giữ data 7 ngày
    restart: unless-stopped

  # ── Grafana: Visualize metrics từ Prometheus và InfluxDB ───────────────────
  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"           # Web UI: http://localhost:3000
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin123
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana-data:/var/lib/grafana
    depends_on:
      - prometheus
      - influxdb
    restart: unless-stopped

  # ── InfluxDB: Lưu kết quả k6 (time-series database) ───────────────────────
  influxdb:
    image: influxdb:1.8       # k6 hỗ trợ InfluxDB v1.x native
    container_name: influxdb
    ports:
      - "8086:8086"
    environment:
      - INFLUXDB_DB=k6        # Database tự động tạo khi start
      - INFLUXDB_HTTP_AUTH_ENABLED=false
    volumes:
      - influxdb-data:/var/lib/influxdb
    restart: unless-stopped

  # ── cAdvisor: Thu thập CPU/RAM/Network của Docker containers ──────────────
  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    container_name: cadvisor
    ports:
      - "8080:8080"
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker/:/var/lib/docker:ro
    restart: unless-stopped

volumes:
  prometheus-data:
  grafana-data:
  influxdb-data:
```

### 1.4 Khởi động monitoring stack

```bash
cd docker/
docker compose -f docker-compose.monitoring.yml up -d

# Kiểm tra tất cả container đang chạy
docker compose -f docker-compose.monitoring.yml ps
```

Kết quả mong đợi:
```
NAME          STATUS
prometheus    running (healthy)
grafana       running (healthy)
influxdb      running
cadvisor      running
```

---

## Bước 2 — Cài Prometheus metrics vào .NET API

### 2.1 Thêm package

```bash
cd AuthDemo.Api/
dotnet add package prometheus-net.AspNetCore
dotnet add package prometheus-net.DotNetRuntime   # Thêm GC, thread pool metrics
```

### 2.2 Cập nhật `Program.cs`

```csharp
using Prometheus;

// ... sau builder.Build() ...

// Khai báo metrics trước UseRouting
app.UseRouting();

app.UseHttpMetrics(options =>
{
    // Ghi lại method, status code, path (gộp path params → /api/products/{id})
    options.AddCustomLabel("version", _ => "v1");
});

// DotNetRuntime metrics: GC, thread pool, exception rate
DotNetRuntimeStatsCollector.StartCollection();

// Expose /metrics endpoint — Prometheus sẽ scrape endpoint này
app.MapMetrics();  // Default: GET /metrics

// Health check endpoint — k6 dùng để verify API online trước khi test
app.MapHealthChecks("/health");
```

### 2.3 Verify endpoint hoạt động

```bash
# Sau khi chạy API
curl http://localhost:5000/metrics | head -30
```

Output mẫu:
```
# HELP http_requests_received_total Total HTTP requests received
# TYPE http_requests_received_total counter
http_requests_received_total{code="200",method="GET",controller="Products"} 1250
http_requests_received_total{code="201",method="POST",controller="Orders"} 342
...
# HELP process_cpu_seconds_total Total user and system CPU time
process_cpu_seconds_total 45.23
```

---

## Bước 3 — Seed dữ liệu và kiểm tra API sẵn sàng

### 3.1 Khởi động API (tự seed users)

```bash
cd AuthDemo.Api/
dotnet run
```

Chờ thấy log:
```
[INF] Đã tạo 100 test users cho k6 load testing
[INF] Application started. Press Ctrl+C to shut down.
```

### 3.2 Seed thêm Categories và Products (chạy script SQL)

Tạo file `seed-data.sql` và chạy trên SQL Server:

```sql
-- Seed 5 categories
INSERT INTO Categories (Name, Description, IsActive, CreatedAt) VALUES
('Điện thoại',     'Smartphone các loại', 1, GETUTCDATE()),
('Laptop',         'Máy tính xách tay',   1, GETUTCDATE()),
('Phụ kiện',       'Tai nghe, sạc, ốp',   1, GETUTCDATE()),
('Máy tính bảng',  'iPad, Android tablet', 1, GETUTCDATE()),
('Đồng hồ thông minh', 'Smartwatch',      1, GETUTCDATE());

-- Seed 50 products (lặp 10 sản phẩm × 5 category)
DECLARE @i INT = 1;
WHILE @i <= 50
BEGIN
    INSERT INTO Products (CategoryId, Name, Description, Price, Stock, IsActive, CreatedAt)
    VALUES (
        (@i % 5) + 1,
        N'Sản phẩm ' + CAST(@i AS NVARCHAR),
        N'Mô tả sản phẩm số ' + CAST(@i AS NVARCHAR),
        CAST(100000 + (@i * 50000) AS DECIMAL(18,2)),
        1000,
        1,
        GETUTCDATE()
    );
    SET @i = @i + 1;
END;
```

### 3.3 Verify bằng curl trước khi test

```bash
# Login thử với 1 test user
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile"

# Kết quả phải có access_token
# {"access_token":"eyJ...","token_type":"Bearer","expires_in":3600}

# Dùng token vừa lấy để xem sản phẩm
TOKEN="eyJ..."
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/products
```

---

## Bước 4 — Chạy k6 từng giai đoạn

### 4.1 Smoke Test — Kiểm tra script trước (1–2 users, 1 phút)

> Mục tiêu: Đảm bảo script không có lỗi, flow hoạt động đúng.

Tạo file `k6/smoke-test.js`:
```javascript
import { options as mainOptions } from './load-test.js';
export { default } from './load-test.js';

// Override: chỉ 2 VU trong 1 phút
export const options = {
  vus:      2,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
  },
};
```

Chạy:
```bash
# Cách 1: Override trực tiếp từ command line
k6 run --vus 2 --duration 1m k6/load-test.js

# Cách 2: Docker
docker run --rm -i \
  -v $(pwd)/k6:/scripts \
  --add-host=host.docker.internal:host-gateway \
  grafana/k6 run --vus 2 --duration 1m /scripts/load-test.js
```

Kết quả mong đợi (smoke test PASS):
```
✓ login 200.............: 100%
✓ products 200..........: 100%
✓ create order 201......: 98%   ← 2% fail là bình thường (stock 0, sản phẩm không tồn tại)

http_req_duration: avg=180ms  p(95)=420ms
```

### 4.2 Load Test — Tải bình thường (k6/load-test.js gốc)

```bash
# Chạy với output vào InfluxDB để xem real-time trên Grafana
k6 run \
  --out influxdb=http://localhost:8086/k6 \
  -e BASE_URL=http://localhost:5000 \
  k6/load-test.js

# Docker version:
docker run --rm -i \
  -v $(pwd)/k6:/scripts \
  --network host \
  grafana/k6 run \
    --out influxdb=http://localhost:8086/k6 \
    -e BASE_URL=http://localhost:5000 \
    /scripts/load-test.js
```

### 4.3 Stress Test — Tìm giới hạn hệ thống

Chỉnh sửa stages trong `load-test.js` để test tới 2000 VU:

```javascript
export const options = {
  stages: [
    { duration: '2m', target: 200  },
    { duration: '5m', target: 500  },
    { duration: '5m', target: 1000 },
    { duration: '5m', target: 2000 },  // ← Hệ thống bắt đầu fail ở đây
    { duration: '3m', target: 0    },
  ],
};
```

### 4.4 Spike Test — Đột ngột tăng tải (giả lập flash sale)

```javascript
export const options = {
  stages: [
    { duration: '30s', target: 10   },  // Tải thấp bình thường
    { duration: '10s', target: 1000 },  // ← Đột ngột tăng (spike)
    { duration: '3m',  target: 1000 },  // Giữ tải cao
    { duration: '10s', target: 10   },  // Drop về bình thường
    { duration: '30s', target: 0    },
  ],
};
```

---

## Bước 5 — Đọc Output k6 Terminal (Real-time)

Trong khi k6 chạy, terminal sẽ hiển thị:

```
          /\      |‾‾| /‾‾/   /‾‾/
     /\  /  \     |  |/  /   /  /
    /  \/    \    |     (   /   ‾‾\
   /          \   |  |\  \ |  (‾)  |
  / __________ \  |__| \__\ \_____/ .io

  execution: local
     script: k6/load-test.js
     output: influxdb (http://localhost:8086/k6)

  scenarios: (100.00%) 1 scenario, 1000 max VUs, 14m30s max duration
           * default: Up to 1000 looping VUs for 13m0s over 5 stages

running (05m30.0s), 500/1000 VUs, 12450 complete and 0 interrupted iterations
default ↓ [===============>------] 500/1000 VUs  05m30.0s/13m00.0s
```

### Giải thích các dòng cuối sau khi test kết thúc

```
     ✓ login 200.............: 99.82% ✓ 24560 ✗ 44
     ✓ products 200..........: 98.41% ✓ 24173 ✗ 387
     ✓ create order 201......: 94.20% ✓ 11610 ✗ 714  ← Nhiều fail → vấn đề!
     ✓ create order not 500..: 97.30% ✓ 11980 ✗ 324

     checks.........................: 97.35% ✓ 72343 ✗ 1969
     data_received..................: 1.2 GB 1.5 MB/s
     data_sent......................: 45 MB  57 kB/s
     http_req_blocked...............: avg=1.2ms    min=1µs    med=3µs    max=2.89s   p(90)=5µs    p(95)=7µs
     http_req_connecting............: avg=800µs    min=0s     med=0s     max=1.84s   p(90)=0s     p(95)=0s
   ✗ http_req_duration..............: avg=1.23s    min=12ms   med=890ms  max=28.3s   p(90)=2.8s   p(95)=4.1s
       { expected_response:true }...: avg=1.10s    min=12ms   med=780ms  max=25s     p(90)=2.5s   p(95)=3.6s
     http_req_failed................: 3.20%  ✗ 1969  ✓ 59634
     http_req_receiving.............: avg=145µs    min=9µs    med=68µs   max=890ms
     http_req_sending...............: avg=89µs     min=5µs    med=32µs   max=450ms
     http_req_tls_handshaking.......: avg=0s       min=0s     med=0s     max=0s
     http_req_waiting...............: avg=1.08s    min=8ms    med=730ms  max=27.8s   p(90)=2.6s   p(95)=3.9s
     http_reqs......................: 61603  78.98/s
     iteration_duration.............: avg=8.45s    min=2.1s   med=7.2s   max=45s     p(90)=15s    p(95)=22s
     iterations.....................: 12320  15.80/s
     login_duration.................: avg=420ms    p(95)=1.2s
     product_list_duration..........: avg=180ms    p(95)=450ms
     create_order_duration..........: avg=2.1s     p(95)=5.8s  ← Bottleneck rõ ràng
     vus............................: 1000   min=2     max=1000
     vus_max........................: 1000   min=1000  max=1000
```

### Bảng giải thích từng metric

| Metric | Ý nghĩa | Giá trị tốt | Giá trị xấu |
|---|---|---|---|
| `http_req_duration` | Tổng thời gian 1 request (từ gửi đến nhận xong) | p95 < 2s | p95 > 5s |
| `http_req_waiting` | Thời gian server xử lý (TTFB) | < 1s | > 3s |
| `http_req_blocked` | Chờ kết nối TCP (connection pool full?) | < 5ms | > 100ms |
| `http_req_failed` | Tỷ lệ lỗi (4xx, 5xx, timeout) | < 1% | > 5% |
| `http_reqs` | Tổng requests và throughput (req/s) | Tùy mục tiêu | Drop đột ngột |
| `iteration_duration` | Thời gian 1 vòng lặp của VU (bao gồm sleep) | < 10s | > 30s |
| `vus` | Số VU đang chạy thực tế | = target | < target = VU bị treo |

### Hiểu Percentiles (p50, p90, p95, p99)

```
avg=1.23s   ← Trung bình (bị ảnh hưởng bởi outlier)
p(50)=890ms ← 50% request hoàn thành dưới 890ms (median — đáng tin hơn avg)
p(90)=2.8s  ← 90% request hoàn thành dưới 2.8s
p(95)=4.1s  ← 95% request hoàn thành dưới 4.1s  ← DÙNG METRIC NÀY để đánh giá SLA
p(99)=12s   ← 1% request chậm nhất (outlier, thường là request đầu tiên hoặc GC pause)
max=28.3s   ← Request chậm nhất (đừng quá lo nếu chỉ 1–2 lần)
```

> **Quy tắc thực tế:** Dùng **p95** để đánh giá hệ thống. Nếu p95 < 2s, 95% người dùng trải nghiệm tốt.

### Đọc Thresholds (PASS/FAIL)

```
✓ http_req_duration............: avg=1.23s  p(95)=4.1s    ← ✗ FAIL (threshold p95<3000ms)
✗ http_req_failed..............: 3.20%                    ← ✗ FAIL (threshold rate<0.01)
```

- `✓` = threshold đạt
- `✗` = threshold không đạt → k6 exit với error code 99 → CI pipeline fail

---

## Bước 6 — Setup Grafana Dashboard (Visual Monitoring)

### 6.1 Mở Grafana và cấu hình InfluxDB datasource

1. Mở trình duyệt: `http://localhost:3000`
2. Login: `admin` / `admin123`
3. Vào **Connections → Data Sources → Add data source**
4. Chọn **InfluxDB**
5. Điền:
   ```
   URL:      http://influxdb:8086
   Database: k6
   ```
6. Click **Save & Test** → phải hiện "Data source connected"

### 6.2 Cấu hình Prometheus datasource

1. **Connections → Data Sources → Add data source**
2. Chọn **Prometheus**
3. URL: `http://prometheus:9090`
4. **Save & Test**

### 6.3 Import Dashboard k6 (có sẵn trên Grafana.com)

1. Vào **Dashboards → Import**
2. Nhập ID: **`2587`** → Click **Load**
3. Chọn datasource **InfluxDB** → **Import**

Hoặc import dashboard .NET:
- ID **`10915`** — ASP.NET Core metrics (prometheus-net)
- ID **`893`** — Docker container metrics (cAdvisor)

### 6.4 Đọc Dashboard k6 (ID 2587)

Dashboard có các panels:

```
┌─────────────────────────────────────────────────────────┐
│  Virtual Users (VUs)        │  Request Rate (req/s)     │
│  [graph tăng theo stages]   │  [số request/giây]        │
├─────────────────────────────────────────────────────────┤
│  Response Time (ms)                                     │
│  ── p50 (xanh)  ── p90 (vàng)  ── p95 (cam)  ── p99    │
│                                                         │
│  Hình dạng lý tưởng: đường thẳng ngang                 │
│  Nguy hiểm: đường đi lên theo số VU → không scale được │
├─────────────────────────────────────────────────────────┤
│  Error Rate (%)             │  Check Pass Rate (%)      │
│  [< 1% = tốt]              │  [> 99% = tốt]            │
└─────────────────────────────────────────────────────────┘
```

**Cách đọc Response Time graph:**

```
ms
4000 |                                      ╭────╮
3000 |                              ╭───────╯    │  ← p95 vượt ngưỡng ở 800 VUs
2000 |                     ╭────────╯            │    → đây là điểm bão hòa
1000 |──────────────────────╯
   0 └────────────────────────────────────────────→ thời gian / VUs
       50  100  200  300  500  800  1000
```

→ Hệ thống này bắt đầu degraded ở 800 VUs.

---

## Bước 7 — Đọc Dashboard .NET API (ID 10915)

### Panels quan trọng

| Panel | Ý nghĩa | Hành động khi xấu |
|---|---|---|
| **HTTP Request Rate** | Request/s đang xử lý | Nếu thấp hơn k6 gửi → API bị queue |
| **HTTP Request Duration** | Latency theo route | Tìm route nào chậm nhất |
| **Active Requests** | Số request đang chờ | > 50 → connection pool có vấn đề |
| **GC Collections (Gen0/1/2)** | Garbage Collection | Gen2 tăng liên tục → memory leak |
| **Heap Size** | RAM .NET đang dùng | Tăng không ngừng → memory leak |
| **Thread Pool Queue** | Tasks chờ trong queue | > 0 → CPU là bottleneck |
| **Exception Rate** | Unhandled exceptions/s | > 0 → bug hoặc quá tải |

### Dấu hiệu bottleneck theo panel

**CPU bottleneck (2 core bị saturate):**
```
Thread Pool Queue > 0          → Có task đang chờ CPU
process_cpu_seconds tăng ~2/s  → 100% CPU utilization
HTTP Duration tăng đều theo VU → Không scale được thêm
```

**SQL Server bottleneck:**
```
HTTP Request Duration cao (>2s) nhưng CPU .NET thấp
→ App đang chờ DB trả lời → Cần xem SQL Server metrics
```

**Memory pressure:**
```
GC Gen2 collections tăng → Major GC đang chạy → Pause mọi thread
Heap size tăng liên tục → Memory leak
```

---

## Bước 8 — Monitoring SQL Server trong khi test

### 8.1 Xem slow queries real-time (SSMS)

Chạy query sau trên SQL Server trong lúc k6 đang test:

```sql
-- Top 10 query chậm nhất đang chạy
SELECT TOP 10
    r.session_id,
    r.status,
    r.wait_type,
    r.wait_time / 1000.0       AS wait_sec,
    r.cpu_time / 1000.0        AS cpu_sec,
    r.total_elapsed_time / 1000.0 AS elapsed_sec,
    r.logical_reads,
    SUBSTRING(t.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS query_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50        -- Bỏ qua system sessions
ORDER BY r.total_elapsed_time DESC;
```

### 8.2 Xem số connections đang mở

```sql
-- Connections theo database
SELECT DB_NAME(dbid) AS database_name, COUNT(*) AS connections
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid
ORDER BY connections DESC;
```

**Bình thường:** 10–30 connections
**Nguy hiểm:** > 100 connections → connection pool leak hoặc không đủ pool size

### 8.3 Xem wait statistics (tìm bottleneck SQL)

```sql
-- Xem SQL Server đang chờ gì nhiều nhất
SELECT TOP 10
    wait_type,
    wait_time_ms / 1000.0       AS wait_sec,
    signal_wait_time_ms / 1000.0 AS signal_sec,
    waiting_tasks_count
FROM sys.dm_os_wait_stats
WHERE wait_type NOT IN (
    'SLEEP_TASK','BROKER_TO_FLUSH','BROKER_TASK_STOP','CLR_AUTO_EVENT',
    'DISPATCHER_QUEUE_SEMAPHORE','FT_IFTS_SCHEDULER_IDLE_WAIT',
    'HADR_FILESTREAM_IOMGR_IOCOMPLETION','HADR_WORK_QUEUE',
    'LAZYWRITER_SLEEP','LOGMGR_QUEUE','ONDEMAND_TASK_QUEUE',
    'REQUEST_FOR_DEADLOCK_SEARCH','RESOURCE_QUEUE','SERVER_IDLE_CHECK',
    'SLEEP_DBSTARTUP','SLEEP_DBRECOVER','SLEEP_MASTERDBREADY',
    'SLEEP_MASTERMDREADY','SLEEP_MASTERUPGRADED','SLEEP_MSDBSTARTUP',
    'SLEEP_SYSTEMTASK','SLEEP_TEMPDBSTARTUP','SNI_HTTP_ACCEPT',
    'SP_SERVER_DIAGNOSTICS_SLEEP','SQLTRACE_BUFFER_FLUSH',
    'SQLTRACE_INCREMENTAL_FLUSH_SLEEP','WAIT_XTP_OFFLINE_CKPT_NEW_LOG',
    'WAITFOR','XE_DISPATCHER_WAIT','XE_TIMER_EVENT'
)
ORDER BY wait_time_ms DESC;
```

| Wait Type | Ý nghĩa |
|---|---|
| `PAGEIOLATCH_SH` | Đọc dữ liệu từ disk → cần SSD hoặc thêm RAM |
| `LCK_M_X` | Lock tranh chấp → deadlock, cần tối ưu transaction |
| `CXPACKET` | Parallel query → CPU bị chia nhỏ |
| `ASYNC_NETWORK_IO` | App đọc data chậm hơn SQL ghi → N+1 query |
| `RESOURCE_SEMAPHORE` | Quá nhiều query cần RAM cùng lúc |

---

## Bước 9 — Phân tích kết quả và hành động

### 9.1 Quy trình phân tích 5 bước

```
1. Xem Error Rate trong k6 output
       ↓
   Nếu > 5% → Hệ thống đang fail → Tìm nguyên nhân ngay

2. Xem p95 của http_req_duration
       ↓
   < 1s  → Tốt
   1–3s  → Chấp nhận được (warning)
   > 3s  → Xấu, cần tối ưu

3. Xem tại route nào chậm nhất (Grafana: HTTP Duration by route)
       ↓
   Thường là: POST /api/orders (write + deduct stock + transaction)

4. Xem CPU và RAM (cAdvisor dashboard)
       ↓
   CPU 100% → Scale CPU hoặc tối ưu code
   RAM đầy → Tăng RAM hoặc fix memory leak

5. Xem SQL Server wait stats
       ↓
   LCK_M_X cao → Transaction quá dài, cần tối ưu
   PAGEIOLATCH cao → Thêm index
```

### 9.2 Các tối ưu theo thứ tự ưu tiên

| Vấn đề | Phát hiện qua | Giải pháp | Effort |
|---|---|---|---|
| Thiếu DB index | SQL slow query, high wait | Thêm index (CreatedAt, UserId, CategoryId) | Thấp |
| N+1 query | CPU thấp nhưng latency cao | Dùng `.Include()` hoặc batch query | Thấp |
| Connection pool nhỏ | `http_req_blocked` cao | Tăng `Max Pool Size` trong connection string | Thấp |
| Response không cache | Sản phẩm query nhiều lần | Thêm Redis cache cho GET /api/products | Trung bình |
| Transaction quá dài | `LCK_M_X` wait cao | Tách nhỏ transaction, dùng optimistic lock | Trung bình |
| CPU bão hòa | Thread pool queue > 0 | Scale out (thêm instance API) | Cao |
| DB là single point | Mọi thứ chậm cùng lúc | Read replica hoặc sharding | Cao |

### 9.3 Lệnh một dòng để xem container resource

```bash
# Xem CPU/RAM của từng container real-time (cập nhật 2 giây)
docker stats --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"

# Output:
# NAME          CPU %     MEM USAGE / LIMIT     MEM %
# api           185.2%    1.2GiB / 16GiB        7.5%    ← Đang dùng 185% CPU (2 cores = 200% max)
# sqlserver     45.3%     4.8GiB / 16GiB        30%
# prometheus    2.1%      512MiB / 16GiB         3.2%
```

---

## Checklist trước mỗi lần chạy test

```
[ ] Monitoring stack đang chạy (docker stats)
[ ] API trả về /health = 200
[ ] 100 test users đã có trong DB
[ ] Categories và Products đã được seed
[ ] Grafana đang load dashboard
[ ] Smoke test (2 VU) chạy thành công
[ ] Mở tab SQL Server để theo dõi wait stats
[ ] Sẵn sàng ghi nhận thời điểm hệ thống bắt đầu fail
```
