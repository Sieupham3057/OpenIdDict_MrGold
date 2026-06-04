# Cập nhật tài liệu học tập Load Testing & Monitoring — khuyến nghị Cách 2

> File này là bản cập nhật theo tình trạng thực tế của project hiện tại.
>
> Quyết định kỹ thuật hiện tại:
>
> - **Giữ k6 + InfluxDB + Grafana** để xem kết quả load test.
> - **Giữ Prometheus scrape `.NET API /metrics`** để xem request rate, latency, CPU/RAM process, GC, thread pool.
> - **Không lấy cAdvisor làm luồng chính** vì trên server hiện tại cAdvisor chỉ expose được root cgroup `id="/"`, không expose được container `api/docker-api`.
> - **Dùng `docker stats` để xem CPU/RAM container thật** trong lúc chạy k6.
> - **Không phụ thuộc dashboard community ID 2587 / 10915 / 14282**. Dashboard nên tạo thủ công bằng UI để hiểu bản chất.

---

## 1. Vì sao cần cập nhật tài liệu cũ?

Tài liệu cũ đang có một số điểm không còn phù hợp với thực tế server hiện tại:

| Nội dung cũ | Vấn đề | Cách cập nhật |
|---|---|---|
| cAdvisor là luồng chính để xem container CPU/RAM | Server hiện tại cAdvisor không map được Docker container metadata, chỉ thấy `id="/"` | Chuyển cAdvisor thành optional/debug |
| Import dashboard community `2587`, `10915`, `14282` | Dễ lỗi version/schema/query/datasource variable | Tạo panel thủ công trong Grafana |
| Dùng `grafana/grafana:latest`, `prom/prometheus:latest` ở một số đoạn ví dụ | `latest` dễ kéo version mới gây lỗi UI/query | Pin version |
| Query container memory: `container_memory_usage_bytes{name="api"}` | Không có data nếu cAdvisor không expose label `name="api"` | Dùng `.NET process metrics` thay thế |
| Checklist bắt buộc cAdvisor thấy `api` | Không đúng với môi trường hiện tại | Checklist mới: API metrics + k6 data + docker stats |

---

## 2. Kiến trúc khuyến nghị hiện tại

```text
Luồng 1 — Load test result:

[k6 chạy trên host]
    |
    | --out influxdb=http://localhost:8086/k6
    v
[InfluxDB 1.8 database: k6]
    |
    | Grafana datasource: influxdb
    v
[Grafana k6 dashboard thủ công]
```

```text
Luồng 2 — API runtime metrics:

[.NET API /metrics]
    |
    | Prometheus scrape mỗi 5s
    v
[Prometheus]
    |
    | Grafana datasource: Prometheus
    v
[Grafana API dashboard thủ công]
```

```text
Luồng 3 — Container resource thật:

[docker stats]
    |
    v
Terminal realtime khi chạy k6
```

Ghi nhớ:

- **k6 terminal** cho bạn kết quả tổng sau test.
- **InfluxDB** lưu metric k6 theo thời gian.
- **Prometheus** lưu metric `.NET API /metrics`.
- **Grafana** chỉ vẽ biểu đồ, không tự sinh data.
- **docker stats** là cách chắc chắn nhất để xem CPU/RAM container trên máy hiện tại.

---

## 3. docker-compose.monitoring.yml khuyến nghị

Bản này **không cần cAdvisor** trong luồng chính.

```yaml
services:

  api:
    build:
      context: ..
      dockerfile: AuthDemo.Api/Dockerfile
    container_name: api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=192.168.1.35,1433;Database=AuthDemoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
    volumes:
      - api-logs:/app/logs
    restart: unless-stopped

  influxdb:
    image: influxdb:1.8
    container_name: influxdb
    ports:
      - "8086:8086"
    environment:
      - INFLUXDB_DB=k6
      - INFLUXDB_HTTP_AUTH_ENABLED=false
    volumes:
      - influxdb-data:/var/lib/influxdb
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8086/ping"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 10s
    restart: unless-stopped

  prometheus:
    image: prom/prometheus:v2.55.1
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    command:
      - "--config.file=/etc/prometheus/prometheus.yml"
      - "--storage.tsdb.retention.time=7d"
      - "--web.enable-lifecycle"
    depends_on:
      - api
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:9090/-/healthy"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 15s
    restart: unless-stopped

  grafana:
    image: grafana/grafana:11.6.1
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin123
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - ./grafana/grafana.ini:/etc/grafana/grafana.ini:ro
      - ./grafana/provisioning:/etc/grafana/provisioning:ro
      - grafana-data:/var/lib/grafana
    depends_on:
      - prometheus
      - influxdb
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:3000/api/health"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 30s
    restart: unless-stopped

volumes:
  prometheus-data:
  grafana-data:
  influxdb-data:
  api-logs:
```

