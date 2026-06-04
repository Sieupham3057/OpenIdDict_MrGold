# Lab k6 + InfluxDB + Prometheus + Grafana — cách 2: không phụ thuộc cAdvisor

Tài liệu này dùng cho bài toán hiện tại của bạn: chạy `.NET API` trong Docker, dùng `k6` giả lập request, ghi kết quả k6 vào `InfluxDB`, dùng `Prometheus` scrape metrics từ `.NET API /metrics`, rồi tự tạo dashboard bằng tay trong Grafana.

Phiên bản này đã đổi theo **cách 2**:

- **Không dùng cAdvisor làm phần bắt buộc** cho dashboard CPU/RAM container.
- Dùng metrics từ `.NET API /metrics` để xem **API process CPU/RAM**.
- Dùng `docker stats` để xem **container CPU/RAM thật** khi cần.
- Giữ phần học chính: `k6 → InfluxDB → Grafana` và `API /metrics → Prometheus → Grafana`.

Lý do bỏ cAdvisor khỏi luồng chính: trên server hiện tại, cAdvisor đọc được Docker socket nhưng không map được Docker layer, log có lỗi dạng:

```text
failed to identify the read-write layer ID for container ...
open /rootfs/var/lib/docker/image/overlayfs/layerdb/mounts/.../mount-id: no such file or directory
```

Khi đó cAdvisor chỉ expose được metric root như:

```text
container_memory_usage_bytes{id="/"} ...
```

và không có series theo container `api` / `docker-api`, nên Grafana query container CPU/RAM sẽ không có data. Đây là vấn đề compatibility với Docker storage layout, không phải bạn thao tác sai Grafana.

---

## 1. Hiểu bản chất luồng dữ liệu

### Luồng 1 — k6 load test

```text
[k6 chạy trên host]
    |
    | --out influxdb=http://localhost:8086/k6
    v
[InfluxDB database: k6]
    |
    | datasource influxdb
    v
[Grafana dashboard k6]
```

Luồng này dùng để xem:

- P95 response time.
- Average response time.
- Requests per second.
- Virtual users.
- Error rate.
- Checks per second.

### Luồng 2 — API monitoring

```text
[.NET API /metrics]
    |
    | Prometheus scrape api:8080/metrics
    v
[Prometheus]
    |
    | datasource Prometheus
    v
[Grafana dashboard API]
```

Luồng này dùng để xem:

- API target có UP không.
- API request rate.
- API p95 latency.
- API process memory.
- API process CPU.
- GC / thread pool nếu metric có expose.

### Luồng 3 — Container resource xem bằng terminal

```text
[docker stats]
    |
    v
CPU/RAM thật của container api, sqlserver, grafana, prometheus, influxdb
```

Khi chạy load test, mở một terminal riêng:

```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

---

## 2. Thông tin môi trường

| Thành phần | URL |
|---|---|
| API | `http://192.168.1.35:5000` |
| Swagger | `http://192.168.1.35:5000/swagger/index.html` |
| InfluxDB | `http://192.168.1.35:8086` |
| Prometheus | `http://192.168.1.35:9090` |
| Grafana | `http://192.168.1.35:3000` |
| SQL Server | `192.168.1.35:1433` |

---

## 3. Đứng đúng thư mục

```bash
cd ~/projects/OpenIdDict_MrGold/docker
pwd
```

Kỳ vọng:

```text
/home/bank/projects/OpenIdDict_MrGold/docker
```

---

## 4. Làm sạch file cấu hình cũ

```bash
rm -rf grafana prometheus

mkdir -p grafana/provisioning/datasources
mkdir -p prometheus
```

Kiểm tra:

```bash
find grafana prometheus -maxdepth 3 -type d | sort
```

Kỳ vọng:

```text
grafana
grafana/provisioning
grafana/provisioning/datasources
prometheus
```

Lưu ý: không tạo `grafana/dashboards`, vì tài liệu này hướng dẫn tạo dashboard bằng tay trên UI.

---

## 5. Tạo `docker-compose.monitoring.yml`

