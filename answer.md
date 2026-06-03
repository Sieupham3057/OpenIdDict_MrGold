# Trả lời câu hỏi về Load Testing & Monitoring

## Mục lục

**[Phần 1 — Hỏi & Đáp](#trả-lời-câu-hỏi-về-load-testing--monitoring)**
- [1. Server specs phục vụ được bao nhiêu concurrent user?](#1-server-specs-này-phục-vụ-được-bao-nhiêu-concurrent-user)
- [2. Có nên tạo thêm table Product, Category, Order?](#2-có-nên-tạo-thêm-table-product-category-order-không)
- [3. Giả lập hàng triệu user login + thao tác đồng thời](#3-giả-lập-hàng-triệu-user-login--thao-tác-đồng-thời)
- [4. Monitoring hệ thống](#4-monitoring-hệ-thống)
- [Tóm tắt roadmap thực hành](#tóm-tắt-roadmap-thực-hành)

**[Phần 2 — Thực hành: Chạy và Đọc Monitoring](#phần-2--thực-hành-chạy-và-đọc-monitoring-step-by-step)**
- [Kiến trúc tổng quan](#kiến-trúc-tổng-quan--các-thành-phần-liên-kết-với-nhau-như-thế-nào)
- [Bước 1 — Khởi động toàn bộ stack](#bước-1--khởi-động-toàn-bộ-stack-api--monitoring)
- [Bước 2 — Verify API hoạt động đúng](#bước-2--verify-api-hoạt-động-đúng)
- [Bước 3 — Cấu hình Grafana](#bước-3--cấu-hình-grafana-làm-1-lần-duy-nhất)
  - [3.4 Tìm dashboard & xem ngay không cần k6](#34-tìm-dashboard-sau-khi-import--xem-ngay-không-cần-k6)
  - [3.5 Debug N/A theo từng dashboard](#35-tại-sao-dashboard-hiện-na--debug-theo-từng-dashboard)
  - [3.6 Percentile P50/P95/P99](#36-percentile-là-gì-p50-p95-p99--đọc-như-thế-nào)
- [Bước 4 — Chạy k6 từng giai đoạn](#bước-4--chạy-k6-từng-giai-đoạn)
  - [4.1 Smoke Test](#41-smoke-test--2-phút-2-vu-luôn-chạy-đầu-tiên)
  - [4.2 Load Test](#42-load-test--tải-bình-thường-13-phút)
  - [4.3 Stress Test](#43-stress-test--tìm-giới-hạn-manual)
  - [4.4 Spike Test](#44-spike-test--flash-sale-simulation-manual)
- [Bước 5 — Đọc Output k6 Terminal](#bước-5--đọc-output-k6-terminal)
- [Bước 6 — Đọc Grafana Dashboard](#bước-6--đọc-grafana-dashboard)
- [Bước 7 — Theo dõi SQL Server](#bước-7--theo-dõi-sql-server-trong-lúc-test)
- [Bước 8 — Quy trình phân tích vấn đề](#bước-8--quy-trình-phân-tích-khi-thấy-vấn-đề)

**[Phần 3 — Hiểu Từng Thành Phần](#phần-3--hiểu-từng-thành-phần-trong-hệ-thống)**
- [Tại sao cần nhiều tool?](#tại-sao-lại-cần-nhiều-tool-như-vậy)
- [1. k6 — Giả lập user](#1-k6--công-cụ-giả-lập-user)
- [2. InfluxDB — Lưu kết quả k6](#2-influxdb--nơi-k6-lưu-kết-quả)
- [3. Prometheus — Thu thập metrics](#3-prometheus--thu-thập-số-liệu-từ-api)
- [4. Grafana — Visualize](#4-grafana--trung-tâm-quan-sát)
- [5. cAdvisor — Giám sát container](#5-cadvisor--giám-sát-docker-container)
- [6. Tổng hợp — Khi nào dùng cái nào](#6-tổng-hợp--khi-nào-dùng-cái-nào)

**[Phần 4 — Bảng lệnh Docker Compose thường dùng](#phần-4--bảng-lệnh-docker-compose-thường-dùng)**
- [Khởi động / Dừng stack](#khởi-động--dừng-stack)
- [Build image](#build-image)
- [Xem log](#xem-log)
- [Kiểm tra trạng thái](#kiểm-tra-trạng-thái)
- [Vào trong container](#vào-trong-container)
- [Dọn dẹp](#dọn-dẹp)
- [Workflow thực tế](#workflow-thực-tế)

**[Phần 5 — OpenIddict Certificate trong Docker: Nguyên nhân lỗi & Fix chuẩn](#phần-5--openiddict-certificate-trong-docker-nguyên-nhân-lỗi--fix-chuẩn)**
- [Ephemeral là gì?](#ephemeral-là-gì)
- [Cách dùng hiện tại có đúng cho production không?](#cách-dùng-hiện-tại-có-đúng-cho-production-không)
  - [3 vấn đề của Ephemeral trong production](#3-vấn-đề-của-ephemeral-trong-production)
  - [Dự án thiếu gì để production-ready?](#dự-án-thiếu-gì-để-production-ready)
- [Chuỗi nguyên nhân từng bước](#chuỗi-nguyên-nhân-từng-bước)
  - [Bước 1: ASPNETCORE_ENVIRONMENT dẫn code vào nhánh nào?](#bước-1-aspnetcore_environment-dẫn-code-vào-nhánh-nào)
  - [Bước 2: AddDevelopmentEncryptionCertificate làm gì bên trong?](#bước-2-adddevelopmentencryptioncertificate-làm-gì-bên-trong)
  - [Bước 3: Tại sao /home/appuser không tồn tại?](#bước-3-tại-sao-homeappuser-không-tồn-tại)
- [Tại sao cờ EphemeralKeySet bắt buộc khi load cert từ file?](#tại-sao-cờ-ephemeralkeyset-bắt-buộc-khi-load-cert-từ-file)
- [Fix cấp độ 1 — Docker lab / staging](#fix-cấp-độ-1--docker-lab--staging)
- [Fix cấp độ 2 — Production thực sự](#fix-cấp-độ-2--production-thực-sự)
- [Hiểu điều gì thay đổi khi deploy version mới](#hiểu-điều-gì-thay-đổi-khi-deploy-version-mới)
- [Tóm tắt quyết định theo môi trường](#tóm-tắt-quyết-định-theo-môi-trường)
- [Checklist debug khi gặp lỗi](#checklist-debug-khi-gặp-lỗi-certificate-openiddict-trong-docker)

**[Phần 6 — Prometheus Web UI & PromQL thực tế](#phần-6--prometheus-web-ui--promql-thực-tế)**
- [Prometheus Web UI dùng để làm gì?](#prometheus-web-ui--dùng-để-làm-gì)
- [Các nhóm PromQL phổ biến thực tế](#các-nhóm-promql-phổ-biến-thực-tế)
  - [HTTP metrics — đo tải và latency](#1-http-metrics--đo-tải-và-latency)
  - [.NET process metrics — CPU và bộ nhớ](#2-net-process-metrics--cpu-và-bộ-nhớ)
  - [.NET GC metrics — theo dõi Garbage Collector](#3-net-gc-metrics--theo-dõi-garbage-collector)
  - [cAdvisor container metrics](#4-cadvisor-container-metrics)
- [Các hàm PromQL cốt lõi](#các-hàm-promql-cốt-lõi-cần-biết)
- [Làm sao biết metric nào tồn tại?](#làm-sao-biết-metric-nào-tồn-tại--tìm-ở-đâu)
- [Tìm tài liệu ở đâu?](#tìm-tài-liệu-ở-đâu)

**[Phần 7 — Crash Test & Scale Load Balancing Thực Tế](#phần-7--crash-test--scale-load-balancing-thực-tế)**
- [Tổng quan kịch bản & IP Plan](#70-tổng-quan-kịch-bản--ip-plan)
- [Giai đoạn 0 — Chuẩn bị VMware Workstation](#giai-đoạn-0--chuẩn-bị-vmware-workstation)
- [Giai đoạn 1 — Deploy 1 máy + Crash Test](#giai-đoạn-1--deploy-1-máy--crash-test)
  - [Step 1 — Tạo cert dùng chung](#step-1--tạo-cert-dùng-chung)
  - [Step 2 — Cập nhật docker-compose dùng cert](#step-2--cập-nhật-docker-compose-dùng-cert)
  - [Step 3 — Khởi động stack VM1](#step-3--khởi-động-toàn-bộ-stack-vm1)
  - [Step 4 — Verify VM1 hoạt động đúng](#step-4--verify-vm1-hoạt-động-đúng)
  - [Step 5 — Chạy Crash Test lần 1](#step-5--chạy-crash-test-lần-1-tìm-ngưỡng-chết)
  - [Step 6 — Đọc kết quả & ghi ngưỡng](#step-6--đọc-kết-quả--ghi-ngưỡng-quan-trọng)
- [Giai đoạn 2 — Setup VM2 trong VMware](#giai-đoạn-2--setup-vm2-trong-vmware)
  - [Step 7 — Tạo VM2 trong VMware Workstation](#step-7--tạo-vm2-trong-vmware-workstation)
  - [Step 8 — Cài Docker trên VM2](#step-8--cài-docker-trên-vm2)
  - [Step 9 — Copy source code + cert sang VM2](#step-9--copy-source-code--cert-sang-vm2)
  - [Step 10 — Khởi động API trên VM2](#step-10--khởi-động-api-trên-vm2)
  - [Step 11 — Verify VM2 API healthy](#step-11--verify-vm2-api-healthy)
- [Giai đoạn 3 — Setup Nginx Load Balancer trên VM1](#giai-đoạn-3--setup-nginx-load-balancer-trên-vm1)
  - [Step 12 — Cấu hình nginx.conf với IP thực](#step-12--cấu-hình-nginxconf-với-ip-thực)
  - [Step 13 — Khởi động Nginx LB](#step-13--khởi-động-nginx-lb)
  - [Step 14 — Verify LB phân phối đến cả 2 node](#step-14--verify-lb-phân-phối-đến-cả-2-node)
- [Giai đoạn 4 — Verify Token Cross-Instance](#giai-đoạn-4--verify-token-cross-instance)
  - [Step 15 — Test token được chấp nhận trên cả 2 instance](#step-15--test-token-được-chấp-nhận-trên-cả-2-instance)
- [Giai đoạn 5 — Crash Test 2 & So Sánh](#giai-đoạn-5--crash-test-2--so-sánh)
  - [Step 16 — Update Prometheus scrape VM2](#step-16--update-prometheus-scrape-vm2)
  - [Step 17 — Chạy Crash Test lần 2 qua Load Balancer](#step-17--chạy-crash-test-lần-2-qua-load-balancer)
  - [Step 18 — Đọc và so sánh kết quả](#step-18--đọc-và-so-sánh-kết-quả)
- [Bottleneck tiếp theo sau khi scale API](#bottleneck-tiếp-theo-sau-khi-scale-api)

**[Phụ lục — Tóm tắt thay đổi code & infrastructure](#phụ-lục--tóm-tắt-thay-đổi-code--infrastructure)**
- [Những file đã được tạo mới](#những-file-đã-được-tạo-mới)
- [Những file đã được cập nhật](#những-file-đã-được-cập-nhật)
- [Luồng triển khai toàn bộ](#luồng-triển-khai-toàn-bộ)

---

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
│                SERVER (192.168.1.35) — toàn bộ chạy Docker             │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      Docker Compose Network                      │   │
│  │                                                                  │   │
│  │  ┌─────────────────┐   scrape   ┌──────────────────────────┐   │   │
│  │  │  api            │◄───────────│  Prometheus (9090)        │   │   │
│  │  │  :8080 (→5000)  │            │                          │   │   │
│  │  │  GET /metrics   │            │  Grafana    (3000) ◄──bạn│   │   │
│  │  │  GET /health    │            │  InfluxDB   (8086) ◄──k6 │   │   │
│  │  └─────────────────┘            │  cAdvisor   (8080)       │   │   │
│  │       ↑ cAdvisor                └──────────────────────────┘   │   │
│  │       theo dõi container này                                    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
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
- **api**: AuthDemo .NET 8 chạy trong Docker container, port 8080 bên trong, map ra host port 5000
- **k6**: giả lập hàng trăm/nghìn user thật — login, đọc sản phẩm, tạo đơn hàng
- **Prometheus**: cứ 5 giây scrape `api:8080/metrics` để lấy CPU, request count, latency (dùng tên service Docker, không cần IP)
- **InfluxDB**: k6 đẩy kết quả test vào đây theo thời gian thực
- **Grafana**: đọc cả Prometheus lẫn InfluxDB rồi vẽ đồ thị
- **cAdvisor**: theo dõi tất cả container trong Docker kể cả `api` — giờ thấy được CPU/RAM của API

---

## Bước 1 — Khởi động toàn bộ stack (API + Monitoring)

### 1.1 Lần đầu — Build image và chạy

```bash
# Đứng ở thư mục docker/ trên server
cd ~/projects/OpenIdDict_MrGold/docker

# --build: build Docker image cho API từ Dockerfile (lần đầu mất ~2-3 phút)
docker compose -f docker-compose.monitoring.yml up -d --build
```

> Lần sau (không đổi code) dùng `up -d` thôi — không cần `--build` vì image đã được cache.

> **Lý do API chạy Docker thay vì `dotnet run`:** Dockerfile cũ bị broken (copy project không tồn tại). Sau khi sửa, toàn bộ stack — API + monitoring — đều chạy trong Docker cùng một network, Prometheus dùng tên service `api:8080` thay vì `host.docker.internal`.

### 1.2 Kiểm tra tất cả 5 container đang chạy

```bash
docker compose -f docker-compose.monitoring.yml ps
```

Kết quả kỳ vọng (tất cả phải `(healthy)`):
```
NAME         STATUS
api          running (healthy)    ← API .NET 8, port 5000
prometheus   running (healthy)
grafana      running (healthy)
influxdb     running (healthy)
cadvisor     running
```

> `api` và `prometheus` có `depends_on` nên start theo thứ tự: influxdb/api → prometheus → grafana.
> Nếu `api` chưa healthy, Prometheus chưa start — đây là đúng thiết kế.

Nếu có container `Exited` → xem log:
```bash
docker logs api          # xem lỗi startup, migration DB
docker logs prometheus
```

### 1.3 Verify từng service

| Service | URL | Kỳ vọng |
|---|---|---|
| **API** | `http://192.168.1.35:5000/health` | `{"status":"Healthy"}` |
| **API metrics** | `http://192.168.1.35:5000/metrics` | Hàng trăm dòng `# HELP ...` |
| Prometheus | `http://192.168.1.35:9090` | Web UI → Status → Targets: `api` = UP |
| Grafana | `http://192.168.1.35:3000` | Login page (admin / admin123) |
| InfluxDB | `http://192.168.1.35:8086/ping` | HTTP 204, không có body |
| cAdvisor | `http://192.168.1.35:8080` | Dashboard container metrics |

---

## Bước 2 — Verify API hoạt động đúng

### 2.1 Xem log API để confirm seed thành công

```bash
docker logs api --follow
```

Chờ thấy 2 dòng quan trọng rồi Ctrl+C:
```
[INF] Đã tạo 100 test users cho k6 load testing   ← WorkerService seed xong
[INF] Application started. Press Ctrl+C to shut down.
```

> WorkerService tự động seed 100 test user vào DB khi container khởi động.
> Migration DB (`MigrateAsync`) cũng chạy tự động — không cần chạy tay.

### 2.2 Verify 3 endpoint cần thiết

```bash
# 1. Health check — k6 gọi trước khi test (setup())
curl http://192.168.1.35:5000/health
# Kỳ vọng: {"status":"Healthy"}

# 2. Metrics endpoint — Prometheus scrape endpoint này mỗi 5 giây
curl http://192.168.1.35:5000/metrics | head -20
# Kỳ vọng: thấy dòng # HELP, # TYPE và số liệu

# 3. Thử login với 1 test user (xác nhận seed thành công)
curl -X POST http://192.168.1.35:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile email roles"
# Kỳ vọng: {"access_token":"eyJ...","token_type":"Bearer","expires_in":3600}
```

### 2.3 Verify Prometheus đang scrape API

1. Mở `http://192.168.1.35:9090`
2. Vào **Status → Targets**
3. Target `dotnet-api` phải có `State = UP` màu xanh

> Prometheus dùng `api:8080` (tên service Docker, port nội bộ) — không phải `localhost:5000`.
> Nếu `DOWN` mà container `api` đang `healthy`: reload Prometheus config bằng `curl -X POST http://192.168.1.35:9090/-/reload`

---

## Bước 3 — Cấu hình Grafana (làm 1 lần duy nhất)

### 3.1 Thêm datasource InfluxDB (cho k6)

> InfluxDB lưu kết quả k6 — cần add để Grafana đọc được.

1. Mở `http://192.168.1.35:3000`, login `admin / admin123`
2. Sidebar trái → **Connections → Data Sources → Add new data source**
3. Chọn **InfluxDB**
4. Điền các trường sau (scroll xuống để thấy hết):

   | Trường | Giá trị | Ghi chú |
   |--------|---------|---------|
   | **URL** | `http://192.168.1.35:8086` | ⚠️ KHÔNG dùng `http://influxdb:8086` — Grafana UI báo "Invalid URL" với hostname không có dấu chấm |
   | **Database** | `k6` | ⚠️ Bắt buộc — để trống sẽ lỗi "database name required" |
   | Query Language | InfluxQL | Giữ mặc định |
   | User / Password | *(để trống)* | InfluxDB 1.8 không cần auth mặc định |

5. Bấm **Save & Test** → phải hiện "datasource is working"

> **Tại sao dùng IP thay hostname?** Grafana frontend validate URL — hostname không có TLD như `influxdb` bị reject ở UI. Dùng IP `192.168.1.35` bypass được validation này. Port 8086 đã map ra ngoài nên Grafana backend kết nối được.

### 3.2 Thêm datasource Prometheus (cho .NET API metrics)

1. **Add new data source → Prometheus**
2. URL: `http://prometheus:9090`
3. **Save & Test**

### 3.3 Import 3 dashboard có sẵn

1. Vào **Dashboards** (sidebar trái)
2. Click nút **New ▾** (góc trên phải) → chọn **Import**
3. Nhập Dashboard ID → **Load** → chọn datasource tương ứng → **Import**

| Dashboard ID | Tên | Datasource | Dùng để xem |
|---|---|---|---|
| `2587` | k6 Load Testing Results | InfluxDB | Kết quả k6 real-time |
| `10915` | ASP.NET Core & Controllers | Prometheus | .NET API metrics |
| `893` | Docker and OS metrics | Prometheus | CPU/RAM container |

> ⚠️ Nút **Import** không hiển thị trực tiếp trên trang Dashboards — phải click **New ▾** trước rồi mới thấy Import trong dropdown.

---

### 3.4 Tìm dashboard sau khi import & xem ngay không cần k6

**Sau khi import, dashboard xuất hiện ở đâu?**

Vào **Dashboards** (sidebar trái) → danh sách giờ có 3 dashboard vừa import. Click tên để mở.

```
Dashboards
  ├── k6 Load Testing Results          ← ID 2587, datasource InfluxDB
  ├── ASP.NET Core & Controllers       ← ID 10915, datasource Prometheus
  └── Docker and OS metrics            ← ID 893, datasource Prometheus
```

---

#### Dashboard 893 — Docker and OS metrics: ⚠️ CẦN node_exporter (chưa có trong setup)

> Dashboard 893 dùng metric `node_*` từ **node_exporter** — **không phải** cAdvisor. Setup hiện tại chưa có node_exporter → toàn bộ N/A là đúng.
>
> Xem hướng dẫn thêm node_exporter ở **section 3.5** bên dưới.

Sau khi thêm node_exporter, các panel sẽ hiện:

| Panel | Ý nghĩa | Ngưỡng cần chú ý |
|---|---|---|
| **CPU Usage** | % CPU máy host | > 80% liên tục → bottleneck |
| **Memory Usage** | RAM máy host đang dùng | Tăng liên tục → leak |
| **Disk space** | Dung lượng đĩa | > 80% → cần dọn |
| **Network I/O** | Bytes gửi/nhận của host | Tăng khi k6 chạy là bình thường |
| **Container uptime** | Container restart không | Restart giữa test → crash |

---

#### Dashboard 10915 — ASP.NET Core & Controllers: cần chọn Instance thủ công

> Datasource Prometheus → data có sẵn **sau khi gọi ít nhất 1 request vào API** và **chọn đúng Instance**.

**Bước bắt buộc trước khi xem:**

```
1. Mở dashboard 10915
2. Đầu trang có 2 dropdown: [Instances ▾] [Controllers ▾]
3. Click [Instances ▾] → chọn "api:8080"
4. Dashboard tự reload → panels hiện data
```

> Nếu dropdown **Instances trống** (không có option nào, chỉ thấy "All") → có 2 khả năng:
> - **Khả năng 1**: Prometheus chưa scrape được API → vào `http://192.168.1.35:9090` → Status → Targets → `dotnet-api` phải **UP** màu xanh.
> - **Khả năng 2**: Biến `instances` trong dashboard settings chưa có query → fix theo hướng dẫn bên dưới.

---

##### Fix biến `instances` khi dropdown chỉ hiện "All" (không có option nào)

**Nguyên nhân thực tế**: Vào variable settings sẽ thấy field **Data source đang bỏ trống** ("Select data source") — không có data source thì query không chạy được dù query đúng cú pháp.

**Cách fix — thao tác trong Grafana UI:**

**Bước 1** — Mở dashboard settings:

```
Góc trên phải dashboard → click icon ✏ Edit → click icon ⚙ Settings
```

**Bước 2** — Vào Variables:

```
Sidebar trái trong Settings → chọn "Variables" → click vào biến "instances"
```

**Bước 3** — Trong section **Query options**, chọn Data source:

Dropdown **Data source** → chọn **Prometheus**

**Bước 4** — Chọn **Query type** (hay bị bỏ qua — đây là lý do Run query không có hiệu ứng gì):

Dropdown **Query type** đang hiện "Select query type" → chọn **"Label values"**

> ⚠️ Query type chưa chọn → Grafana không biết chạy kiểu query nào → Run query im lặng hoàn toàn, Preview of values chỉ hiện "All".
>
> Lưu ý: đừng nhập `label_values(...)` vào field **Regex** — Regex là filter kết quả, không phải nơi nhập query expression.

**Bước 5** — Sau khi chọn "Label values", điền vào các field xuất hiện:

| Field | Giá trị |
|---|---|
| **Label** | `instance` |
| **Metric** | `up` |
| **Label filter** | `job` = `dotnet-api` |

**Bước 6** — Click **Run query** → **Preview of values** ở cuối trang hiện `api:8080` → click **Back to list** → **Save dashboard**

**Kết quả**: Dropdown Instances sẽ hiện `api:8080` thay vì chỉ có "All". Chọn `api:8080` → dashboard tải data bình thường.

**Sau khi chọn Instance, các panel quan trọng:**

| Panel | Ý nghĩa | Ngưỡng cần chú ý |
|---|---|---|
| **Request Rate** | Số request/giây vào API | Baseline ~0 khi chưa có traffic |
| **Request Duration (p95)** | 95% request hoàn thành trong bao lâu | < 500ms = tốt, > 2s = có vấn đề |
| **Error Rate** | Tỷ lệ request lỗi (4xx, 5xx) | Phải = 0% khi idle |
| **GC Collections** | Số lần .NET Garbage Collector chạy | Gen2 tăng liên tục → memory pressure |
| **Active Requests** | Request đang xử lý đồng thời | > 100 khi không có test → leak |
| **Heap Size** | Bộ nhớ .NET heap đang dùng | Tăng không ngừng sau GC → memory leak |

**Verify nhanh Prometheus đang scrape đúng:**

```bash
# Kiểm tra API đang expose metrics
curl http://192.168.1.35:5000/metrics | grep "^http_requests_received_total"

# Kết quả mong đợi (sau khi đã gọi ít nhất 1 request):
# http_requests_received_total{code="200",controller="...",method="GET"} 3
# Nếu không ra dòng nào → UseHttpMetrics() chưa được gọi trong Program.cs
```

Vào `http://192.168.1.35:9090` → Graph → gõ `http_requests_received_total` → Execute → nếu thấy data = Prometheus đang scrape đúng → dashboard 10915 sẽ có data khi chọn Instance.

---

#### Dashboard 2587 — k6 Load Testing Results (chỉ có data khi k6 đang chạy)

> Datasource InfluxDB → **panel trống là bình thường** khi chưa chạy k6.

Khi k6 chạy với `--out influxdb=http://192.168.1.35:8086/k6`, dashboard này tự cập nhật real-time.

**Các panel quan trọng:**

| Panel | Ý nghĩa | Ngưỡng cần chú ý |
|---|---|---|
| **Virtual Users** | Số VU đang chạy | Theo đúng stages trong script |
| **Request Rate** | Số request/giây k6 gửi đi | |
| **Response Time (p95)** | ⭐ **Quan trọng nhất** — p95 latency | < 500ms = pass, > 3s = fail |
| **Request Failed %** | Tỷ lệ request k6 đánh dấu fail | Phải = 0% ở smoke/load test |
| **Checks** | Các assertion trong script pass bao nhiêu % | 100% = hệ thống trả đúng dữ liệu |
| **HTTP Request Duration** | Phân phối thời gian response | Đuôi dài (p99 >> p95) → có slow outlier |

**Cách xem real-time khi k6 đang chạy:**
- Góc trên phải → time range: **Last 5 minutes**
- Bật **Auto refresh: 5s** (icon đồng hồ cạnh time range)

---

#### Workflow đọc dashboard khi load test đang chạy

```
Mở 3 tab browser song song:

Tab 1 — k6 terminal     Xem progress, số liệu tổng từ k6
Tab 2 — Dashboard 2587  Xem latency và error rate theo thời gian
Tab 3 — Dashboard 10915 Xem .NET CPU, GC, heap — correlate với latency

Khi thấy p95 latency tăng trên dashboard 2587:
→ Chuyển sang dashboard 10915
→ Xem GC Collections, Heap Size, CPU có tăng đồng thời không
→ Nếu GC Gen2 tăng đúng lúc latency tăng → GC pressure là nguyên nhân
→ Nếu CPU lên 100% → CPU bottleneck
→ Nếu CPU thấp, GC bình thường nhưng latency cao → SQL Server bottleneck
```

---

### 3.5 Tại sao dashboard hiện N/A — debug theo từng dashboard

> **N/A** = panel query không trả về data. Mỗi dashboard có nguyên nhân khác nhau.

---

#### Dashboard 2587 — k6 Load Testing Results: N/A là bình thường

**Nguyên nhân:** InfluxDB database `k6` chưa có data — k6 chưa chạy lần nào.

```
Dashboard 2587 chỉ có data khi k6 đang chạy với flag:
--out influxdb=http://192.168.1.35:8086/k6

Trước khi chạy k6 → tất cả panel đều "No data" / N/A → ĐÚNG, không phải lỗi.
```

→ **Bỏ qua dashboard này cho đến Bước 4.**

---

#### Dashboard 10915 — ASP.NET Core & Controllers: cần chọn Instance

**Nguyên nhân:** Biến `Instances` ở đầu trang chưa được chọn — dashboard không biết lấy data từ instance nào.

**Cách fix:**

1. Nhìn đầu trang dashboard → thấy dropdown **Instances** đang trống hoặc có icon ⚠️
2. Click vào dropdown **Instances** → chọn `api:8080`
3. Dashboard tự load lại → panels hiện data

> Nếu dropdown `Instances` chỉ hiện "All" (không có option `api:8080`):
>
> **Trường hợp A — Prometheus chưa scrape được API:**
> - Vào `http://192.168.1.35:9090` → Status → Targets
> - Target `dotnet-api` phải là **UP** (màu xanh)
> - Nếu DOWN → `docker logs api --tail 20` để xem API có lỗi không
>
> **Trường hợp B — Biến `instances` trong dashboard settings chưa cấu hình Query type:**
> 1. Click icon ✏ **Edit** → icon ⚙ **Settings** (góc trên phải dashboard)
> 2. Sidebar trái → **Variables** → click biến **instances**
> 3. Section **Query options** → dropdown **Data source** → chọn **Prometheus**
> 4. Dropdown **Query type** đang hiện "Select query type" → chọn **"Label values"** ← hay bị bỏ qua, đây là lý do Run query im lặng
> 5. Các field xuất hiện sau khi chọn "Label values": **Label** = `instance`, **Metric** = `up`, **Label filter**: `job` = `dotnet-api`
> 6. Click **Run query** → **Preview of values** cuối trang hiện `api:8080` → **Back to list** → **Save dashboard**

**Lưu ý thêm:** Dashboard này chỉ hiện data cho các Controller đã được gọi ít nhất 1 request. Nếu chưa gọi API nào → `Controllers` dropdown cũng trống. Thử gọi:

```bash
curl http://192.168.1.35:5000/health
curl http://192.168.1.35:5000/metrics | head -5
```

Sau đó chờ 5-10 giây (scrape interval) → F5 lại dashboard.

---

#### Dashboard 893 — Docker and System Monitoring: thiếu node_exporter

**Nguyên nhân:** Dashboard 893 yêu cầu **node_exporter** để lấy metrics hệ thống (CPU, RAM, Disk, Uptime của máy host). Setup hiện tại **không có node_exporter** — chỉ có cAdvisor.

```
prometheus.yml hiện tại có:
  ✓ job: dotnet-api    → API metrics
  ✓ job: cadvisor      → container metrics

  ✗ job: node_exporter → system metrics (CPU host, RAM host, Disk) — THIẾU
```

Dashboard 893 dùng metric dạng `node_cpu_seconds_total`, `node_memory_*`, `node_filesystem_*` — các metric này chỉ có từ node_exporter. Không có node_exporter → toàn bộ N/A.

**Giải pháp — thêm node_exporter vào `docker/docker-compose.monitoring.yml`:**

```yaml
  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    ports:
      - "9100:9100"
    restart: unless-stopped
```

Thêm vào `docker/prometheus/prometheus.yml`:

```yaml
  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']
```

Áp dụng (đứng trong thư mục `docker/`):

```bash
# Khởi động node-exporter (không cần rebuild các service khác)
docker compose -f docker-compose.monitoring.yml up -d node-exporter

# Reload Prometheus config để nhận scrape job mới
# Cách 1: restart container (luôn dùng được, không mất data vì volume)
docker compose -f docker-compose.monitoring.yml restart prometheus

# Cách 2: curl reload (chỉ dùng được nếu Prometheus đã có flag --web.enable-lifecycle)
# Nếu gặp "Lifecycle API is not enabled" → dùng Cách 1
curl -X POST http://localhost:9090/-/reload
```

> **Lưu ý `Lifecycle API is not enabled`:** Prometheus mặc định tắt HTTP reload API. Cần thêm flag `--web.enable-lifecycle` vào `command:` trong docker-compose để dùng được lệnh curl:
> ```yaml
> command:
>   - '--config.file=/etc/prometheus/prometheus.yml'
>   - '--storage.tsdb.retention.time=7d'
>   - '--web.enable-lifecycle'
> ```
> File `docker-compose.monitoring.yml` đã được cập nhật flag này. Sau lần `up` tiếp theo thì curl reload sẽ hoạt động.

Đợi 15 giây → vào `http://192.168.1.35:9090` → Status → Targets → phải thấy `node-exporter` = **UP**.

---

##### Fix biến `Node` khi filter ⚠️ trống — node_exporter đã UP nhưng dashboard vẫn N/A

**Nguyên nhân:** Dù node_exporter đã được Prometheus scrape, dashboard vẫn hiện N/A nếu biến `Node` ở đầu trang chưa được cấu hình datasource — **giống hệt vấn đề biến `instances` của dashboard 10915**.

**Nhận biết:** Filter **Node** (hoặc **Container Group**) có icon ⚠️ đỏ và không có option nào để chọn, dù Prometheus target đang UP.

**Cách fix — thao tác trong Grafana UI:**

**Bước 1** — Mở dashboard settings:

```
Góc trên phải dashboard → click icon ✏ Edit → click icon ⚙ Settings
```

**Bước 2** — Vào Variables:

```
Sidebar trái trong Settings → chọn "Variables" → click vào biến "node" (hoặc "Node")
```

**Bước 3** — Trong section **Query options**, điền:

| Field | Giá trị |
|---|---|
| **Data source** | Prometheus |
| **Query type** | Label values |
| **Label** | `instance` |
| **Metric** | `up` |
| **Label filter** | `job` = `node-exporter` |

> ⚠️ Nhớ chọn **Query type = "Label values"** trước — nếu để "Select query type" thì Run query sẽ im lặng hoàn toàn (không báo lỗi, không có kết quả).

**Bước 4** — Click **Run query** → **Preview of values** cuối trang hiện `192.168.1.35:9100` → click **Back to list** → **Save dashboard**

**Bước 5** — Quay lại dashboard, chọn filter:

```
Filter Node ▾            → chọn "192.168.1.35:9100"
Filter Container Group ▾ → chọn giá trị trong list (nếu có)
```

Dashboard tự reload → Uptime, Memory, Disk space, Load, Swap hiện data thực.

> **Lưu ý biến `ContainerGroup`:** Nếu filter Container Group cũng có ⚠️, làm tương tự — Variables → click `containergroup` → Data source: Prometheus, Query type: Label values, Label: `container_label_com_docker_compose_service`, Metric: `container_last_seen`.

---

#### cAdvisor — `container_*` metrics hiện N/A: thiếu `privileged: true`

**Nguyên nhân:** cAdvisor chạy **không có** `privileged: true` → không thể đọc `/proc/<pid>/` của các container ở tầng kernel → toàn bộ metric `container_cpu_usage_seconds_total`, `container_memory_usage_bytes`, `container_network_transmit_bytes_total`... trả về 0 hoặc không xuất hiện → Grafana hiện N/A.

**Kiểm tra nhanh:**

```bash
# Nếu lệnh này không ra kết quả (hoặc toàn giá trị 0) → cAdvisor chưa có privileged
curl http://192.168.1.35:8080/metrics | grep container_memory_usage_bytes | grep 'name="api"'

# Kết quả đúng phải có số thực, ví dụ:
# container_memory_usage_bytes{...name="api"...} 245760000
```

Hoặc vào Prometheus → Graph → gõ `container_memory_usage_bytes{name="api"}` → nếu không ra dòng nào → cAdvisor không collect được metrics.

**Cách fix — `docker/docker-compose.monitoring.yml`:**

```yaml
cadvisor:
  image: gcr.io/cadvisor/cadvisor:latest
  container_name: cadvisor
  privileged: true          # ← THÊM — cho phép đọc kernel /proc
  devices:
    - /dev/kmsg             # ← THÊM — đọc kernel ring buffer (OOM, throttle events)
  ports:
    - "8080:8080"
  volumes:
    - /:/rootfs:ro
    - /var/run:/var/run:ro
    - /sys:/sys:ro
    - /var/lib/docker/:/var/lib/docker:ro
  restart: unless-stopped
```

**Tại sao cần `privileged: true`?**

Docker mặc định chặn container đọc `/proc` của container khác — đây là security sandbox. cAdvisor cần đọc `/proc/<pid>/cgroup`, `/proc/<pid>/net/dev`... của *mọi container* trên host để thu thập CPU, RAM, network. Không có `privileged: true`:
- CPU metrics: trả về 0 liên tục
- Memory metrics: không đọc được cgroup memory stats
- Network I/O: không thấy interface của container
- Tất cả Grafana panel dùng `container_*` → N/A

**Tại sao cần `/dev/kmsg`?**

cAdvisor đọc kernel ring buffer qua `/dev/kmsg` để theo dõi sự kiện OOM kill và CPU throttling. Thiếu device này → cAdvisor vẫn start được nhưng log lỗi `Failed to open /dev/kmsg` và một số metric bị thiếu trên kernel mới (Linux 5.x+).

**Áp dụng fix:**

```bash
cd docker
docker compose -f docker-compose.monitoring.yml up -d --force-recreate cadvisor
```

Đợi 15 giây → vào Prometheus → `container_memory_usage_bytes{name="api"}` → phải thấy số liệu thực.

---

#### Tổng hợp — 2 tầng metrics, 2 nguồn khác nhau

> Dashboard 893 hiện N/A toàn bộ khi **thiếu 1 trong 2** nguồn dưới đây. Cần cả hai để dashboard hoạt động đầy đủ.

| Tầng | Nguồn dữ liệu | Metric prefix | Panel trong dashboard 893 | Fix |
|---|---|---|---|---|
| **Container** (Docker) | **cAdvisor** | `container_*` | Containers count, CPU per container, Network per container | Thêm `privileged: true` + `/dev/kmsg` |
| **Host** (OS / máy vật lý) | **node_exporter** | `node_*` | Uptime, Memory, Disk space, Load, Swap | Thêm service `node-exporter` vào compose |

```
Câu hỏi thực tế                         → Dùng nguồn nào?
──────────────────────────────────────────────────────────
"Container api đang dùng bao nhiêu RAM?" → cAdvisor  (container_memory_usage_bytes)
"Máy host còn bao nhiêu RAM trống?"      → node_exporter (node_memory_MemAvailable_bytes)
"Container api dùng bao nhiêu CPU?"      → cAdvisor  (container_cpu_usage_seconds_total)
"Load average của máy host là bao nhiêu?"→ node_exporter (node_load1)
"Disk của container đang dùng bao nhiêu?"→ node_exporter (node_filesystem_avail_bytes)
"Có bao nhiêu container đang chạy?"      → cAdvisor  (container_last_seen)
```

**Verify cả 2 nguồn đều UP:**

```bash
# 1. Kiểm tra Prometheus đang scrape đủ 4 job
curl -s http://192.168.1.35:9090/api/v1/targets | grep -o '"job":"[^"]*"' | sort -u
# Kết quả mong đợi:
# "job":"cadvisor"
# "job":"dotnet-api"
# "job":"node-exporter"

# 2. Kiểm tra nhanh từng nguồn có data không
curl -s 'http://192.168.1.35:9090/api/v1/query?query=node_memory_MemTotal_bytes' | grep -o '"value":\[[^]]*\]'
# Phải ra số gigabytes thực, ví dụ: "value":[1748000000,"16765276160"]

curl -s 'http://192.168.1.35:9090/api/v1/query?query=container_memory_usage_bytes{name="api"}' | grep -o '"value":\[[^]]*\]'
# Phải ra số bytes thực, ví dụ: "value":[1748000000,"245760000"]
```

---

### 3.6 Percentile là gì? P50, P95, P99 — đọc như thế nào?

#### Tại sao không dùng Average (trung bình)?

```
Scenario: 10 requests với response time (ms):
  50, 55, 48, 52, 51, 49, 53, 47, 200, 1500

Average = (50+55+48+52+51+49+53+47+200+1500) / 10 = 210ms

Nhưng 8/10 user thực ra nhận được < 60ms.
2 request bất thường (200ms, 1500ms) kéo average lên 210ms.
→ Average nói "hệ thống chậm 210ms" trong khi 80% user thấy < 60ms.
→ Average bị outlier bóp méo — không đáng tin.
```

**Percentile nói sự thật hơn:**

```
Sắp xếp 10 giá trị từ nhỏ đến lớn:
  47, 48, 49, 50, 51, 52, 53, 55, 200, 1500

P50 (median) = giá trị ở vị trí 50% = 51ms
               → 50% user nhận < 51ms

P90           = giá trị ở vị trí 90% = 200ms
               → 90% user nhận < 200ms, 10% chậm hơn

P95           = giá trị ở vị trí 95% = ~1500ms (với 10 điểm)
               → 95% user nhận nhanh hơn mức này

P99           = 99% user nhận nhanh hơn mức này
               → Chỉ 1/100 user gặp trường hợp xấu nhất
```

---

#### Bảng percentile thực tế

| Percentile | Ý nghĩa | Dùng khi nào |
|---|---|---|
| **P50** (median) | Trải nghiệm của user "trung bình" — 50% nhanh hơn, 50% chậm hơn | Hiểu baseline |
| **P75** | 75% user được serve nhanh hơn mức này | Ít dùng |
| **P90** | 90% user được serve nhanh hơn | Ngưỡng cảnh báo sớm |
| **P95** | ⭐ **Standard SLA** — 95% user được serve nhanh hơn | **Dùng nhiều nhất trong industry** |
| **P99** | 99% user được serve nhanh hơn — đo "tail latency" | API quan trọng, payment, auth |
| **P99.9** (P999) | 999/1000 user được serve nhanh hơn | High-scale: Netflix, Google, trading |
| **P99.99** | 9999/10000 user nhanh hơn | Ultra high-scale: financial systems |

---

#### Trong thực tế dùng P bao nhiêu?

**Startup / Web thông thường:**
```
P95 < 500ms  → Target SLA tiêu chuẩn
P99 < 2s     → Dưới đây user vẫn chịu được

Nếu P95 > 1s → có vấn đề nghiêm trọng cần điều tra
```

**E-commerce (checkout, thanh toán):**
```
P95 < 300ms  → Checkout chậm → cart abandonment tăng
P99 < 1s     → Amazon research: mỗi 100ms chậm hơn = -1% revenue
```

**Auth / Login (như AuthDemo của bạn):**
```
P95 < 200ms  → Login nhanh, UX tốt
P99 < 500ms  → Chấp nhận được
P99 > 1s     → User nghĩ "app đơ" → đăng nhập lại
```

**Microservices (gọi nhau):**
```
P99 < 50ms   → Mỗi service chain 5 hop = 250ms tổng — OK
P99 > 200ms  → Chain 5 hop = 1s tổng — quá chậm
```

**High-frequency trading / Real-time:**
```
P99.9 < 1ms  → Mọi microsecond quan trọng
Dùng P99.9 hoặc P99.99 vì outlier = lost money
```

---

#### Đọc percentile chart trên Grafana như thế nào?

```
Trục Y: response time (ms)
Trục X: thời gian

                P99 ────────────────── 850ms  ← 1% request chậm hơn đây
                P95 ─────────────────  420ms  ← SLA line — theo dõi cái này
                P90 ────────────────── 280ms
                P50 ────────────────── 95ms   ← Trải nghiệm "bình thường"

Khi load tăng (k6 tăng VU):
  P50 tăng nhẹ: 95ms → 120ms       → bình thường, hệ thống chịu được
  P95 tăng vừa: 420ms → 680ms      → cần chú ý
  P99 tăng mạnh: 850ms → 3500ms    → có bottleneck — P99 luôn nhạy hơn P95
```

**"Long tail" — tại sao P99 quan trọng:**

```
P50 = 95ms, P95 = 420ms, P99 = 3500ms

Khoảng cách P95→P99 rất lớn (420ms → 3500ms) = "long tail"
→ Có một số request đặc biệt chậm (có thể: GC pause, DB lock, cold cache)
→ Với 1000 user/phút: 10 user mỗi phút gặp 3.5s wait

P50 = 95ms, P95 = 380ms, P99 = 450ms

Khoảng cách P95→P99 nhỏ = "tight distribution" 
→ Hệ thống consistent, ít outlier — tốt hơn nhiều
```

---

#### Ngưỡng thực tế cho AuthDemo của bạn

Khi chạy load test với 1000 VU, target:

| Metric | Pass | Warning | Fail |
|---|---|---|---|
| **P95 response time** | < 500ms | 500ms–1s | > 1s |
| **P99 response time** | < 2s | 2s–5s | > 5s |
| **Error rate** | < 0.1% | 0.1%–1% | > 1% |
| **P95 login** (`/connect/token`) | < 300ms | 300ms–800ms | > 800ms |
| **P95 read** (`GET /products`) | < 200ms | 200ms–500ms | > 500ms |
| **P95 write** (`POST /orders`) | < 500ms | 500ms–1.5s | > 1.5s |

> k6 script đã có `thresholds` config — nếu vượt ngưỡng, k6 exit code != 0 và in ✗ thay vì ✓.

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
  grafana/k6 run \
    --env BASE_URL=http://192.168.1.35:5000 \
    /scripts/smoke-test.js
```

> API giờ chạy trong Docker và expose port 5000 ra host. k6 kết nối qua IP của server `192.168.1.35:5000` — không cần `host.docker.internal` nữa.

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

Docker (nếu không cài k6 local):
```bash
docker run --rm -i \
  -v ${PWD}/k6:/scripts \
  grafana/k6 run \
    --out influxdb=http://192.168.1.35:8086/k6 \
    --env BASE_URL=http://192.168.1.35:5000 \
    /scripts/load-test.js
```

> Dùng IP của server thay vì `localhost` vì k6 container không nằm trong cùng Docker network với InfluxDB và API. Cả InfluxDB (8086) lẫn API (5000) đều đã expose ra host nên truy cập được qua IP.

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

> API giờ chạy trong Docker container (`api`) nên cAdvisor thấy được. `docker stats` sẽ hiện luôn container `api` cùng với các container monitoring.

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
│ Error rate > 5%?           │──Có──► Xem log API: docker logs api --follow
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
[ ] docker compose ps → 5 container đang running, api và influxdb/prometheus/grafana phải (healthy)
[ ] docker logs api --follow → thấy "Đã tạo 100 test users" và "Application started"
[ ] curl 192.168.1.35:5000/health → {"status":"Healthy"}
[ ] curl 192.168.1.35:5000/metrics → thấy dòng # HELP (không phải 404)
[ ] curl 192.168.1.35:8086/ping → HTTP 204 (InfluxDB healthy, không có body là đúng)
[ ] 192.168.1.35:9090 → Status → Targets → dotnet-api = UP (xanh)
[ ] 192.168.1.35:3000 → Grafana login được, 2 datasource đã add
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
  Prometheus → GET http://api:8080/metrics   ← dùng tên service Docker
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

### cAdvisor trong project này — giờ có ý nghĩa thực sự

> API đã chạy trong Docker container (`api`), nên cAdvisor **thấy được** nó.

cAdvisor sẽ expose metrics cho container `api`:
```
container_cpu_usage_seconds_total{name="api"}       ← CPU của .NET API
container_memory_usage_bytes{name="api"}            ← RAM của .NET API
container_network_transmit_bytes_total{name="api"}  ← Network out
```

Xem trong Grafana dashboard ID 893 → chọn container `api` để thấy resource usage theo thời gian thực trong lúc k6 chạy.

Kết hợp với dashboard ID 10915 (.NET metrics từ Prometheus) cho bức tranh đầy đủ:
- cAdvisor → biết container đang dùng bao nhiêu CPU/RAM ở tầng OS
- Prometheus → biết bên trong .NET: GC bao nhiêu lần, thread pool queue bao nhiêu

### Nhược điểm cAdvisor

- **Privileged access**: Cần mount `/`, `/sys`, `/var/run` → security concern trong production. Chỉ phù hợp môi trường lab/monitoring nội bộ.
- **Overhead nhỏ**: cAdvisor chạy liên tục đọc `/proc` — tốn ~3–5% CPU của host. Không đáng kể với 2 core lab.

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

---

---

# PHẦN 4 — Bảng lệnh Docker Compose thường dùng

> Tất cả lệnh chạy từ thư mục chứa file `docker-compose.monitoring.yml`.

---

## Khởi động / Dừng stack

```bash
# Khởi động toàn bộ stack (lần đầu — build image + start)
docker compose -f docker-compose.monitoring.yml up -d --build

# Khởi động lại (không build lại — image đã cache)
docker compose -f docker-compose.monitoring.yml up -d

# Dừng tất cả container (giữ nguyên volume/data)
docker compose -f docker-compose.monitoring.yml down

# Dừng và XÓA volume (reset sạch data Grafana, InfluxDB, Prometheus)
docker compose -f docker-compose.monitoring.yml down -v

# Restart 1 service cụ thể (ví dụ: api)
docker compose -f docker-compose.monitoring.yml restart api
```

---

## Build image

```bash
# Build lại toàn bộ image (dùng khi đổi code hoặc Dockerfile)
docker compose -f docker-compose.monitoring.yml build

# Build lại KHÔNG dùng cache (khi thay đổi base image, cài package mới)
docker compose -f docker-compose.monitoring.yml build --no-cache

# Build 1 service cụ thể không cache
docker compose -f docker-compose.monitoring.yml build --no-cache api

# Build xong rồi start luôn (kết hợp)
docker compose -f docker-compose.monitoring.yml up -d --build --force-recreate
```

---

## Xem log

```bash
# Xem log realtime của API (Ctrl+C để thoát)
docker logs api --follow

# Xem 50 dòng cuối của API log
docker logs api --tail 50

# Xem log của tất cả service trong compose (realtime)
docker compose -f docker-compose.monitoring.yml logs -f

# Xem log 1 service cụ thể qua compose
docker compose -f docker-compose.monitoring.yml logs -f api
docker compose -f docker-compose.monitoring.yml logs -f prometheus
```

---

## Kiểm tra trạng thái

```bash
# Xem tất cả container đang chạy + trạng thái health
docker compose -f docker-compose.monitoring.yml ps

# Xem CPU/RAM realtime của tất cả container
docker stats

# Xem CPU/RAM dạng bảng gọn (không refresh)
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"

# Kiểm tra network nội bộ Docker (tên service → IP)
docker network inspect docker_default
```

---

## Vào trong container

```bash
# Mở shell bash vào container api
docker exec -it api bash

# Chạy lệnh 1 lần trong container (không mở shell)
docker exec api dotnet --version
docker exec api env | grep ConnectionStrings
```

---

## Dọn dẹp

```bash
# Xóa tất cả image không dùng (giải phóng disk)
docker image prune -a

# Xóa tất cả container đã dừng + image + volume không dùng
docker system prune -a --volumes

# Xem disk Docker đang dùng bao nhiêu
docker system df
```

---

## Workflow thực tế

```
# Lần đầu chạy — hoặc sau khi sửa code/Dockerfile
docker compose -f docker-compose.monitoring.yml up -d --build

# Lần sau (không đổi code)
docker compose -f docker-compose.monitoring.yml up -d

# Sau khi đổi Dockerfile hoặc cài thêm package NuGet
docker compose -f docker-compose.monitoring.yml build --no-cache api
docker compose -f docker-compose.monitoring.yml up -d

# Reset hoàn toàn (xóa data cũ, build từ đầu)
docker compose -f docker-compose.monitoring.yml down -v
docker compose -f docker-compose.monitoring.yml up -d --build --no-cache

# Kiểm tra sau khi start
docker compose -f docker-compose.monitoring.yml ps
docker logs api --tail 30
```

---

---

# PHẦN 5 — OpenIddict Certificate trong Docker: Nguyên nhân lỗi & Fix chuẩn

> Lỗi: `Access to the path '/home/appuser' is denied` khi container `api` khởi động với `ASPNETCORE_ENVIRONMENT=Production`.

---

## Ephemeral là gì?

**"Ephemeral"** = **tạm thời, chỉ sống trong lúc process đang chạy, mất khi process tắt.**

Hình dung như RAM vs đĩa cứng: dữ liệu trong RAM mất khi tắt nguồn, dữ liệu trên đĩa còn mãi.

```
Ephemeral key:
  Container khởi động → .NET sinh key ngẫu nhiên → giữ trong RAM
  Container bị kill   → RAM bị xóa → key mất vĩnh viễn
  Container khởi động lại → .NET sinh key MỚI hoàn toàn khác

Persistent key (cert file):
  Container khởi động → .NET đọc file openiddict.pfx → lấy key → giữ trong RAM
  Container bị kill   → RAM bị xóa, nhưng file .pfx vẫn còn trên đĩa
  Container khởi động lại → .NET đọc lại cùng file → cùng key cũ
```

Ephemeral key và Persistent key đều ký token như nhau trong lúc đang chạy. Sự khác biệt **chỉ xuất hiện sau khi restart**.

---

## Cách dùng hiện tại có đúng cho production không?

**Code đang dùng (sau khi fix):**

```csharp
else  // Production — ASPNETCORE_ENVIRONMENT != Development
{
    var certPath = configuration["OpenIddict:CertPath"];

    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        // Đường dẫn A: có cert file → dùng persistent key ← ĐÚNG cho production thật
        var cert = new X509Certificate2(certPath, certPassword, ...EphemeralKeySet);
        options.AddEncryptionCertificate(cert).AddSigningCertificate(cert);
    }
    else
    {
        // Đường dẫn B: không có cert file → dùng Ephemeral làm fallback ← Hiện tại đang đây
        options.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
    }
}
```

Docker Compose hiện tại **không mount cert file và không set `OpenIddict__CertPath`** → code luôn đi vào đường dẫn B → luôn dùng Ephemeral.

**Đánh giá:**

| Môi trường | Đánh giá | Lý do |
|---|---|---|
| Lab / staging (dự án này) | ✅ Hoàn toàn ổn | k6 login lại từ đầu mỗi lần test, không có user thật |
| Production có user thật | ❌ Không ổn | 3 vấn đề nghiêm trọng bên dưới |

### 3 vấn đề của Ephemeral trong production

**Vấn đề 1 — User bị logout đồng loạt mỗi lần deploy:**

```
14:00 — bạn push code mới, chạy docker compose up → container restart → key mới sinh
14:00 — 500 user đang dùng app → access token của họ được ký bằng key cũ
14:00 — API validate token bằng key mới → key mới ≠ key cũ → 401 Unauthorized
14:00 — Tất cả 500 user nhận thông báo "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại"
       → User tức giận → support ticket tăng vọt
```

**Vấn đề 2 — Không scale được sang nhiều instance:**

Khi chạy 3 instance API sau load balancer:

```
Instance 1 khởi động → sinh key A
Instance 2 khởi động → sinh key B  (khác key A)
Instance 3 khởi động → sinh key C  (khác A và B)

User gửi request:
  Request 1 → Instance 1 → login OK → token được ký bằng key A
  Request 2 → Instance 2 → validate token → dùng key B để verify → FAIL → 401
  Request 3 → Instance 3 → validate token → dùng key C để verify → FAIL → 401

Kết quả: user login thành công nhưng 2/3 request tiếp theo bị 401
→ Lỗi ngẫu nhiên, không tái hiện được, cực kỳ khó debug
```

**Vấn đề 3 — Không audit được khi có incident bảo mật:**

Trong production, khi phát hiện token bị giả mạo hoặc rò rỉ, cần biết "token này được ký bằng key nào, lúc nào, có cần revoke không". Ephemeral key không có lịch sử — không audit được.

### Dự án thiếu gì để production-ready?

Logic code đã đúng — có cả 2 đường dẫn. Chỉ cần bổ sung **2 bước config** (không phải thay đổi code):

**Bước 1** — Tạo cert file (1 lần duy nhất):
```bash
cd docker
chmod +x create-certs.sh && ./create-certs.sh
```

**Bước 2** — Thêm vào docker-compose:
```yaml
services:
  api:
    volumes:
      - ./certs/openiddict.pfx:/app/certs/openiddict.pfx:ro
    environment:
      - OpenIddict__CertPath=/app/certs/openiddict.pfx
      - OpenIddict__CertPassword=YourPassword
```

Sau khi rebuild → code tự chuyển sang đường dẫn A (cert file) thay vì fallback Ephemeral.

---

## Bức tranh tổng thể trước khi đọc chi tiết

Khi OpenIddict phát hành JWT token, nó cần **ký token đó** bằng một private key — giống như đóng dấu vào văn bản. Bên nhận dùng public key để xác minh dấu đó có thật không.

Câu hỏi đặt ra: **private key đó lấy từ đâu và lưu ở đâu?** Đây chính là gốc rễ của vấn đề.

---

## Chuỗi nguyên nhân từng bước

### Bước 1: `ASPNETCORE_ENVIRONMENT=Production` dẫn code vào nhánh nào?

Nhìn vào code:

```csharp
if (environment.IsDevelopment())   // ← kiểm tra biến môi trường này
{
    options.AddEphemeralEncryptionKey();   // Nhánh này chạy khi dev local
}
else                                       // ← mọi giá trị KHÁC Development đều vào đây
{
    options.AddDevelopmentEncryptionCertificate();  // ← đây là thủ phạm
}
```

`IsDevelopment()` trả về `true` chỉ khi `ASPNETCORE_ENVIRONMENT = Development`. Mọi giá trị khác — `Production`, `Staging`, thậm chí `production` (chữ thường) — đều rơi vào `else`.

Dockerfile đang đặt:

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
```

→ Container khởi động → code đi thẳng vào nhánh `else` → gọi `AddDevelopmentEncryptionCertificate()`.

Để xác nhận điều này trên container đang chạy:

```bash
docker exec api printenv ASPNETCORE_ENVIRONMENT
# Output: Production  ← xác nhận code đang chạy nhánh else
```

---

### Bước 2: `AddDevelopmentEncryptionCertificate()` làm gì bên trong?

Đây là điểm nhiều người hiểu nhầm. Tên có chữ "Development" nhưng nó **không phải** chỉ sinh key trong RAM như `AddEphemeral...`.

Method này làm **2 việc**:

```
Việc A: Sinh self-signed X.509 certificate trong bộ nhớ (RAM)
Việc B: Cố LƯU certificate đó xuống đĩa để dùng lại sau khi restart
```

Lý do tồn tại của Việc B: nếu chỉ lưu trong RAM, mỗi lần restart server thì key mới → toàn bộ token cũ không validate được → tất cả user bị logout. Method này giải quyết bằng cách persist cert xuống đĩa, lần sau restart thì đọc lại cert cũ.

**Nghe có vẻ hợp lý — vậy nó lưu xuống đâu?**

Trên **Linux**, .NET lưu certificate vào thư mục X.509 store của user hiện tại:

```
/home/{username}/.dotnet/corefx/cryptography/x509stores/my/
```

Với `appuser`, đường dẫn đầy đủ là:

```
/home/appuser/.dotnet/corefx/cryptography/x509stores/my/
```

Bảng so sánh 2 method:

| Method | Sinh key ở đâu | Lưu key | Sau restart |
|---|---|---|---|
| `AddEphemeralEncryptionKey()` | RAM | Không lưu đi đâu | Key mất, token cũ không validate được |
| `AddDevelopmentEncryptionCertificate()` | RAM | **Cố lưu vào `/home/user/.dotnet/...`** | Key còn → token vẫn hợp lệ |

---

### Bước 3: Tại sao `/home/appuser` không tồn tại?

Nhìn vào Dockerfile:

```dockerfile
RUN groupadd -g 1001 appgroup \
    && useradd -u 1001 -g appgroup -s /usr/sbin/nologin appuser \
    ...
```

Lệnh `useradd` có 2 cách dùng:

```bash
# Không có -m → tạo user nhưng KHÔNG tạo /home/appuser
useradd -u 1001 -g appgroup appuser

# Có -m → tạo user VÀ tạo /home/appuser với đầy đủ quyền
useradd -u 1001 -g appgroup -m appuser
```

Dockerfile dùng cách không có `-m` → `/home/appuser` **không tồn tại** trên filesystem container.

---

### Kết quả của chuỗi 3 bước trên

```
Container khởi động
    → ASPNETCORE_ENVIRONMENT=Production → vào nhánh else
        → AddDevelopmentEncryptionCertificate() được gọi
            → Sinh cert trong RAM ✓
            → Cố tạo thư mục /home/appuser/.dotnet/...
                → /home/appuser không tồn tại (useradd không có -m)
                    → "Access to the path '/home/appuser' is denied" ✗
```

> **Tóm tắt 1 câu:** `AddDevelopmentEncryptionCertificate()` được thiết kế cho máy dev Windows có profile user đầy đủ — không phải cho container Linux non-root không có home directory.

---

## Tại sao cờ EphemeralKeySet bắt buộc khi load cert từ file?

Đây là chi tiết tinh tế nhất mà hầu hết hướng dẫn bỏ qua. Giả sử bạn đã fix bằng cách load cert từ file `.pfx`:

```csharp
// Trông có vẻ đúng...
var cert = new X509Certificate2("/app/certs/openiddict.pfx", "password");
```

Nhưng **vẫn lỗi tương tự**. Tại sao?

Khi `new X509Certificate2(path, password)` chạy, .NET không chỉ đọc file. Nó còn:

```
1. Đọc file .pfx → giải mã → lấy private key vào RAM ✓
2. Tự động CỐ COPY private key vào OS key store
   → trên Linux: /home/appuser/.dotnet/...
   → /home/appuser không tồn tại → lại lỗi quyền ✗
```

Đây là hành vi mặc định kế thừa từ Windows CryptoAPI — vốn thiết kế để protect private key bằng cách lưu vào secure store có ACL. .NET trên Linux giả lập behavior này.

**`EphemeralKeySet` tắt hành vi đó:**

```csharp
// ĐÚNG — giữ private key trong RAM, không cố ghi ra OS key store
var cert = new X509Certificate2(
    "/app/certs/openiddict.pfx",
    "password",
    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
    //                                  ↑ Nói với .NET: private key chỉ sống trong RAM process này
);
```

| Flag | Ý nghĩa |
|---|---|
| `MachineKeySet` | Dùng machine store thay vì user store — tránh phụ thuộc vào home directory |
| `EphemeralKeySet` | Giữ key trong RAM, không persist ra đĩa dù bằng bất kỳ cơ chế nào |

```
Không có EphemeralKeySet:
  Đọc .pfx → key vào RAM → .NET cố copy vào /home/appuser/... → LỖI

Có EphemeralKeySet:
  Đọc .pfx → key vào RAM → xong, không làm gì thêm → OK
```

---

## Fix cấp độ 1 — Docker lab / staging

Dùng khi: Docker lab, staging, CI/CD — chấp nhận token bị mất khi container restart.

Code đã có logic fallback tự động: nếu không có `OpenIddict:CertPath` trong config → dùng Ephemeral:

```csharp
else
{
    // Production: load cert từ file nếu có
    var certPath = configuration["OpenIddict:CertPath"];

    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        // ... load cert từ file
    }
    else
    {
        // Fallback: không có cert file → dùng Ephemeral
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();
    }
}
```

Với Docker lab (không mount cert file, không set env `OpenIddict__CertPath`) → `certPath` rỗng → tự động vào fallback → **không còn lỗi**.

**Hậu quả cần biết:**

```
Kịch bản: deploy version mới → docker compose up → container restart
  → Key mới được sinh trong RAM
  → Tất cả access token và refresh token cũ KHÔNG hợp lệ
  → User đang dùng app bị trả về 401 Unauthorized
  → User phải đăng nhập lại
```

Chấp nhận được cho lab/staging (ít user test). Không chấp nhận được cho production (real users bị log out đồng loạt khi deploy).

---

## Fix cấp độ 2 — Production thực sự

Dùng khi: môi trường production, user không được bị log out khi deploy.

### Bước 1 — Tạo certificate (làm 1 lần duy nhất)

**Trên Windows dùng Git Bash hoặc WSL:**

```bash
# Tạo thư mục chứa cert — sẽ KHÔNG commit lên git
mkdir -p docker/certs

# Sinh private key RSA 2048-bit
openssl genrsa -out docker/certs/openiddict.key 2048

# Tạo self-signed certificate hạn 10 năm
# CN và O chỉ là metadata — đặt gì cũng được, cert này không phải TLS
openssl req -new -x509 \
  -key docker/certs/openiddict.key \
  -out docker/certs/openiddict.crt \
  -days 3650 \
  -subj "/CN=OpenIddict/O=AuthDemo"

# Đóng gói key + cert thành file .pfx có password bảo vệ
openssl pkcs12 -export \
  -out docker/certs/openiddict.pfx \
  -inkey docker/certs/openiddict.key \
  -in docker/certs/openiddict.crt \
  -password pass:MyStrongPass2024
```

Sau khi chạy xong, thư mục `docker/certs/` có 3 file:

```
docker/certs/
  openiddict.key   ← private key (BẢO MẬT — không commit lên git)
  openiddict.crt   ← certificate (public)
  openiddict.pfx   ← gói cả 2 lại + password bảo vệ (đây là file dùng)
```

> Cert này **không phải TLS/HTTPS certificate**. Nó chỉ dùng để ký JWT token nội bộ. Dùng self-signed là hoàn toàn hợp lệ cho mục đích này.

### Bước 2 — Thêm vào .gitignore ngay

```bash
echo "docker/certs/*.pfx" >> .gitignore
echo "docker/certs/*.key" >> .gitignore
echo "docker/certs/*.crt" >> .gitignore
```

File `.pfx` chứa private key — nếu commit lên GitHub là lộ key, bất kỳ ai cũng giả mạo được token.

### Bước 3 — Tạo file `.env` chứa password (cũng không commit)

```bash
# docker/.env
OPENIDDICT_CERT_PASSWORD=MyStrongPass2024
```

```bash
echo "docker/.env" >> .gitignore
```

### Bước 4 — Mount cert vào docker-compose

```yaml
services:
  api:
    volumes:
      # Mount file .pfx vào container, chỉ đọc (:ro = read-only)
      - ./certs/openiddict.pfx:/app/certs/openiddict.pfx:ro
    environment:
      # Dấu __ trong tên env var = dấu : trong config C#
      # OpenIddict__CertPath = configuration["OpenIddict:CertPath"] trong code
      - OpenIddict__CertPath=/app/certs/openiddict.pfx
      - OpenIddict__CertPassword=${OPENIDDICT_CERT_PASSWORD}  # đọc từ file .env
```

### Bước 5 — Verify sau khi deploy

```bash
# Build và start
docker compose -f docker-compose.monitoring.yml up -d --build

# Kiểm tra cert đã được mount vào container chưa
docker exec api ls -la /app/certs/
# Kết quả tốt:
# -r--r--r-- 1 root root 2472 Jun 02 openiddict.pfx  ← có file, read-only

# Kiểm tra env var đã được đọc vào container chưa
docker exec api printenv | grep OpenIddict
# Kết quả tốt:
# OpenIddict__CertPath=/app/certs/openiddict.pfx
# OpenIddict__CertPassword=MyStrongPass2024

# Xem log — phải KHÔNG còn lỗi "Access denied"
docker logs api --tail 30
# Kết quả tốt: thấy "Application started" mà không có exception
```

---

## Hiểu điều gì thay đổi khi deploy version mới

```
Trước khi fix (Ephemeral hoặc AddDevelopmentCertificate thất bại):
  Deploy → container restart → key mới sinh trong RAM
  → Token cũ không hợp lệ → tất cả user bị logout đồng loạt

Sau khi fix (cert file được mount):
  Deploy → container restart → đọc cùng file openiddict.pfx → cùng private key
  → Token cũ vẫn hợp lệ → user không bị logout
```

---

## Tóm tắt quyết định theo môi trường

```
Môi trường                    │ Cấu hình                          │ Hậu quả khi restart
──────────────────────────────│───────────────────────────────────│────────────────────
Local dev (dotnet run)        │ IsDevelopment → AddEphemeral       │ Token mất — OK
Docker lab / staging          │ Không có CertPath → AddEphemeral   │ Token mất — OK
Docker production 1 instance  │ CertPath + cert file               │ Token giữ nguyên
Docker production multi-inst  │ CertPath + cùng 1 cert file        │ Token giữ nguyên
                              │ (mount cùng .pfx vào tất cả inst.) │ (dùng chung 1 key)
```

> **Quy tắc:** Chỉ quan tâm đến persistent cert khi user thật bị ảnh hưởng. Với lab/staging ít user test — ephemeral là đủ.

---

## Script tiện dụng — Tạo cert trong 1 lệnh

Lưu thành `docker/create-certs.sh`, chạy 1 lần khi setup production:

```bash
#!/bin/bash
# Cách dùng: chmod +x create-certs.sh && ./create-certs.sh

CERT_DIR="./certs"
CERT_PASSWORD="ChangeThisToStrongPassword"

mkdir -p "$CERT_DIR"

openssl genrsa -out "$CERT_DIR/openiddict.key" 2048
openssl req -new -x509 \
  -key "$CERT_DIR/openiddict.key" \
  -out "$CERT_DIR/openiddict.crt" \
  -days 3650 \
  -subj "/CN=OpenIddict/O=AuthDemo"
openssl pkcs12 -export \
  -out "$CERT_DIR/openiddict.pfx" \
  -inkey "$CERT_DIR/openiddict.key" \
  -in "$CERT_DIR/openiddict.crt" \
  -password pass:$CERT_PASSWORD

echo "Tạo xong: $CERT_DIR/openiddict.pfx"
echo "Password: $CERT_PASSWORD"
echo ""
echo "Thêm vào .gitignore:"
echo "  docker/certs/*.pfx"
echo "  docker/certs/*.key"
echo "  docker/certs/*.crt"
```

---

## Checklist debug khi gặp lỗi certificate OpenIddict trong Docker

```
Triệu chứng: container api không healthy, log có "Access to the path ... is denied"

Bước 1 — Xác nhận môi trường:
  docker exec api printenv ASPNETCORE_ENVIRONMENT
  → "Production"? → code đang chạy nhánh else của OpenIddictExtensions.cs

Bước 2 — Kiểm tra Dockerfile có tạo home directory không:
  Mở Dockerfile → tìm dòng useradd
  → Có flag -m không? Không có → /home/appuser không tồn tại
  → Đây là lý do AddDevelopmentEncryptionCertificate() không ghi được

Bước 3 — Kiểm tra code đang dùng method nào trong nhánh else:
  Mở OpenIddictExtensions.cs → tìm nhánh else
  → Đang dùng AddDevelopmentEncryptionCertificate()?
  → Sửa thành: nếu có cert file thì load, không thì dùng Ephemeral

Bước 4 — Nếu đã load cert từ file nhưng vẫn lỗi:
  Kiểm tra có cờ EphemeralKeySet không:
  var cert = new X509Certificate2(path, password,
      X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
  → Thiếu cờ này: .NET vẫn cố ghi vào OS keystore dù đọc từ file → vẫn lỗi

Bước 5 — Nếu đã có cờ nhưng cert không load được:
  docker exec api ls -la /app/certs/
  → File openiddict.pfx có ở đây không?
  → Nếu không có: kiểm tra docker-compose volumes đã mount đúng chưa
  docker exec api printenv | grep OpenIddict
  → OpenIddict__CertPath và OpenIddict__CertPassword đã được set chưa?
```

---

---

# PHẦN 6 — Prometheus Web UI & PromQL thực tế

> Prometheus UI tại `http://localhost:9090` — dùng để debug nhanh, kiểm tra metric trước khi đưa lên Grafana.

---

## Prometheus Web UI — Dùng để làm gì?

```
http://localhost:9090
  │
  ├── Status → Targets          Xem target nào UP/DOWN, lần scrape cuối lúc nào
  ├── Status → Configuration    Xem nội dung prometheus.yml đang áp dụng
  ├── Status → Service Discovery Xem Prometheus đang discover service nào
  └── Graph (trang chính)       Gõ PromQL → xem số liệu dạng bảng hoặc đồ thị
```

Tab **Graph** là nơi bạn sẽ dùng nhiều nhất:

```
1. Gõ metric name vào ô Expression  (có autocomplete)
2. Bấm Execute
3. Chọn tab "Table" để xem giá trị tức thời, hoặc "Graph" để xem theo thời gian
4. Điều chỉnh time range góc trên phải (mặc định 1 giờ)
```

> Prometheus UI chỉ dùng để debug/kiểm tra nhanh. Để xem đẹp và lưu layout → dùng Grafana.

---

## Các nhóm PromQL phổ biến thực tế

### 1. HTTP metrics — Đo tải và latency

Các metric này do `prometheus-net.AspNetCore` tự động sinh ra khi bạn gọi `app.UseHttpMetrics()`.

```promql
# Số request mỗi giây (tính trung bình trong 1 phút qua)
rate(http_requests_received_total[1m])

# Tách theo HTTP status code — tìm lỗi 5xx
rate(http_requests_received_total{code=~"5.."}[1m])

# Tách theo route — route nào đang bị gọi nhiều nhất?
sum(rate(http_requests_received_total[1m])) by (controller, action)

# Tổng request đang xử lý đồng thời (in-flight)
http_requests_in_progress

# p95 latency — 95% request hoàn thành trong bao nhiêu giây?
# Đây là query quan trọng nhất để đánh giá performance
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# p95 latency tách theo route
histogram_quantile(0.95,
  sum(rate(http_request_duration_seconds_bucket[5m])) by (le, controller, action)
)

# Tỷ lệ lỗi — nên dưới 1%
sum(rate(http_requests_received_total{code=~"5.."}[1m]))
  /
sum(rate(http_requests_received_total[1m]))
```

**Đọc kết quả:**

| Query | Giá trị bình thường | Dấu hiệu xấu |
|---|---|---|
| `rate(...[1m])` cho req/s | Tùy tải, xem xu hướng | Drop đột ngột |
| p95 latency | < 500ms cho GET, < 2s cho POST | > 3s → bottleneck |
| Error rate | < 0.01 (1%) | > 0.05 (5%) → điều tra ngay |
| `http_requests_in_progress` | < 50 | > 200 → Kestrel đang queue |

---

### 2. .NET process metrics — CPU và bộ nhớ

Các metric này do chính .NET runtime tự động expose — không cần cài thêm gì.

```promql
# Tổng CPU (giây) process đã dùng kể từ khi start — dùng rate() để ra %
process_cpu_seconds_total

# CPU usage trong 1 phút qua (giá trị từ 0 đến số core, ví dụ 0.7 = 70% của 1 core)
rate(process_cpu_seconds_total[1m])

# RAM process đang chiếm (bytes) — đây là RSS, bao gồm cả managed heap + native
process_resident_memory_bytes

# RAM virtual (địa chỉ ảo được map, thường lớn hơn RSS nhiều)
process_virtual_memory_bytes

# Số file descriptor đang mở (socket, file, pipe)
# Nếu tăng liên tục → resource leak
process_open_fds

# Số thread đang chạy
process_num_threads

# Thời gian process đã sống (giây) — dùng để biết khi nào container restart
process_start_time_seconds
```

**Tại sao `rate(process_cpu_seconds_total[1m])` mà không dùng thẳng?**

`process_cpu_seconds_total` là **counter** — chỉ tăng, không bao giờ giảm (như đồng hồ đo điện). Dùng `rate()` để tính tốc độ tăng trong khoảng thời gian → ra số giây CPU dùng mỗi giây thực → gần bằng % CPU.

---

### 3. .NET GC metrics — Theo dõi Garbage Collector

Các metric này từ `prometheus-net` (khi dùng `UseHttpMetrics()`):

```promql
# Số lần GC chạy theo thế hệ (Gen0 chạy nhiều nhất, Gen2 tốn nhất)
dotnet_gc_collections_total

# Tần suất GC — số lần/giây trong 5 phút qua
rate(dotnet_gc_collections_total[5m])

# Kích thước heap theo thế hệ (bytes) — xem gen nào đang chiếm nhiều RAM
dotnet_gc_heap_size_bytes

# Tổng bytes đã được allocate kể từ khi start
dotnet_gc_allocated_bytes_total

# Tốc độ cấp phát bộ nhớ (bytes/giây) — cho biết app đang tạo object nhanh thế nào
rate(dotnet_gc_allocated_bytes_total[1m])
```

**Đọc GC metrics:**

```
Gen0 rate cao → bình thường, GC Gen0 nhanh và thường xuyên
Gen1 rate cao → hơi bất thường, object sống qua 1 lần GC Gen0
Gen2 rate tăng liên tục → nguy hiểm — có object sống rất lâu (có thể memory leak)

dotnet_gc_heap_size_bytes tăng không ngừng dù Gen2 đã chạy
→ Dấu hiệu memory leak — cần profiler (dotnet-dump, dotMemory)
```

---

### 4. cAdvisor container metrics

cAdvisor expose metrics cho **tất cả container Docker** — dùng label `name` để lọc theo tên container.

```promql
# CPU container api dùng trong 1 phút (kết quả từ 0 đến số core)
rate(container_cpu_usage_seconds_total{name="api"}[1m])

# RAM container api đang dùng (bytes)
container_memory_usage_bytes{name="api"}

# RAM container api — chỉ tính working set (loại trừ cache có thể giải phóng)
container_memory_working_set_bytes{name="api"}

# Network bytes gửi đi từ container api (bytes tích lũy, dùng rate() ra bytes/s)
rate(container_network_transmit_bytes_total{name="api"}[1m])

# Network bytes nhận vào container api
rate(container_network_receive_bytes_total{name="api"}[1m])

# Disk I/O — bytes đọc từ đĩa
rate(container_fs_reads_bytes_total{name="api"}[1m])

# Xem tất cả container, không lọc theo tên — sort by RAM
sort_desc(container_memory_usage_bytes{name!=""})
```

**So sánh `process_resident_memory_bytes` (từ .NET) và `container_memory_usage_bytes` (từ cAdvisor):**

```
process_resident_memory_bytes   → RAM mà .NET process thấy từ bên trong
container_memory_usage_bytes    → RAM container thực sự chiếm theo Docker/kernel
                                  (bao gồm cả buffer, cache của container)

Thường: container_memory_usage_bytes >= process_resident_memory_bytes
Nếu cách nhau quá xa → container đang cache nhiều data từ đĩa
```

---

## Các hàm PromQL cốt lõi cần biết

| Hàm | Dùng cho | Ví dụ |
|---|---|---|
| `rate(metric[window])` | Counter → tính tốc độ tăng/giây | `rate(http_requests_received_total[1m])` |
| `increase(metric[window])` | Counter → tổng tăng trong khoảng thời gian | `increase(dotnet_gc_collections_total[1h])` |
| `histogram_quantile(φ, metric)` | Histogram → tính percentile (p50, p95, p99) | `histogram_quantile(0.95, rate(...bucket[5m]))` |
| `sum(metric) by (label)` | Gộp nhiều time series, tách theo label | `sum(rate(...[1m])) by (controller)` |
| `avg(metric)` | Trung bình các instance | `avg(process_cpu_seconds_total)` |
| `max(metric)` | Instance tệ nhất | `max(container_memory_usage_bytes)` |
| `delta(metric[window])` | Gauge → thay đổi trong khoảng thời gian | `delta(process_resident_memory_bytes[10m])` |
| `absent(metric)` | Alert khi metric không còn tồn tại (target DOWN) | `absent(up{job="dotnet-api"})` |

**Phân biệt Counter vs Gauge:**

```
Counter: chỉ tăng, không bao giờ giảm
  → Luôn dùng rate() hoặc increase() để có nghĩa
  → Ví dụ: http_requests_received_total, dotnet_gc_collections_total

Gauge: có thể tăng hoặc giảm
  → Dùng thẳng, không cần rate()
  → Ví dụ: process_resident_memory_bytes, http_requests_in_progress

Histogram: phân phối giá trị thành các "bucket" (rổ)
  → Dùng histogram_quantile() + rate() để ra percentile
  → Ví dụ: http_request_duration_seconds
```

---

## Làm sao biết metric nào tồn tại? Tìm ở đâu?

### Cách 1 — Xem thẳng endpoint `/metrics` của API

```bash
# Xem tất cả metric API đang expose
curl http://localhost:5000/metrics

# Lọc chỉ metric liên quan đến HTTP
curl http://localhost:5000/metrics | grep "^http_"

# Lọc metric liên quan đến .NET GC
curl http://localhost:5000/metrics | grep "^dotnet_gc"

# Lọc theo process
curl http://localhost:5000/metrics | grep "^process_"
```

Mỗi metric có 2 dòng header giải thích:
```
# HELP http_requests_received_total Provides the count of HTTP requests that have been processed by the ASP.NET Core pipeline.
# TYPE http_requests_received_total counter
http_requests_received_total{code="200",method="GET"} 12450
```

Đây là nguồn chính xác nhất — bạn **chỉ có thể dùng metric nào đang được expose**.

### Cách 2 — Autocomplete trong Prometheus UI

Vào `http://localhost:9090` → Graph tab → bắt đầu gõ tên metric → Prometheus tự gợi ý tất cả metric đang có.

Ví dụ: gõ `http_` → sẽ thấy toàn bộ metric HTTP từ API.

### Cách 3 — Prometheus UI → Status → TSDB Status

Trang này liệt kê các metric đang chiếm nhiều bộ nhớ nhất — giúp biết cái gì đang tồn tại:

```
http://localhost:9090/tsdb-status
```

### Cách 4 — Xem metrics cAdvisor expose

```bash
curl http://localhost:8080/metrics | grep "^container_" | grep -o "^[^{]*" | sort -u
```

Lệnh này lấy tên tất cả metric bắt đầu bằng `container_` mà cAdvisor đang expose.

---

## Tìm tài liệu ở đâu?

### 1. Tài liệu chính thức Prometheus

```
https://prometheus.io/docs/prometheus/latest/querying/basics/
  → PromQL syntax, operators, functions

https://prometheus.io/docs/prometheus/latest/querying/functions/
  → Danh sách đầy đủ tất cả hàm PromQL (rate, increase, histogram_quantile, v.v.)

https://prometheus.io/docs/practices/naming/
  → Quy ước đặt tên metric — giúp đoán tên metric mới
```

### 2. Tài liệu prometheus-net (thư viện .NET đang dùng)

```
https://github.com/prometheus-net/prometheus-net
  → README liệt kê tất cả metric mà UseHttpMetrics() tự sinh ra
  → Tên chính xác của từng metric, labels có sẵn
```

Đây là tài liệu quan trọng nhất để biết metric nào có trong dự án .NET — vì tên metric do thư viện định nghĩa, không phải do Prometheus.

### 3. Grafana Explore tab — thực hành tốt nhất

```
http://localhost:3000 → Explore (icon kính lúp bên trái)
  → Chọn datasource Prometheus
  → Có autocomplete và gợi ý label
  → Thử query trực tiếp, kết quả hiện ngay
```

Grafana Explore tốt hơn Prometheus UI vì: autocomplete thông minh hơn, vẽ đồ thị đẹp hơn, có thể so sánh 2 query cạnh nhau.

### 4. Dashboard Grafana.com làm tài liệu tham khảo

Vào `https://grafana.com/grafana/dashboards/` → tìm dashboard bạn đã import (ví dụ ID 10915) → click **Download JSON** → mở file JSON → tìm trường `"expr"`:

```json
"expr": "histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (le, controller, action))"
```

Đây là cách học PromQL nhanh nhất: xem query trong dashboard có sẵn rồi hiểu từng phần.

### 5. Tài liệu cAdvisor metrics

```
https://github.com/google/cadvisor/blob/master/docs/storage/prometheus.md
  → Danh sách đầy đủ tất cả metric cAdvisor expose
  → Ý nghĩa từng metric, unit đo
```

---

## Workflow thực tế khi cần tìm metric mới

```
Câu hỏi: "Tôi muốn xem số lần .NET thread pool bị đầy (thread starvation)"

Bước 1 — Tìm tên metric:
  curl http://localhost:5000/metrics | grep -i "thread"
  → Thấy: dotnet_threadpool_queue_length, dotnet_threadpool_num_threads

Bước 2 — Hiểu loại metric:
  # TYPE dotnet_threadpool_queue_length gauge
  → Gauge → dùng thẳng, không cần rate()

Bước 3 — Thử trong Prometheus UI:
  http://localhost:9090 → Graph → gõ: dotnet_threadpool_queue_length
  → Xem giá trị hiện tại

Bước 4 — Thêm vào Grafana dashboard:
  Dashboard → Edit panel → Query → dán query vào
  → đặt alert nếu > 0 trong hơn 30 giây
```

### Cheat sheet: metric nào dùng cho mục đích gì

```
Mục đích                           │ Metric nên dùng
───────────────────────────────────│─────────────────────────────────────────
Số request/giây                    │ rate(http_requests_received_total[1m])
p95 latency                        │ histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
Tỷ lệ lỗi                          │ rate({code=~"5.."}[1m]) / rate(total[1m])
CPU API process                    │ rate(process_cpu_seconds_total[1m])
RAM API process                    │ process_resident_memory_bytes
GC pressure                        │ rate(dotnet_gc_collections_total{generation="2"}[5m])
Thread pool bão hòa                │ dotnet_threadpool_queue_length
CPU container (qua Docker)         │ rate(container_cpu_usage_seconds_total{name="api"}[1m])
RAM container (qua Docker)         │ container_memory_working_set_bytes{name="api"}
API có còn sống không              │ up{job="dotnet-api"} (1=UP, 0=DOWN)
```

---

---

# PHẦN 7 — Crash Test & Scale Load Balancing Thực Tế

> **Mục tiêu:** Tìm ngưỡng chết của 1 instance trên VM1 → deploy thêm VM2 trong VMware → setup Nginx LB → chạy lại cùng bài test → so sánh kết quả.
>
> **Kịch bản:** Máy bạn chạy VMware Workstation. Tất cả VM trong dải `192.168.1.*`. VM1 đã có sẵn, VM2 sẽ tạo mới.

---

## 7.0 Tổng quan kịch bản & IP Plan

```
GIAI ĐOẠN 1 — 1 máy:

  k6 (laptop/VM)
       │
       ▼ port 5000
  ┌─────────────────────────────────┐
  │  VM1 — 192.168.1.35             │
  │  ┌─────────────────────────┐    │
  │  │  Docker Compose         │    │
  │  │  ├─ api       :5000     │    │
  │  │  ├─ prometheus:9090     │    │
  │  │  ├─ grafana   :3000     │    │
  │  │  ├─ influxdb  :8086     │    │
  │  │  └─ cadvisor  :8080     │    │
  │  └─────────────────────────┘    │
  │  SQL Server (Windows host)      │
  └─────────────────────────────────┘


GIAI ĐOẠN 2 — 2 máy + Load Balancer:

  k6 (laptop/VM)
       │
       ▼ port 80
  ┌───────────────────────────────────────────────────────┐
  │  VM1 — 192.168.1.35                                   │
  │  ┌────────────────────────────────────────────────┐   │
  │  │  nginx-lb (port 80) ← entry point duy nhất     │   │
  │  │       │                    │                   │   │
  │  │  api:5000 (VM1)    192.168.1.36:5000 (VM2)     │   │
  │  │  + monitoring stack                            │   │
  │  └────────────────────────────────────────────────┘   │
  │  SQL Server ← cả 2 instance cùng kết nối vào đây      │
  └───────────────────────────────────────────────────────┘

  ┌─────────────────────────┐
  │  VM2 — 192.168.1.36     │
  │  ├─ Docker: api :5000   │
  │  └─ (không monitoring)  │
  └─────────────────────────┘
```

**IP Plan (thay theo mạng thực của bạn):**

| Máy | IP | Role |
|---|---|---|
| VM1 | `192.168.1.35` | API + Monitoring + Nginx LB + SQL Server |
| VM2 | `192.168.1.36` | API instance thứ 2 (không có monitoring) |
| Laptop/máy test | Bất kỳ trong dải | Chạy k6 |

**Files đã có trong repo:**

| File | Mục đích |
|---|---|
| [k6/crash-test.js](k6/crash-test.js) | Script crash test — tăng VU đến 2000 |
| [docker/nginx.conf](docker/nginx.conf) | Cấu hình Nginx LB |
| [docker/docker-compose.lb.yml](docker/docker-compose.lb.yml) | Service nginx-lb cho VM1 |
| [docker/docker-compose.node2.yml](docker/docker-compose.node2.yml) | Stack API-only cho VM2 |
| [docker/create-certs.sh](docker/create-certs.sh) | Script tạo cert .pfx dùng chung |
| [docker/prometheus/prometheus.yml](docker/prometheus/prometheus.yml) | Đã có comment để bật scrape VM2 |

---

## Giai đoạn 0 — Chuẩn bị VMware Workstation

> Nếu VM1 đã chạy sẵn và bạn chỉ cần tạo VM2, bỏ qua bước này và đi thẳng vào **Giai đoạn 1**.

### Network Mode: Bridged (bắt buộc để lấy IP 192.168.1.*)

Trong VMware Workstation:

```
Edit → Virtual Network Editor → VMnet0
  → Chọn "Bridged" → Automatic (hoặc chọn card mạng vật lý của bạn)
```

Với mỗi VM:
```
VM Settings → Network Adapter → Bridged (Replicate physical network connection state)
```

Sau khi vào VM, verify IP đúng dải:
```bash
ip addr show | grep "inet 192"
# Phải thấy: inet 192.168.1.XX/24
```

> Nếu không thấy IP 192.168.1.* mà thấy 172.x.x.x → đang dùng NAT, không phải Bridged.
> Đổi lại sang Bridged và restart VM.

### Specs khuyên dùng cho từng VM

| | VM1 | VM2 |
|---|---|---|
| OS | Ubuntu Server 22.04 LTS | Ubuntu Server 22.04 LTS |
| CPU | 2–4 cores | 2–4 cores |
| RAM | 8–12 GB (SQL Server cần ~4–6 GB) | 4–6 GB |
| Disk | 40–80 GB | 20–40 GB |
| Network | Bridged | Bridged |

> **Tip VMware:** Snapshot VM1 trước khi bắt đầu — nếu có lỗi bạn có thể rollback nhanh mà không cần cài lại.

---

## Giai đoạn 1 — Deploy 1 máy + Crash Test

> Tất cả lệnh trong giai đoạn này chạy trên **VM1**.

### Step 1 — Tạo cert dùng chung

Cert này sẽ dùng chung cho cả VM1 và VM2. Tạo 1 lần duy nhất trên VM1.

**Tại sao cần cert file?** Nếu dùng Ephemeral key (mặc định khi chạy 1 instance), mỗi instance tự sinh key riêng trong RAM. Khi scale lên 2 instance:

```
VM1 sinh key A (trong RAM)
VM2 sinh key B (trong RAM, khác A)

Token được ký bởi VM1 (key A)
  → Request đến VM2 → VM2 dùng key B để verify → FAIL 401

Kết quả trong k6: error rate ~50%, lỗi ngẫu nhiên, cực kỳ khó debug
```

**Giải pháp:** Cả 2 instance dùng cùng 1 file `.pfx`:

```bash
# Trên VM1, từ thư mục gốc project
cd ~/projects/OpenIdDict_MrGold/docker

chmod +x create-certs.sh
./create-certs.sh "OpenIddict@2024"
```

Output kỳ vọng:
```
[1/3] Sinh RSA 2048-bit private key...
[2/3] Tạo self-signed certificate (10 năm)...
[3/3] Đóng gói thành .pfx...

Tạo xong: ./certs/openiddict.pfx
  Password: OpenIddict@2024
```

Bảo vệ cert (KHÔNG commit lên git):
```bash
echo "docker/certs/" >> ../.gitignore
```

Tạo file `.env` để docker-compose đọc password:
```bash
echo "OPENIDDICT_CERT_PASSWORD=OpenIddict@2024" > .env
```

---

### Step 2 — Cập nhật docker-compose dùng cert

Mở [docker/docker-compose.monitoring.yml](docker/docker-compose.monitoring.yml), tìm service `api` và thêm:

```yaml
  api:
    # ... giữ nguyên các dòng hiện tại ...
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=...
      # ↓ THÊM 2 dòng này
      - OpenIddict__CertPath=/app/certs/openiddict.pfx
      - OpenIddict__CertPassword=${OPENIDDICT_CERT_PASSWORD}
    volumes:
      - api-logs:/app/logs
      # ↓ THÊM dòng này
      - ./certs/openiddict.pfx:/app/certs/openiddict.pfx:ro
```

---

### Step 3 — Khởi động toàn bộ stack VM1

```bash
# Từ thư mục docker/
cd ~/projects/OpenIdDict_MrGold/docker

# Lần đầu: build image + khởi động
docker compose -f docker-compose.monitoring.yml up -d --build

# Lần sau (không thay đổi code): bỏ --build
docker compose -f docker-compose.monitoring.yml up -d
```

Chờ ~2–3 phút, kiểm tra trạng thái:
```bash
docker compose -f docker-compose.monitoring.yml ps
```

Kết quả kỳ vọng:
```
NAME         STATUS
api          running (healthy)
prometheus   running (healthy)
grafana      running (healthy)
influxdb     running (healthy)
cadvisor     running
```

Nếu `api` bị `Exited` → xem log nguyên nhân:
```bash
docker logs api --tail 30
```

---

### Step 4 — Verify VM1 hoạt động đúng

**4a. Verify cert được load (không phải Ephemeral fallback):**

```bash
docker logs api --tail 20 | grep -iE "(cert|error|access denied|started)"
# Tốt: thấy "Application started" và KHÔNG thấy "Access to the path"

docker exec api printenv | grep OpenIddict
# Kết quả tốt:
# OpenIddict__CertPath=/app/certs/openiddict.pfx
# OpenIddict__CertPassword=OpenIddict@2024
```

**4b. Verify các endpoint cần thiết:**

```bash
# 1. Health check
curl http://192.168.1.35:5000/health
# Kỳ vọng: {"status":"Healthy"}

# 2. Metrics endpoint — Prometheus scrape cái này mỗi 5 giây
curl http://192.168.1.35:5000/metrics | head -5
# Kỳ vọng: thấy # HELP ... # TYPE ...

# 3. Thử login với test user
curl -s -X POST http://192.168.1.35:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile email roles" \
  | jq .access_token
# Kỳ vọng: chuỗi JWT dài (eyJ...)
```

**4c. Verify Prometheus đang scrape API:**

```
Mở: http://192.168.1.35:9090 → Status → Targets
→ dotnet-api: State = UP (màu xanh)
```

**4d. Cấu hình Grafana (làm 1 lần):**

> Xem hướng dẫn chi tiết tại **Bước 3 — Cấu hình Grafana** trong Phần 2.
>
> Tóm tắt nhanh: Add datasource InfluxDB (URL: `http://192.168.1.35:8086`, DB: `k6`) + datasource Prometheus (`http://prometheus:9090`) → Import 3 dashboard: **2587**, **10915**, **893**.

**4e. Mở SSMS và chuẩn bị query monitoring SQL:**

```sql
-- Query 1: Top query chậm đang chạy (chạy lại nhiều lần trong khi test)
SELECT TOP 10
    r.session_id, r.status, r.wait_type,
    r.wait_time / 1000.0 AS wait_sec,
    r.total_elapsed_time / 1000.0 AS elapsed_sec,
    r.logical_reads,
    SUBSTRING(t.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS query_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50
ORDER BY r.total_elapsed_time DESC;

-- Query 2: Số connection đang mở
SELECT DB_NAME(dbid) AS db, COUNT(*) AS connections
FROM sys.sysprocesses WHERE dbid > 0
GROUP BY dbid ORDER BY connections DESC;
```

---

### Step 5 — Chạy Crash Test lần 1 (tìm ngưỡng chết)

> Crash test chạy ~12 phút. Trong lúc đó mở Grafana song song để xem trực tiếp.

Mở 3 cửa sổ/tab song song:

```
Cửa sổ 1 — Terminal: chạy k6
Cửa sổ 2 — Grafana dashboard 2587 (k6): xem VU, p95, error rate
Cửa sổ 3 — Grafana dashboard 10915 (.NET): chọn instance "api:8080" → xem CPU, GC, Heap
```

Chạy crash test:

```bash
# Từ thư mục gốc project (không phải docker/)
cd ~/projects/OpenIdDict_MrGold

# Nếu đã cài k6 trên VM1
k6 run \
  --out influxdb=http://192.168.1.35:8086/k6 \
  --env BASE_URL=http://192.168.1.35:5000 \
  k6/crash-test.js
```

Nếu chưa cài k6, dùng Docker:
```bash
docker run --rm -i \
  --network host \
  -v ${PWD}/k6:/scripts \
  grafana/k6 run \
    --out influxdb=http://192.168.1.35:8086/k6 \
    --env BASE_URL=http://192.168.1.35:5000 \
    /scripts/crash-test.js
```

Hoặc chạy k6 từ laptop (nếu laptop trong cùng mạng 192.168.1.*):
```bash
# Trên laptop có k6
k6 run \
  --out influxdb=http://192.168.1.35:8086/k6 \
  --env BASE_URL=http://192.168.1.35:5000 \
  k6/crash-test.js
```

---

### Step 6 — Đọc kết quả & ghi ngưỡng quan trọng

**6 giai đoạn bạn sẽ thấy trong Grafana khi VU tăng dần:**

```
══════════════════════════════════════════════════════════════════
Giai đoạn 1 — HEALTHY (0 → 200 VU, ~0–3 phút đầu)
══════════════════════════════════════════════════════════════════
  Grafana 2587:
    ● Virtual Users: tăng đều
    ● p95 latency:   < 300ms  ← API xử lý kịp
    ● Error rate:    0%

  Grafana 10915 (chọn instance api:8080):
    ● CPU:           < 40%
    ● GC Gen0:       tăng bình thường (đây là bình thường)
    ● Thread Pool Queue: = 0

  k6 terminal:
    ✓ login ok:     100%
    ✓ products ok:  100%

══════════════════════════════════════════════════════════════════
Giai đoạn 2 — DEGRADED (200 → 600 VU, ~3–6 phút)
══════════════════════════════════════════════════════════════════
  Grafana 2587:
    ● p95 latency:  500ms → 2s  ← TĂNG NHANH = điểm "knee"
    ● Error rate:   0.5–3%

  Grafana 10915:
    ● CPU:          60–85%
    ● GC Gen2:      bắt đầu tăng
    ● Thread Pool Queue: > 0 đôi lúc

  ⚠️ GHI LẠI: số VU khi p95 vượt 1s = _____ VU
              đây là "ngưỡng degraded" của hệ thống

══════════════════════════════════════════════════════════════════
Giai đoạn 3 — COLLAPSE (600 → 1500+ VU, ~6–9 phút)
══════════════════════════════════════════════════════════════════
  Grafana 2587:
    ● p95 latency:  > 10s  ← server treo
    ● Error rate:   > 30%
    ● Request rate: giảm (k6 không gửi kịp vì timeout)

  Grafana 10915:
    ● CPU:          100% liên tục
    ● GC Gen2:      tăng không ngừng
    ● Heap:         tăng không dừng sau GC

  k6 terminal:
    ✗ login ok:     < 50%  ← quá nửa login thất bại

  ⚠️ GHI LẠI: số VU khi error rate vượt 30% = _____ VU
              đây là "ngưỡng collapse"

══════════════════════════════════════════════════════════════════
Giai đoạn 4 — RAMP DOWN (VU → 0, ~9–12 phút)
══════════════════════════════════════════════════════════════════
  Kịch bản A — Self-recover (tốt):
    Sau 30–60s khi VU = 0 → p95 giảm về < 500ms
    → Hệ thống tự hồi phục, không cần can thiệp

  Kịch bản B — Không recover (cần xử lý):
    VU = 0 nhưng error rate vẫn > 0, p95 vẫn cao
    → Connection pool bị kẹt hoặc GC đang làm việc nặng
    → Chạy: docker restart api
    → Sau đó chờ 30s → verify health lại
```

**Output k6 terminal mẫu khi hệ thống collapse:**

```
✗ login ok............: 41.2%  ✓ 8234   ✗ 11766
✗ products ok.........: 38.7%
✗ order ok............: 22.1%  ✓ 3340   ✗ 11720

crash_login_ok........: 41.20%   ← giảm từ 100%
crash_timeout.........: 31.40%   ← 31% request không nhận được response trong 20s
crash_5xx.............: 18.30%   ← 18% nhận 500/503

http_req_duration......: avg=12.3s  p(50)=9.8s  p(95)=28.1s
http_req_failed........: 58.80%
http_req_blocked.......: avg=4.2s   ← chờ TCP connection cực lâu → pool cạn

vus....................: 1847  max=2000
```

**Điền vào bảng trước khi chuyển giai đoạn 2:**

```
╔══════════════════════════════════════════════════════╗
║  KẾT QUẢ CRASH TEST 1 (1 INSTANCE)                  ║
╠══════════════════════════════════════════════════════╣
║  Ngưỡng degraded (p95 > 1s):    _____ VU            ║
║  Ngưỡng collapse (error > 30%): _____ VU            ║
║  Max throughput:                _____ req/s          ║
║  p95 latency tại 300 VU:        _____ ms            ║
║  Self-recover sau khi hạ tải:   CÓ / KHÔNG          ║
╚══════════════════════════════════════════════════════╝
```

---

## Giai đoạn 2 — Setup VM2 trong VMware

### Step 7 — Tạo VM2 trong VMware Workstation

**7a. Tạo VM mới:**

```
VMware Workstation → File → New Virtual Machine
  → Typical (Recommended) → Next
  → Installer disc image file (iso) → chọn file Ubuntu 22.04 Server .iso
  → Next → đặt tên VM: "OpenIdDict-Node2"
  → Disk size: 30 GB → Store as single file
  → Customize Hardware:
      Memory: 4096 MB (4 GB)
      Processors: 2 cores
      Network Adapter: Bridged (Replicate physical connection)
  → Finish
```

**7b. Cài Ubuntu Server 22.04:**

```
Trong quá trình cài đặt:
  - Language: English
  - Keyboard: English (US)
  - Ubuntu Server (không cần Ubuntu Server minimized)
  - Network: để tự lấy DHCP → sau đó set IP tĩnh
  - Disk: Use entire disk → Done
  - Profile: đặt username và password (ví dụ: user / password)
  - Install OpenSSH server: ✓ Bật (để SSH từ VM1 sang)
  - Featured server snaps: bỏ qua tất cả → Done
  - Chờ cài xong → Reboot
```

**7c. Set IP tĩnh cho VM2 (quan trọng — không để DHCP):**

```bash
# Sau khi login vào VM2
sudo nano /etc/netplan/00-installer-config.yaml
```

Sửa thành:
```yaml
network:
  version: 2
  ethernets:
    ens33:                          # Kiểm tra tên interface: ip link show
      dhcp4: no
      addresses:
        - 192.168.1.36/24           # IP tĩnh cho VM2
      gateway4: 192.168.1.1         # Gateway của mạng bạn
      nameservers:
        addresses: [8.8.8.8, 8.8.4.4]
```

```bash
sudo netplan apply

# Verify IP
ip addr show
# Phải thấy: inet 192.168.1.36/24

# Test kết nối đến VM1
ping 192.168.1.35 -c 3
# Phải thấy reply
```

> **Tên interface có thể khác:** Dùng `ip link show` để xem tên thực tế (thường là `ens33`, `ens32`, `eth0`, hoặc `enp0s3`).

---

### Step 8 — Cài Docker trên VM2

```bash
# Chạy trên VM2
# Cài Docker Engine (không phải Docker Desktop)
curl -fsSL https://get.docker.com | sudo sh

# Thêm user hiện tại vào group docker (không cần sudo mỗi lần)
sudo usermod -aG docker $USER

# Logout và login lại để group có hiệu lực
exit
# → SSH lại vào VM2

# Verify Docker hoạt động
docker run hello-world
# Phải thấy "Hello from Docker!"

docker compose version
# Phải thấy Docker Compose version 2.x.x
```

---

### Step 9 — Copy source code + cert sang VM2

> Chạy các lệnh này từ **VM1**, SSH sang VM2.

**9a. Copy toàn bộ source code:**

```bash
# Trên VM1
# Thay "user" bằng username trên VM2
# Thay 192.168.1.36 bằng IP thực của VM2

scp -r ~/projects/OpenIdDict_MrGold user@192.168.1.36:~/projects/
```

Hoặc nếu chưa có git trên VM2:
```bash
# Trên VM2 — clone từ git repo
sudo apt install -y git
git clone <repo-url> ~/projects/OpenIdDict_MrGold
```

**9b. Copy cert sang VM2 (QUAN TRỌNG):**

```bash
# Trên VM1
# Tạo thư mục certs trên VM2
ssh user@192.168.1.36 "mkdir -p ~/projects/OpenIdDict_MrGold/docker/certs"

# Copy file cert
scp docker/certs/openiddict.pfx user@192.168.1.36:~/projects/OpenIdDict_MrGold/docker/certs/

# Verify
ssh user@192.168.1.36 "ls -la ~/projects/OpenIdDict_MrGold/docker/certs/"
# Phải thấy: openiddict.pfx
```

**9c. Tạo file .env trên VM2:**

```bash
# Trên VM2
cd ~/projects/OpenIdDict_MrGold/docker
echo "OPENIDDICT_CERT_PASSWORD=OpenIddict@2024" > .env

# Verify
cat .env
# OPENIDDICT_CERT_PASSWORD=OpenIddict@2024
```

**9d. Cập nhật connection string trỏ vào SQL Server trên VM1:**

Mở [docker/docker-compose.node2.yml](docker/docker-compose.node2.yml), kiểm tra phần `ConnectionStrings__DefaultConnection`:

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Server=192.168.1.35,1433;Database=OpenIdDict;...
```

> SQL Server chạy trên VM1 (`192.168.1.35`). VM2 phải dùng IP này, không phải `localhost`.

---

### Step 10 — Khởi động API trên VM2

```bash
# Trên VM2
cd ~/projects/OpenIdDict_MrGold/docker

# Build và chạy (lần đầu mất ~3–5 phút)
docker compose -f docker-compose.node2.yml up -d --build
```

Theo dõi log:
```bash
docker logs api --follow
```

Chờ thấy **cả 2 dòng** sau rồi Ctrl+C:
```
[INF] Checking database migration status...
[INF] Database already up-to-date, skipping migration.    ← DB đã migrate từ VM1
[INF] Users already exist, skipping seed.                  ← Users đã có từ VM1
[INF] Application started. Press Ctrl+C to shut down.
```

Nếu thấy lỗi sau:
```
# Lỗi kết nối SQL: timeout / refused
→ Kiểm tra firewall trên VM1 cho phép port 1433
→ Trên VM1: sudo ufw allow 1433 (nếu dùng ufw)
→ Hoặc: SQL Server phải bind 0.0.0.0, không phải chỉ localhost

# Lỗi cert: "Access to the path is denied"
→ File .pfx chưa copy hoặc quyền sai
→ ls -la docker/certs/openiddict.pfx
→ chmod 644 docker/certs/openiddict.pfx
```

---

### Step 11 — Verify VM2 API healthy

```bash
# Từ VM1 hoặc laptop (kiểm tra VM2 accessible từ bên ngoài)
curl http://192.168.1.36:5000/health
# {"status":"Healthy"}

curl http://192.168.1.36:5000/metrics | head -5
# # HELP ... # TYPE ... → metrics đang expose

# Test login trực tiếp vào VM2
curl -s -X POST http://192.168.1.36:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile email roles" \
  | jq -r '.access_token | .[0:60]'
# Phải thấy chuỗi JWT (eyJ...)
```

---

## Giai đoạn 3 — Setup Nginx Load Balancer trên VM1

### Step 12 — Cấu hình nginx.conf với IP thực

Mở [docker/nginx.conf](docker/nginx.conf), sửa IP VM2:

```nginx
upstream api_backend {
    least_conn;                                         # Chia đều connection (không phải request)
    server 192.168.1.35:5000 max_fails=3 fail_timeout=30s;  # VM1 — API instance 1
    server 192.168.1.36:5000 max_fails=3 fail_timeout=30s;  # VM2 — đổi IP này
    keepalive 64;
}
```

> `least_conn` tốt hơn `round_robin` mặc định: khi VM1 đang xử lý request lâu (GC pause), Nginx sẽ chuyển request sang VM2 thay vì tiếp tục gửi vào VM1 đang bận.

---

### Step 13 — Khởi động Nginx LB

```bash
# Trên VM1
cd ~/projects/OpenIdDict_MrGold/docker

docker compose -f docker-compose.monitoring.yml \
               -f docker-compose.lb.yml up -d nginx-lb
```

Kiểm tra:
```bash
docker ps | grep nginx-lb
# nginx-lb   Up X seconds (healthy)
```

---

### Step 14 — Verify LB phân phối đến cả 2 node

**14a. Health check qua LB:**

```bash
curl http://192.168.1.35/lb-health
# nginx-lb-ok
```

**14b. Gọi nhiều lần, xem log Nginx để thấy request đến cả 2 node:**

```bash
# Gọi 10 lần qua LB
for i in {1..10}; do
  curl -s http://192.168.1.35/health
  echo ""
done

# Xem Nginx access log
docker logs nginx-lb --tail 20
```

Kết quả log mong đợi (thấy 2 upstream IP xen kẽ):
```
192.168.1.100 - "GET /health" 200 - 0.013s "upstream: 192.168.1.35:5000"
192.168.1.100 - "GET /health" 200 - 0.015s "upstream: 192.168.1.36:5000"
192.168.1.100 - "GET /health" 200 - 0.012s "upstream: 192.168.1.35:5000"
192.168.1.100 - "GET /health" 200 - 0.014s "upstream: 192.168.1.36:5000"
```

> Nếu tất cả request đều đến 1 node → `least_conn` chọn node ít connection hơn — khi idle, đây là bình thường. Sẽ thấy phân phối rõ hơn khi có tải.

---

## Giai đoạn 4 — Verify Token Cross-Instance

### Step 15 — Test token được chấp nhận trên cả 2 instance

Đây là bước quan trọng nhất trước khi test: xác nhận token ký trên VM1 hợp lệ trên VM2.

```bash
# Bước 1: Lấy token (request sẽ đến VM1 hoặc VM2 ngẫu nhiên qua LB)
TOKEN=$(curl -s -X POST http://192.168.1.35:80/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile email roles" \
  | jq -r .access_token)

echo "Token nhận được: ${TOKEN:0:60}..."

# Bước 2: Gọi trực tiếp vào VM1 (BYPASS LB — port 5000)
echo "=== Test VM1 (trực tiếp) ==="
curl -s -o /dev/null -w "HTTP Status: %{http_code}\n" \
  -H "Authorization: Bearer $TOKEN" \
  http://192.168.1.35:5000/api/products?pageSize=1
# Kỳ vọng: HTTP Status: 200

# Bước 3: Gọi trực tiếp vào VM2 (BYPASS LB — port 5000)
echo "=== Test VM2 (trực tiếp) ==="
curl -s -o /dev/null -w "HTTP Status: %{http_code}\n" \
  -H "Authorization: Bearer $TOKEN" \
  http://192.168.1.36:5000/api/products?pageSize=1
# Kỳ vọng: HTTP Status: 200
```

**Kết quả:**

```
Cả 2 đều 200 → cert đồng bộ đúng → SẴN SÀNG load test

VM1 = 200, VM2 = 401 → cert không khớp → kiểm tra lại:
  1. ls -la docker/certs/ trên VM2 → file pfx có tồn tại không?
  2. docker exec api printenv | grep OpenIddict → có CertPath không?
  3. docker logs api | grep -i cert → có lỗi cert không?
  4. Nếu cần: copy lại pfx từ VM1 sang VM2 và rebuild

VM1 = 200, VM2 = 404 → endpoint /api/products chưa seed dữ liệu
  → docker logs api trên VM2 → có dòng "Application started" chưa?
```

---

## Giai đoạn 5 — Crash Test 2 & So Sánh

### Step 16 — Update Prometheus scrape VM2

Mở [docker/prometheus/prometheus.yml](docker/prometheus/prometheus.yml), bỏ comment đoạn scrape VM2:

```yaml
  - job_name: 'dotnet-api-vm2'
    static_configs:
      - targets: ['192.168.1.36:5000']   # ← đổi thành IP thực của VM2
    metrics_path: '/metrics'
```

Reload Prometheus (không cần restart, không mất dữ liệu):
```bash
curl -X POST http://192.168.1.35:9090/-/reload
# Nếu không có gì xuất hiện → thành công (HTTP 200 không có body)
```

Verify cả 2 target đang UP:
```
Mở: http://192.168.1.35:9090 → Status → Targets

Kỳ vọng:
  ● dotnet-api      State=UP  (api:8080)           ← VM1
  ● dotnet-api-vm2  State=UP  (192.168.1.36:5000)  ← VM2
```

Giờ trong Grafana dashboard 10915, dropdown **Instances** sẽ có cả `api:8080` và `192.168.1.36:5000` — bạn có thể mở 2 tab để so sánh load giữa 2 node trong thời gian thực.

---

### Step 17 — Chạy Crash Test lần 2 qua Load Balancer

Mở 4 cửa sổ/tab song song:

```
Cửa sổ 1 — Terminal: chạy k6
Cửa sổ 2 — Grafana 2587: tổng quan — p95, error rate qua LB
Cửa sổ 3 — Grafana 10915 (Instance = api:8080): CPU/GC VM1
Cửa sổ 4 — Grafana 10915 (Instance = 192.168.1.36:5000): CPU/GC VM2
```

Chạy crash test — **chỉ thay BASE_URL sang port 80 (Nginx LB)**:

```bash
k6 run \
  --out influxdb=http://192.168.1.35:8086/k6 \
  --env BASE_URL=http://192.168.1.35:80 \
  k6/crash-test.js
```

Hoặc qua Docker:
```bash
docker run --rm -i \
  --network host \
  -v ${PWD}/k6:/scripts \
  grafana/k6 run \
    --out influxdb=http://192.168.1.35:8086/k6 \
    --env BASE_URL=http://192.168.1.35:80 \
    /scripts/crash-test.js
```

**Quan sát trong lúc test chạy:**

```
Bình thường khi có 2 node:
  ● Grafana 10915 VM1: CPU ~50%, GC bình thường
  ● Grafana 10915 VM2: CPU ~50%, GC bình thường
  ● Nginx phân phối đều → cả 2 node cùng chịu tải

Dấu hiệu Nginx đang hoạt động đúng:
  ● VM1 CPU + VM2 CPU ≈ nhau → least_conn đang cân bằng tốt
  ● Nếu 1 node GC pause → node kia tạm nhận thêm request → node GC xong → về cân bằng

Dấu hiệu vẫn có vấn đề:
  ● Cả 2 node CPU 100% → SQL Server là bottleneck (bottleneck chuyển tầng)
  ● error rate cao ngay từ đầu → kiểm tra lại nginx.conf và token cross-instance
```

---

### Step 18 — Đọc và so sánh kết quả

**Điền vào bảng so sánh:**

```
╔════════════════════════════════════════════════════════════════════╗
║  SO SÁNH CRASH TEST 1 vs CRASH TEST 2                             ║
╠══════════════════════════════╦═══════════════╦════════════════════╣
║  Metric                      ║ 1 Instance    ║ 2 Instance (LB)    ║
╠══════════════════════════════╬═══════════════╬════════════════════╣
║  Ngưỡng degraded (p95 > 1s)  ║ _____ VU      ║ _____ VU           ║
║  Ngưỡng collapse (err > 30%) ║ _____ VU      ║ _____ VU           ║
║  p95 latency tại 300 VU      ║ _____ ms      ║ _____ ms           ║
║  p95 latency tại 500 VU      ║ _____ ms      ║ _____ ms           ║
║  Max throughput (req/s)       ║ _____ req/s   ║ _____ req/s        ║
║  Self-recover sau test        ║ CÓ / KHÔNG   ║ CÓ / KHÔNG        ║
╚══════════════════════════════╩═══════════════╩════════════════════╝
```

**Kết quả lý tưởng (so sánh lý thuyết vs thực tế):**

| Metric | Lý thuyết (2x) | Thực tế |
|---|---|---|
| Ngưỡng degraded | 2x | ~1.5–1.9x |
| Max throughput | 2x | ~1.6–1.9x |
| p95 ở cùng số VU | Giảm 50% | Giảm 30–50% |

> **Tại sao không đạt đúng 2x?** SQL Server vẫn là 1 instance dùng chung cho cả 2 API node. Khi VU tăng cao, SQL Server trở thành bottleneck mới thay vì CPU của API. Gain thực tế = gain của API tầng / (1 + overhead SQL).

**Đọc Grafana để hiểu bottleneck đã chuyển đâu:**

```
Sau khi scale:

Nếu: CPU VM1 + CPU VM2 đều thấp (<50%) nhưng p95 vẫn cao
→ SQL Server là bottleneck mới
→ Trong SSMS: chạy Query 1 để xem wait_type
→ wait_type = PAGEIOLATCH_SH → thiếu index
→ wait_type = LCK_M_X → lock contention (cần optimistic concurrency)

Nếu: CPU VM1 + CPU VM2 đều cao (~80–90%) → p95 tương đối OK
→ Hệ thống đang dùng tài nguyên tốt
→ Muốn scale thêm → thêm VM3

Nếu: Error rate vẫn cao dù p95 thấp
→ Có thể SQL connection pool cạn (cả 2 node dùng chung pool)
→ Kiểm tra: số connection trong SSMS (Query 2)
→ Fix: thêm "Max Pool Size=300" vào connection string
```

---

## Checklist toàn bộ quy trình

```
GIAI ĐOẠN 0 — VMware Setup:
[ ] VM1 dùng Bridged network → ip addr = 192.168.1.35
[ ] VM2 đã tạo xong → Bridged → ip addr = 192.168.1.36
[ ] VM2 đã cài Docker + Docker Compose
[ ] ping từ VM2 đến VM1 → OK
[ ] ping từ VM1 đến VM2 → OK

GIAI ĐOẠN 1 — VM1 Setup:
[ ] create-certs.sh → docker/certs/openiddict.pfx đã tạo
[ ] docker/.env → OPENIDDICT_CERT_PASSWORD đã tạo
[ ] docker-compose.monitoring.yml → thêm cert volumes + env cho api
[ ] docker compose up -d --build → 5 container healthy
[ ] docker logs api → thấy "Application started", KHÔNG thấy "Access denied"
[ ] docker exec api printenv | grep OpenIddict → thấy CertPath
[ ] curl VM1:5000/health → {"status":"Healthy"}
[ ] Prometheus Targets → dotnet-api = UP
[ ] Grafana: 2 datasource + 3 dashboard đã import

GIAI ĐOẠN 1 — Crash Test 1:
[ ] Mở Grafana 2587 + 10915 (instance api:8080)
[ ] Chạy k6 crash-test.js → BASE_URL=VM1:5000
[ ] Ghi lại ngưỡng degraded: _____ VU
[ ] Ghi lại ngưỡng collapse: _____ VU
[ ] Verify self-recover sau khi VU → 0

GIAI ĐOẠN 2 — VM2 Setup:
[ ] VM2: source code đã có (git clone hoặc scp)
[ ] VM2: docker/certs/openiddict.pfx đã copy từ VM1
[ ] VM2: docker/.env đã tạo với cùng password
[ ] VM2: connection string trỏ đúng IP VM1 (SQL Server)
[ ] VM2: docker compose -f docker-compose.node2.yml up -d --build
[ ] VM2: docker logs api → "Application started", KHÔNG thấy migration errors
[ ] curl VM2:5000/health → {"status":"Healthy"}

GIAI ĐOẠN 3 — Load Balancer:
[ ] nginx.conf: IP VM2 đã cập nhật (192.168.1.36:5000)
[ ] docker compose -f docker-compose.lb.yml up -d nginx-lb
[ ] curl VM1:80/lb-health → nginx-lb-ok
[ ] docker logs nginx-lb → thấy request đến cả 2 upstream

GIAI ĐOẠN 4 — Verify Token:
[ ] Lấy TOKEN qua LB (port 80)
[ ] curl -H "Authorization: Bearer $TOKEN" VM1:5000/api/products → HTTP 200
[ ] curl -H "Authorization: Bearer $TOKEN" VM2:5000/api/products → HTTP 200
[ ] Nếu VM2 = 401 → dừng, fix cert trước khi test

GIAI ĐOẠN 5 — Prometheus + Crash Test 2:
[ ] prometheus.yml: bỏ comment job dotnet-api-vm2 với IP VM2
[ ] curl -X POST VM1:9090/-/reload
[ ] Prometheus Targets: cả 2 target UP (VM1 + VM2)
[ ] Mở Grafana 2587 + 10915 VM1 + 10915 VM2 (3 tab)
[ ] Chạy k6 crash-test.js → BASE_URL=VM1:80 (qua LB)
[ ] Điền bảng so sánh kết quả
[ ] Ngưỡng degraded tăng ~1.5–2x → scale thành công
```

---

## Bottleneck tiếp theo sau khi scale API

Sau khi 2 instance API chạy ổn định, SQL Server sẽ là bottleneck mới:

```
Dấu hiệu SQL Server là bottleneck:
  ✗ CPU VM1 + VM2 thấp (< 50%) nhưng p95 vẫn cao
  ✗ Trong SSMS: wait_type = PAGEIOLATCH_SH hoặc LCK_M_X chiếm nhiều
  ✗ http_req_waiting cao trong k6 (thời gian server xử lý) — CPU .NET thấp

Giải pháp theo thứ tự effort (từ thấp đến cao):
  1. Thêm index trên các cột hay WHERE/JOIN:
     UserId, CategoryId, CreatedAt trên bảng Orders/Products
     → Effort thấp nhất, impact cao nhất

  2. Tăng connection pool trong connection string:
     Max Pool Size=300;Min Pool Size=10;
     → Tránh "connection pool exhausted" khi 2 API cùng kết nối

  3. Redis cache cho GET /api/products:
     → Giảm read load SQL xuống ~80%
     → Thêm service redis vào docker-compose

  4. Read replica SQL Server:
     → All reads → read replica
     → All writes → primary
     → Cần SQL Server Enterprise hoặc chuyển sang PostgreSQL với streaming replication
```

---

---

# Phụ lục — Tóm tắt thay đổi code & infrastructure

> Phần này ghi lại **tất cả những gì đã được thêm mới hoặc sửa đổi** trong dự án để phục vụ bài toán Crash Test + Scale Load Balancing, giúp bạn nắm rõ từng file làm gì và liên kết với nhau như thế nào.

---

## Những file đã được tạo mới

### `k6/crash-test.js` — Script tìm điểm chết

**Làm gì:** Tăng Virtual Users từ 50 lên 2000 trong vòng ~10 phút, không có threshold (không dừng sớm khi vượt ngưỡng), sau đó hạ về 0 để quan sát recovery.

**Khác gì load-test.js:**

| | `load-test.js` | `crash-test.js` |
|---|---|---|
| Mục tiêu | Đo performance ở tải bình thường | Tìm điểm hệ thống fail |
| Thresholds | Có (p95 < 3s, error < 5%) | Không có |
| Max VU | 1000 | 2000 |
| Timeout request | Mặc định | 20s (để thấy server treo) |
| Custom metrics | login/product/order duration | `crash_login_ok`, `crash_timeout`, `crash_5xx` |
| Giai đoạn ramp-down | Có | Có — quan sát hệ thống tự recover không |

**Vì sao timeout 20s thay vì mặc định:** Timeout ngắn (5s) sẽ làm k6 đánh dấu fail ngay và chuyển sang VU tiếp theo — bạn không thấy được hệ thống đang bị treo hay chỉ chậm. Timeout 20s cho phép quan sát server thật sự không trả lời được.

**Custom metrics giải thích:**

```
crash_login_ok   → Rate (0–100%): giảm dần khi server bị quá tải
                   100% = khỏe, 0% = chết hoàn toàn

crash_timeout    → Rate (0–100%): tăng khi connection pool cạn, server không accept kết nối mới
                   Khác crash_5xx: timeout nghĩa là server không trả lời gì
                   crash_5xx nghĩa là server trả lời nhưng là lỗi

crash_5xx        → Rate (0–100%): tăng khi server crash / OOM / unhandled exception
crash_login_ms   → Trend (ms): thấy rõ latency leo thang từng bước theo VU
```

---

### `docker/nginx.conf` — Cấu hình Nginx Load Balancer

**Làm gì:** Định nghĩa upstream pool 2 backend (VM1:5000 và VM2:5000), nhận request vào port 80 rồi phân phối.

**Các quyết định thiết kế:**

```
least_conn (thay vì round_robin mặc định):
  → Round-robin chia đều số request
  → Least_conn chia đều số connection đang mở
  → Khi VM1 đang xử lý request lâu (GC pause), least_conn sẽ chuyển sang VM2
  → Round-robin vẫn sẽ gửi tiếp vào VM1 dù nó đang bận

max_fails=3 fail_timeout=30s:
  → Nếu 1 backend fail 3 lần trong 30s → Nginx tạm loại backend đó ra
  → Sau 30s → thử lại → nếu OK → đưa vào pool trở lại
  → Không cần tay can thiệp khi 1 instance bị crash

proxy_next_upstream error timeout http_502 http_503:
  → Nếu request đến VM1 bị lỗi → Nginx tự retry sang VM2
  → User không thấy lỗi 502

keepalive 64:
  → Giữ 64 connection sẵn có đến mỗi backend
  → Tránh tốn thời gian TCP handshake mỗi request

log_format lb_log → upstream_addr:
  → Log ghi rõ request đến 192.168.1.35:5000 hay 192.168.1.36:5000
  → Dùng để verify LB đang phân phối thật sự, không phải chỉ gửi về 1 node
```

---

### `docker/docker-compose.lb.yml` — Service Nginx LB cho VM1

**Làm gì:** Định nghĩa service `nginx-lb` chạy trên VM1, mount `nginx.conf` vào, expose port 80.

**Tại sao tách file thay vì thêm vào `docker-compose.monitoring.yml`:** Nginx LB chỉ cần thiết sau khi VM2 đã up. Tách file cho phép:

```bash
# Thêm LB vào stack hiện có mà không rebuild toàn bộ
docker compose -f docker-compose.monitoring.yml \
               -f docker-compose.lb.yml up -d nginx-lb

# Gỡ LB ra không ảnh hưởng monitoring stack
docker compose -f docker-compose.lb.yml down nginx-lb
```

---

### `docker/docker-compose.node2.yml` — Stack API-only cho VM2

**Làm gì:** Chạy đúng 1 service `api` trên VM2, kết nối vào SQL Server trên VM1, dùng cert file được copy sang.

**Tại sao không chạy monitoring stack trên VM2:**
- Prometheus/Grafana/InfluxDB đã có trên VM1
- Prometheus trên VM1 scrape VM2 bằng IP (không cần VM2 chạy Prometheus riêng)
- Tiết kiệm RAM/CPU cho VM2 — toàn bộ tài nguyên dành cho API

**Điểm khác biệt quan trọng với `docker-compose.monitoring.yml`:**

```yaml
# docker-compose.node2.yml buộc phải có:
- OpenIddict__CertPath=/app/certs/openiddict.pfx
- OpenIddict__CertPassword=${OPENIDDICT_CERT_PASSWORD}
volumes:
  - ./certs/openiddict.pfx:/app/certs/openiddict.pfx:ro

# Lý do: nếu thiếu cert → code fallback về Ephemeral key
#         → key VM2 khác key VM1 → token ký bởi VM1 invalid trên VM2 → 401
```

---

### `docker/create-certs.sh` — Script tạo cert `.pfx`

**Làm gì:** Chạy 3 lệnh `openssl` để sinh RSA key → tạo self-signed cert → đóng gói thành `.pfx`.

**Tại sao dùng self-signed cert (không phải Let's Encrypt / CA thật):**
- Cert này **không phải TLS certificate** — không dùng cho HTTPS
- Chỉ dùng để ký payload bên trong JWT token
- Client không bao giờ thấy cert này — chỉ có server dùng để sign và verify
- Self-signed là hoàn toàn hợp lệ và an toàn cho mục đích này

**Tại sao cert 10 năm:** JWT signing key không cần rotate thường xuyên (khác với TLS cert). Rotate key JWT cần: deploy key mới, giữ key cũ còn hiệu lực đến khi tất cả token cũ hết hạn (7 ngày với config hiện tại), rồi mới xóa key cũ. Dùng 10 năm để tránh làm bài tập này trong vòng đời lab.

---

## Những file đã được cập nhật

### `docker/prometheus/prometheus.yml` — Thêm job VM2 và node-exporter (dạng comment)

**Thay đổi:** Thêm 2 khối scrape config dạng comment:

```yaml
# job dotnet-api-vm2: scrape API trên VM2 — bỏ comment khi VM2 đã up
# job node-exporter:  scrape metrics hệ thống host — bỏ comment khi thêm node-exporter vào compose
```

**Tại sao để dạng comment thay vì xóa:** File này dùng cho cả lúc chưa có VM2 (chạy lần đầu) và lúc đã có VM2. Comment giúp bạn biết chính xác chỗ nào cần bỏ comment mà không cần nhớ cú pháp YAML.

**Cách áp dụng thay đổi prometheus.yml mà không restart container:**
```bash
curl -X POST http://192.168.1.35:9090/-/reload
# Prometheus reload config trong vòng 2–3 giây, không mất dữ liệu đã scrape
```

### `answer.md` — Thêm Phần 7 + Phụ lục + Mục lục

**Thay đổi:**
- Mục lục: thêm Phần 7 (12 section) + Phụ lục
- Nội dung: Phần 7 hướng dẫn crash test → tìm ngưỡng → scale → verify
- Phụ lục (phần này): giải thích từng file thay đổi

---

## Luồng triển khai toàn bộ

```
GIAI ĐOẠN 0 — VMware Setup:
  VM1: Bridged network → 192.168.1.35
  VM2: Tạo mới → Bridged → 192.168.1.36 (IP tĩnh)
  VM2: Cài Docker Engine + Docker Compose
  Verify: ping VM1↔VM2 đều thành công
         │
         ▼
GIAI ĐOẠN 1a — VM1 Setup (cert + stack):
  create-certs.sh → docker/certs/openiddict.pfx          [VM1]
  Cập nhật docker-compose.monitoring.yml (cert volumes)  [VM1]
  docker compose up -d --build                           [VM1]
  Verify: 5 container healthy, login API OK
         │
         ▼
GIAI ĐOẠN 1b — Crash Test 1 (tìm ngưỡng):
  k6 crash-test.js → BASE_URL=http://192.168.1.35:5000  [từ máy test]
  Quan sát Grafana 2587 + 10915 song song
  Ghi lại: VU degraded = _____, VU collapse = _____
         │
         ▼
GIAI ĐOẠN 2 — Setup VM2:
  scp source code → VM2                                  [VM1→VM2]
  scp openiddict.pfx → VM2/docker/certs/                 [VM1→VM2]
  Tạo .env trên VM2
  docker compose -f docker-compose.node2.yml up -d --build  [VM2]
  Verify: curl VM2:5000/health → {"status":"Healthy"}
         │
         ▼
GIAI ĐOẠN 3 — Load Balancer:
  Sửa nginx.conf: điền IP VM2 (192.168.1.36:5000)        [VM1]
  docker compose -f docker-compose.lb.yml up -d nginx-lb  [VM1]
  Verify: curl VM1:80/lb-health → nginx-lb-ok
  Verify: docker logs nginx-lb → thấy 2 upstream xen kẽ
         │
         ▼
GIAI ĐOẠN 4 — Verify Token Cross-Instance:
  Login qua LB (port 80) → lấy TOKEN
  curl -H "Bearer $TOKEN" VM1:5000/api/products → 200    [bypass LB]
  curl -H "Bearer $TOKEN" VM2:5000/api/products → 200    [bypass LB]
  Nếu VM2 = 401 → DỪNG, fix cert trước
         │
         ▼
GIAI ĐOẠN 5a — Update Prometheus:
  Bỏ comment job dotnet-api-vm2 trong prometheus.yml
  curl -X POST http://192.168.1.35:9090/-/reload
  Verify: Status → Targets → 2 target UP (VM1 + VM2)
         │
         ▼
GIAI ĐOẠN 5b — Crash Test 2 (verify scale):
  k6 crash-test.js → BASE_URL=http://192.168.1.35:80    [qua LB]
  Mở Grafana 2587 + 10915(VM1) + 10915(VM2) — 3 tab song song
  Điền bảng so sánh: ngưỡng tăng ~1.5–2x → thành công
```

**Kết quả kỳ vọng sau khi scale:**

| | Trước (1 instance) | Sau (2 instance + LB) |
|---|---|---|
| Ngưỡng degraded | ~300–600 VU | ~600–1000 VU (1.5–2x) |
| Ngưỡng collapse | ~600–1200 VU | ~1200–2000 VU (1.5–2x) |
| p95 ở cùng số VU | Baseline | Giảm 30–50% |
| Max throughput | X req/s | ~1.6–1.9X req/s |
| Token cross-instance | N/A | Hợp lệ (cùng cert file) |
| Recovery sau hạ tải | Phụ thuộc | Tốt hơn (LB phân tán GC pause) |

> Gain thực tế là 1.6–1.9x (không phải 2x lý tưởng) vì SQL Server vẫn là 1 instance dùng chung — đây là bottleneck tầng tiếp theo sau khi tầng API đã được scale.