> Nếu sau này bạn muốn thử lại cAdvisor, để nó ở file compose riêng như `docker-compose.cadvisor.yml`, không đưa vào luồng học chính.

---

## 4. prometheus/prometheus.yml

```bash
mkdir -p prometheus
cat > prometheus/prometheus.yml <<'EOF'
global:
  scrape_interval: 5s
  evaluation_interval: 5s

scrape_configs:
  - job_name: 'dotnet-api'
    static_configs:
      - targets: ['api:8080']
    metrics_path: '/metrics'
EOF
```

Prometheus nằm trong Docker network, nên target phải là:

```text
api:8080
```

Không dùng:

```text
localhost:5000
```

vì `localhost` bên trong container Prometheus là chính container Prometheus.

---

## 5. Grafana datasource provisioning

### grafana/grafana.ini

```bash
mkdir -p grafana/provisioning/datasources

cat > grafana/grafana.ini <<'EOF'
[feature_toggles]
influxdbBackendMigration = false
EOF
```

### grafana/provisioning/datasources/influxdb.yml

```bash
cat > grafana/provisioning/datasources/influxdb.yml <<'EOF'
apiVersion: 1

deleteDatasources:
  - name: influxdb
    orgId: 1

datasources:
  - name: influxdb
    uid: influxdb
    type: influxdb
    access: proxy
    url: http://influxdb:8086
    database: k6
    isDefault: true
    jsonData:
      httpMode: POST
      timeInterval: 10s
    version: 1
    editable: true
EOF
```

### grafana/provisioning/datasources/prometheus.yml

```bash
cat > grafana/provisioning/datasources/prometheus.yml <<'EOF'
apiVersion: 1

deleteDatasources:
  - name: Prometheus
    orgId: 1
  - name: prometheus
    orgId: 1

datasources:
  - name: Prometheus
    uid: prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: false
    jsonData:
      timeInterval: 5s
    version: 1
    editable: true
EOF
```

---

## 6. Làm sạch và chạy lại

```bash
cd ~/projects/OpenIdDict_MrGold/docker

docker compose -f docker-compose.monitoring.yml down -v
docker rm -f grafana prometheus influxdb api cadvisor 2>/dev/null || true

docker compose -f docker-compose.monitoring.yml up -d --build
```

Kiểm tra:

```bash
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}"
```

Kỳ vọng có:

```text
api
influxdb
prometheus
grafana
```

---

## 7. Verify service

### API

```bash
curl http://localhost:5000/health
curl http://localhost:5000/metrics | head
```

### InfluxDB

```bash
curl -i http://localhost:8086/ping
```

Kỳ vọng:

```text
HTTP/1.1 204 No Content
```

### Prometheus

```bash
curl http://localhost:9090/-/healthy
```

### Grafana

```bash
curl -u admin:admin123 http://localhost:3000/api/health
```

### Datasource Grafana

```bash
curl -s -u admin:admin123 http://localhost:3000/api/datasources
```

Kỳ vọng có đúng:

```text
influxdb
Prometheus
```

### Prometheus target

Mở:

```text
http://192.168.1.35:9090/targets
```

Kỳ vọng:

```text
dotnet-api = UP
```

---

## 8. Chạy k6 để tạo data

```bash
cd ~/projects/OpenIdDict_MrGold

k6 run \
  --out influxdb=http://localhost:8086/k6 \
  k6/smoke-test.js
```

Verify InfluxDB có data:

```bash
curl -G "http://localhost:8086/query" \
  --data-urlencode "db=k6" \
  --data-urlencode "q=SHOW MEASUREMENTS"
```

Kỳ vọng thấy:

```text
http_req_duration
http_reqs
http_req_failed
vus
checks
iterations
```

Kiểm tra số điểm dữ liệu:

```bash
curl -G "http://localhost:8086/query" \
  --data-urlencode "db=k6" \
  --data-urlencode 'q=SELECT count("value") FROM "http_req_duration"'
```

Nếu thấy timestamp:

```text
1970-01-01T00:00:00Z
```

thì không sao. Query `count()` không `GROUP BY time(...)` nên InfluxDB trả timestamp mặc định.

Muốn xem timestamp thật:

```bash
curl -G "http://localhost:8086/query" \
  --data-urlencode "db=k6" \
  --data-urlencode 'q=SELECT "value" FROM "http_req_duration" ORDER BY time DESC LIMIT 5'
```