> File này **không có cAdvisor**. Ta chỉ dùng `api`, `influxdb`, `prometheus`, `grafana`.

```bash
cat > docker-compose.monitoring.yml <<'COMPOSE'
services:

  # ── API: AuthDemo .NET 8 ────────────────────────────────────────────────────────
  api:
    build:
      context: ..
      dockerfile: AuthDemo.Api/Dockerfile
    container_name: api
    ports:
      - "5000:8080"                        # Host:5000 → Container:8080
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=192.168.1.35,1433;Database=AuthDemoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
    volumes:
      - api-logs:/app/logs
    restart: unless-stopped

  # ── InfluxDB: k6 ghi kết quả load test vào đây ─────────────────────────────────
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

  # ── Prometheus: scrape API /metrics ────────────────────────────────────────────
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

  # ── Grafana: UI đọc InfluxDB và Prometheus ─────────────────────────────────────
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
COMPOSE
```

---

## 6. Tạo `prometheus/prometheus.yml`

```bash
cat > prometheus/prometheus.yml <<'PROMETHEUS'
global:
  scrape_interval: 5s
  evaluation_interval: 5s

scrape_configs:

  - job_name: 'dotnet-api'
    static_configs:
      - targets: ['api:8080']
    metrics_path: '/metrics'
PROMETHEUS
```

Prometheus nằm trong Docker network, nên nó gọi API bằng `api:8080`, không dùng `localhost:5000`.

---

## 7. Tạo `grafana/grafana.ini`

```bash
cat > grafana/grafana.ini <<'GRAFANA_INI'
[feature_toggles]
influxdbBackendMigration = false
GRAFANA_INI
```

Lý do: với InfluxDB 1.8 và Grafana mới, tắt backend migration giúp tránh lỗi query InfluxDB v1 bị rỗng.

---

## 8. Tạo datasource InfluxDB

```bash
cat > grafana/provisioning/datasources/influxdb.yml <<'INFLUX_DS'
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
INFLUX_DS
```

---

## 9. Tạo datasource Prometheus

```bash
cat > grafana/provisioning/datasources/prometheus.yml <<'PROM_DS'
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
PROM_DS
```

---

## 10. Down sạch volume và chạy lại

Lệnh này xóa sạch dữ liệu cũ của Grafana, Prometheus, InfluxDB.

```bash
docker compose -f docker-compose.monitoring.yml down -v
```

Dọn container cũ nếu còn sót:

```bash
docker rm -f grafana prometheus influxdb cadvisor api 2>/dev/null || true
```

Build và chạy lại:

```bash
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

`cadvisor` không còn bắt buộc trong lab này.

---

## 11. Verify service bằng curl

### API health

```bash
curl http://localhost:5000/health
```

### API metrics

```bash
curl http://localhost:5000/metrics | head
```

Phải thấy dòng dạng:

```text
# HELP ...
# TYPE ...
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

Kỳ vọng:

```text
Prometheus Server is Healthy.
```

### Grafana

```bash
curl -u admin:admin123 http://localhost:3000/api/health
```

Kỳ vọng có:

```json
"database":"ok"
```

---

## 12. Verify datasource Grafana

```bash
curl -s -u admin:admin123 http://localhost:3000/api/datasources
```

Kỳ vọng chỉ có 2 datasource:

```text
influxdb
Prometheus
```

Nếu có 2 Prometheus, nghĩa là volume Grafana chưa được xóa sạch. Chạy lại:

```bash
docker compose -f docker-compose.monitoring.yml down -v
docker compose -f docker-compose.monitoring.yml up -d --build
```

---

## 13. Verify Prometheus target

Mở:

```text
http://192.168.1.35:9090/targets
```

Kỳ vọng:

```text
dotnet-api = UP
```

Nếu `dotnet-api` DOWN:

```bash
docker logs api --tail 100
curl http://localhost:5000/metrics | head
```

---

## 14. Verify API process metrics có tồn tại

Trước khi tạo dashboard API CPU/RAM, kiểm tra metric thật:

```bash
curl http://localhost:5000/metrics | grep '^process_' | head -30
```

Kỳ vọng có các metric như:

```text
process_cpu_seconds_total
process_resident_memory_bytes
process_virtual_memory_bytes
process_num_threads
```

Nếu có, Prometheus query bên dưới sẽ chạy được.

Kiểm tra trực tiếp trong Prometheus:

```text
http://192.168.1.35:9090/graph
```

Query:

```promql
process_resident_memory_bytes{job="dotnet-api"}
```

và:

```promql
rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100
```

---

## 15. Chạy k6 để tạo dữ liệu InfluxDB

Vì đã `down -v`, InfluxDB đang rỗng. Phải chạy k6 thì dashboard k6 mới có data.

```bash
cd ~/projects/OpenIdDict_MrGold

k6 run \
  --out influxdb=http://localhost:8086/k6 \
  k6/smoke-test.js
```

Kiểm tra InfluxDB có measurement:

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

Nếu thấy time `1970-01-01T00:00:00Z` thì không sao. Đó là do query `count()` không `GROUP BY time(...)`. Không phải lệch giờ.

Muốn xem timestamp thật:

```bash
curl -G "http://localhost:8086/query" \
  --data-urlencode "db=k6" \
  --data-urlencode 'q=SELECT "value" FROM "http_req_duration" ORDER BY time DESC LIMIT 5'
```

---

## 16. Tạo dashboard k6 bằng tay trong Grafana

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

Trong Grafana 11:

- Nhìn thanh bên phải.
- Bấm nút dấu `+` màu xanh.
- Chọn card thêm visualization ở trên cùng.
- Chọn datasource `influxdb`.

Nếu thao tác UI bị lỗi, có thể vào:

```text
Explore → datasource influxdb
```

Test query trước, sau đó copy sang panel.

---

## 17. Cách chọn Unit trong Grafana 11

Trong Grafana 11, danh sách `Unit` rất dài và không phải lúc nào cũng hiện đúng chữ `milliseconds` ngay từ đầu.

Cách chọn chuẩn:

```text
Panel bên phải → Standard options → Unit → click ô Choose → gõ từ khóa
```

Các từ khóa nên gõ:

| Muốn hiển thị | Gõ vào ô Unit | Thường nằm trong nhóm |
|---|---|---|
| Milliseconds | `ms` | Time |
| Seconds | `s` | Time |
| Percent | `%` hoặc `percent` | Misc |
| Bytes/RAM | `bytes` | Data |
| Requests/sec | có thể để trống | Throughput |

Nếu không tìm thấy unit, cứ để trống vẫn chạy được. Unit chỉ ảnh hưởng cách hiển thị số, không ảnh hưởng query hay dữ liệu.

---

## 18. Panel k6 — P95 Response Time

Datasource: `influxdb`

Query:

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

Unit trong Grafana 11:

```text
Standard options → Unit → gõ trực tiếp vào ô Choose: ms
```

Sau đó chọn option có ký hiệu `ms`.

---

## 19. Panel k6 — Average Response Time

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

---

## 20. Panel k6 — Requests Per Second

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

---

## 21. Panel k6 — Virtual Users

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

---

## 22. Panel k6 — Error Rate %

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

---

## 23. Panel k6 — Checks Per Second

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

## 24. Lưu dashboard k6

Bấm **Save**.

Tên dashboard:

```text
k6 Manual Load Testing Dashboard
```

Góc phải chọn:

```text
Last 15 minutes
Refresh 5s
```

Chạy lại k6 để xem realtime:

```bash
cd ~/projects/OpenIdDict_MrGold

k6 run \
  --out influxdb=http://localhost:8086/k6 \
  k6/smoke-test.js
```

---

## 25. Tạo dashboard API bằng tay với Prometheus

Tạo dashboard mới:

```text
Dashboards → New → New dashboard → Add visualization → datasource Prometheus
```

---

## 26. Panel Prometheus — API Target UP

PromQL:

```promql
up{job="dotnet-api"}
```

Title:

```text
API Target UP
```

Giá trị:

- `1` là UP.
- `0` là DOWN.

---

## 27. Panel Prometheus — API Request Rate

PromQL:

```promql
sum(rate(http_requests_received_total{job="dotnet-api"}[1m]))
```

Title:

```text
API Request Rate
```

Nếu không có data, kiểm tra tên metric thật:

```bash
curl http://localhost:5000/metrics | grep '^http_' | head -30
```

Hoặc trong Prometheus Graph gõ:

```promql
http_
```

để xem autocomplete.

---

## 28. Panel Prometheus — API P95 Latency

PromQL:

```promql
histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job="dotnet-api"}[5m])) by (le))
```

Title:

```text
API P95 Latency
```

Unit:

```text
seconds
```

Nếu không có data, kiểm tra metric thật:

```bash
curl http://localhost:5000/metrics | grep 'duration_seconds_bucket' | head
```

---

## 29. Panel Prometheus — API Process Memory

PromQL:

```promql
process_resident_memory_bytes{job="dotnet-api"}
```

Title:

```text
API Process Memory
```

Unit trong Grafana 11:

```text
Standard options → Unit → gõ vào ô Choose: bytes
```

Sau đó chọn option có chữ `bytes`.

Ý nghĩa:

- Đây là RAM resident của process `.NET` bên trong container API.
- Không phải toàn bộ container memory.
- Với container API chỉ chạy một process chính `.NET`, metric này đủ hữu ích để theo dõi memory của API khi load test.

---

## 30. Panel Prometheus — API Process CPU %

PromQL:

```promql
rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100
```

Title:

```text
API Process CPU %
```

Unit trong Grafana 11:

```text
Standard options → Unit → gõ vào ô Choose: percent
```

Hoặc gõ `%`.

Ý nghĩa:

- Đây là CPU usage của process API.
- Nếu giá trị khoảng `50`, hiểu là process dùng khoảng 50% của 1 CPU core.
- Nếu máy 2 core, giá trị có thể vượt 100 nếu process dùng hơn 1 core.

---

## 31. Panel Prometheus — API Threads

Nếu metric tồn tại:

```promql
process_num_threads{job="dotnet-api"}
```

Title:

```text
API Process Threads
```

---

## 32. Panel Prometheus — .NET GC Gen2

Kiểm tra metric thật trước:

```bash
curl http://localhost:5000/metrics | grep -i 'gc' | head -50
```

Nếu có `dotnet_gc_collections_total`, dùng:

```promql
rate(dotnet_gc_collections_total{job="dotnet-api"}[5m])
```

Title:

```text
.NET GC Collections Rate
```

Nếu metric có label `generation`, có thể tách Gen2:

```promql
rate(dotnet_gc_collections_total{job="dotnet-api",generation="2"}[5m])
```

---

## 33. Xem container CPU/RAM thật bằng `docker stats`

Vì không dùng cAdvisor làm nguồn chính, khi cần xem container resource thật thì dùng terminal:

```bash
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"
```

Khi chạy k6, mở terminal riêng:

```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

Bạn sẽ thấy kiểu:

```text
NAME         CPU %     MEM USAGE / LIMIT     MEM %
api          35.2%     512MiB / 16GiB        3.2%
sqlserver    8.4%      2.1GiB / 16GiB        13.1%
influxdb     3.2%      300MiB / 16GiB        1.8%
prometheus   1.5%      120MiB / 16GiB        0.7%
grafana      0.8%      180MiB / 16GiB        1.1%
```

---

## 34. Debug k6 dashboard không có data

### InfluxDB có measurement không?

```bash
curl -G "http://localhost:8086/query" \
  --data-urlencode "db=k6" \
  --data-urlencode "q=SHOW MEASUREMENTS"