---

## 9. Tạo dashboard k6 bằng tay trong Grafana

Mở:

```text
http://192.168.1.35:3000
```

Login:

```text
admin / admin123
```

Vào:

```text
Dashboards → New → New dashboard
```

Chọn datasource:

```text
influxdb
```

Góc phải chọn:

```text
Last 15 minutes
Refresh 5s
```

### Panel k6 — P95 Response Time

```sql
SELECT percentile("value", 95)
FROM "http_req_duration"
WHERE $timeFilter
GROUP BY time($__interval) fill(null)
```

Title:

```text
P95 Response Time
```

Unit:

```text
Standard options → Unit → gõ ms → chọn milliseconds/ms
```

### Panel k6 — Average Response Time

```sql
SELECT mean("value")
FROM "http_req_duration"
WHERE $timeFilter
GROUP BY time($__interval) fill(null)
```

Title:

```text
Average Response Time
```

Unit:

```text
ms
```

### Panel k6 — Requests Per Second

```sql
SELECT sum("value")
FROM "http_reqs"
WHERE $timeFilter
GROUP BY time(1s) fill(0)
```

Title:

```text
Requests Per Second
```

### Panel k6 — Virtual Users

```sql
SELECT mean("value")
FROM "vus"
WHERE $timeFilter
GROUP BY time($__interval) fill(null)
```

Title:

```text
Virtual Users
```

### Panel k6 — Error Rate %

```sql
SELECT mean("value") * 100
FROM "http_req_failed"
WHERE $timeFilter
GROUP BY time($__interval) fill(null)
```

Title:

```text
Error Rate %
```

Unit:

```text
percent / %
```

### Panel k6 — Checks Per Second

```sql
SELECT sum("value")
FROM "checks"
WHERE $timeFilter
GROUP BY time(1s) fill(0)
```

Title:

```text
Checks Per Second
```

---

## 10. Tạo dashboard API bằng tay trong Grafana

Tạo dashboard mới, chọn datasource:

```text
Prometheus
```

### Panel API Target UP

```promql
up{job="dotnet-api"}
```

Title:

```text
API Target UP
```

Ý nghĩa:

```text
1 = UP
0 = DOWN
```

### Panel API Request Rate

Trước tiên kiểm tra metric thật:

```bash
curl http://localhost:5000/metrics | grep '^http_' | head -30
```

Nếu có `http_requests_received_total`, dùng:

```promql
sum(rate(http_requests_received_total{job="dotnet-api"}[1m]))
```

Title:

```text
API Request Rate
```

### Panel API P95 Latency

Kiểm tra metric bucket:

```bash
curl http://localhost:5000/metrics | grep 'duration_seconds_bucket' | head
```

Nếu có `http_request_duration_seconds_bucket`, dùng:

```promql
histogram_quantile(
  0.95,
  sum(rate(http_request_duration_seconds_bucket{job="dotnet-api"}[5m])) by (le)
)
```

Title:

```text
API P95 Latency
```

Unit:

```text
seconds
```

### Panel API Process Memory

```promql
process_resident_memory_bytes{job="dotnet-api"}
```

Title:

```text
API Process Memory
```

Unit:

```text
bytes
```

Ý nghĩa:

```text
RAM mà process .NET đang chiếm bên trong container.
```

### Panel API Process CPU %

```promql
rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100
```

Title:

```text
API Process CPU %
```

Unit:

```text
percent / %
```

Ý nghĩa:

```text
CPU mà process API đang sử dụng.
Trên máy 2 core, nếu query ra ~100 tức là gần 1 core.
Nếu ra ~200 tức là gần 2 core.
```

### Panel .NET GC Gen2 Rate

```promql
rate(dotnet_gc_collections_total{job="dotnet-api",generation="2"}[5m])
```

Title:

```text
.NET GC Gen2 Rate
```

Ý nghĩa:

```text
Gen2 tăng nhiều trong lúc load test thường là dấu hiệu memory pressure.
```

### Panel Thread Pool Queue

Thử tìm metric:

```bash
curl http://localhost:5000/metrics | grep -i thread
```

Nếu có `dotnet_threadpool_queue_length`, dùng:

```promql
dotnet_threadpool_queue_length{job="dotnet-api"}
```

Title:

```text
.NET ThreadPool Queue Length
```

Ý nghĩa:

```text
> 0 kéo dài trong lúc load test có thể là dấu hiệu thiếu CPU/thread starvation.
```

---

## 11. Xem CPU/RAM container thật bằng docker stats