```

Nếu không có `http_req_duration`, k6 chưa ghi vào InfluxDB.

Chạy lại đúng lệnh:

```bash
k6 run --out influxdb=http://localhost:8086/k6 k6/smoke-test.js
```

### InfluxDB có data nhưng Grafana không hiện

Vào Grafana Explore → datasource `influxdb`, chạy:

```sql
SELECT mean("value")
FROM "http_req_duration"
WHERE $timeFilter
GROUP BY time($__interval) fill(null)
```

Nếu Explore có data mà panel không có:

- Panel query sai.
- Datasource panel sai.
- Time range sai.

Chọn:

```text
Last 15 minutes
```

hoặc:

```text
Last 6 hours
```

---

## 35. Debug API Prometheus dashboard không có data

### Prometheus target có UP không?

Mở:

```text
http://192.168.1.35:9090/targets
```

Kỳ vọng:

```text
dotnet-api = UP
```

### API có expose metric không?

```bash
curl http://localhost:5000/metrics | grep '^process_' | head
curl http://localhost:5000/metrics | grep '^http_' | head
```

### Prometheus có ingest metric không?

Vào Prometheus Graph chạy:

```promql
up{job="dotnet-api"}
```

```promql
process_resident_memory_bytes{job="dotnet-api"}
```

```promql
rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100
```

---

## 36. Ghi chú về cAdvisor

cAdvisor không còn là phần bắt buộc trong tài liệu này.

Nếu sau này muốn thử lại cAdvisor, cần hiểu:

- cAdvisor phải expose được metric có label `image`/`name` của container.
- Nếu chỉ có `container_memory_usage_bytes{id="/"}`, thì không dùng được để vẽ CPU/RAM từng container.
- Lỗi `failed to identify the read-write layer ID` cho thấy cAdvisor không tương thích với Docker storage layout hiện tại.
- Khi đó dùng `.NET process metrics` + `docker stats` là hướng thực tế hơn cho lab này.

---

## 37. Checklist cuối cùng

```text
[ ] docker ps có api, influxdb, prometheus, grafana
[ ] curl localhost:5000/health OK
[ ] curl localhost:5000/metrics có # HELP
[ ] curl localhost:5000/metrics có process_cpu_seconds_total
[ ] curl localhost:5000/metrics có process_resident_memory_bytes
[ ] curl -i localhost:8086/ping trả 204
[ ] Prometheus /targets: dotnet-api UP
[ ] Grafana datasource có đúng 2 cái: influxdb và Prometheus
[ ] Chạy k6 với --out influxdb=http://localhost:8086/k6
[ ] InfluxDB SHOW MEASUREMENTS có http_req_duration
[ ] Grafana Explore datasource influxdb query ra data
[ ] Dashboard k6 tự tạo bằng tay có panel P95, RPS, VU, Error Rate
[ ] Dashboard API có API Target UP, API Request Rate, API P95 Latency
[ ] Dashboard API có API Process Memory, API Process CPU %
[ ] Terminal docker stats xem được container CPU/RAM thật
```

---

## 38. Lệnh thường dùng

Restart toàn stack:

```bash
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.monitoring.yml restart
```

Xem log API:

```bash
docker logs api --tail 100 -f
```

Xem log Prometheus:

```bash
docker logs prometheus --tail 100 -f
```

Xem log Grafana:

```bash
docker logs grafana --tail 100 -f
```

Reset sạch toàn bộ lab:

```bash
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.monitoring.yml down -v
docker compose -f docker-compose.monitoring.yml up -d --build
```

Xem container resource khi chạy k6:

```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

---

## 39. Ghi nhớ quan trọng

- `down -v` xóa sạch InfluxDB data, nên phải chạy lại k6.
- Grafana datasource có thể provision tự động.
- Dashboard nếu muốn học bản chất thì tự tạo bằng UI.
- Dashboard community như ID `2587` có thể không khớp schema/version, không nên phụ thuộc.
- Query k6 phải dựa trên measurement thật trong InfluxDB: `http_req_duration`, `http_reqs`, `vus`, `checks`, `http_req_failed`.
- Query API Prometheus phải dựa trên metric thật từ `/metrics`: `http_*`, `process_*`, `dotnet_*`.
- cAdvisor là optional trong lab này. Nếu không expose được container label, bỏ qua và dùng `docker stats`.