Mở terminal riêng trước khi chạy k6:

```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

Khi chạy k6, quan sát:

```text
NAME         CPU %     MEM USAGE / LIMIT     MEM %
api          35.2%     512MiB / 15.6GiB      3.2%
sqlserver    8.4%      2.1GiB / 15.6GiB      13.5%
influxdb     3.2%      300MiB / 15.6GiB      1.9%
prometheus   1.5%      120MiB / 15.6GiB      0.8%
grafana      0.8%      180MiB / 15.6GiB      1.1%
```

Đây là cách xem container CPU/RAM thật trong môi trường hiện tại.

---

## 12. Vì sao không dùng cAdvisor làm chính trong tài liệu này?

Trên server hiện tại, cAdvisor có thể đọc được Docker socket nhưng không map được Docker layer metadata.

Dấu hiệu đã thấy:

```text
container_memory_usage_bytes{id="/"} ...
```

và không có dòng:

```text
container_memory_usage_bytes{image="docker-api",name="api",...}
```

Log lỗi dạng:

```text
failed to identify the read-write layer ID for container ...
open /rootfs/var/lib/docker/image/overlayfs/layerdb/mounts/.../mount-id: no such file or directory
```

Vì vậy, các query kiểu này sẽ không có data:

```promql
container_memory_usage_bytes{name="api"}
```

```promql
rate(container_cpu_usage_seconds_total{name="api"}[1m])
```

Trong lab này, nên dùng:

```promql
process_resident_memory_bytes{job="dotnet-api"}
```

```promql
rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100
```

và dùng terminal:

```bash
docker stats
```

---

## 13. Khi nào nên quay lại cAdvisor?

Dùng lại cAdvisor khi:

- Docker storage layout tương thích.
- `curl http://localhost:8080/metrics | grep 'image='` trả ra container label thật.
- Prometheus query `container_memory_usage_bytes{name=~".*api.*"}` có data.

Lúc đó có thể tạo panel:

```promql
container_memory_usage_bytes{name=~".*api.*|.*docker-api.*", image!=""}
```

```promql
rate(container_cpu_usage_seconds_total{name=~".*api.*|.*docker-api.*", image!=""}[1m]) * 100
```

Nhưng đây là **optional**, không phải luồng học chính.

---

## 14. Checklist cuối cùng theo Cách 2

```text
[ ] docker ps có api, influxdb, prometheus, grafana
[ ] curl localhost:5000/health OK
[ ] curl localhost:5000/metrics có # HELP
[ ] curl -i localhost:8086/ping trả 204
[ ] Prometheus /targets: dotnet-api UP
[ ] Grafana datasource có đúng 2 cái: influxdb và Prometheus
[ ] Chạy k6 với --out influxdb=http://localhost:8086/k6
[ ] InfluxDB SHOW MEASUREMENTS có http_req_duration
[ ] Grafana Explore datasource influxdb query ra data
[ ] Dashboard k6 có P95, RPS, VU, Error Rate
[ ] Dashboard API có up, request rate, p95 latency, process memory, process CPU
[ ] Terminal docker stats hiển thị CPU/RAM container khi chạy k6
```

---

## 15. Quy trình đọc khi load test

```text
1. Chạy k6
   ↓
2. Xem dashboard k6
   - P95 latency
   - RPS
   - Error rate
   - VU
   ↓
3. Nếu P95 tăng:
   Xem dashboard API Prometheus
   - API request rate
   - API P95 latency
   - Process CPU
   - Process memory
   - GC Gen2
   - ThreadPool queue
   ↓
4. Mở terminal docker stats
   - api CPU %
   - api memory
   - sqlserver CPU/RAM
   ↓
5. Nếu CPU thấp nhưng latency cao:
   nghi SQL Server bottleneck
   ↓
6. Vào SSMS/Azure Data Studio chạy DMV query
```

---

## 16. Ghi chú cập nhật cho file cũ

Các phần sau trong file cũ nên đổi trạng thái thành **optional/deprecated cho server hiện tại**:

```text
Dashboard 14282 — cAdvisor exporter
cAdvisor container metrics
container_memory_usage_bytes{name="api"}
container_cpu_usage_seconds_total{name="api"}
Checklist bắt buộc cAdvisor thấy container api
```

Các phần nên giữ:

```text
k6 concepts
InfluxDB 1.8 concepts
Prometheus concepts
Grafana concepts
Percentile/P95/P99
OpenIddict certificate explanation
SQL Server DMV queries
Docker Compose command cheat sheet
PromQL fundamentals
.NET process metrics
```
