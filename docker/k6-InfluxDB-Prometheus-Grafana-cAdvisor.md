# Lab k6 + InfluxDB + Prometheus + Grafana — làm thủ công dashboard để hiểu bản chất

Tài liệu này dùng cho bài toán hiện tại của bạn: chạy .NET API trong Docker, dùng k6 giả lập request, ghi kết quả k6 vào InfluxDB, dùng Prometheus scrape metrics từ API và cAdvisor, rồi tự tạo dashboard bằng tay trong Grafana.

Mục tiêu của tài liệu này:

- Làm lại sạch từ đầu bằng `docker compose down -v`.
- Không import dashboard community.
- Không provision dashboard tự động.
- Chỉ provision datasource `influxdb` và `Prometheus`.
- Tự tạo panel trên Grafana UI để hiểu metric đến từ đâu.
- Sửa lỗi cAdvisor không thấy container `api` bằng cấu hình `docker.sock` + `--docker_only=true`.

Thông tin môi trường đang dùng:

| Thành phần | URL |
|---|---|
| API | `http://192.168.1.35:5000` |
| Swagger | `http://192.168.1.35:5000/swagger/index.html` |
| InfluxDB | `http://192.168.1.35:8086` |
| cAdvisor | `http://192.168.1.35:8080` |
| Prometheus | `http://192.168.1.35:9090` |
| Grafana | `http://192.168.1.35:3000` |

---

## 1. Hiểu bản chất luồng dữ liệu

Có 2 luồng metrics khác nhau.

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
- Request per second.
- Virtual users.
- Error rate.
- Checks per second.

### Luồng 2 — API/container monitoring

```text
[.NET API /metrics] ----\
                         \ scrape
                          v
                       [Prometheus] ---> [Grafana datasource Prometheus]
                          ^
                         / scrape
[cAdvisor /metrics] -----
```

Luồng này dùng để xem:

- API target có UP không.
- API request rate.
- API p95 latency từ prometheus-net.
- Container memory.
- Container CPU.

Ghi nhớ: Grafana không tự có data. Grafana chỉ đọc từ datasource.

---

## 2. Đứng đúng thư mục

```bash
cd ~/projects/OpenIdDict_MrGold/docker
pwd
```

Kỳ vọng:

```text
/home/bank/projects/OpenIdDict_MrGold/docker
```

---

## 3. Làm sạch file cấu hình cũ

Vì muốn làm lại từ đầu, xóa cấu hình Grafana/Prometheus cũ rồi tạo lại.

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

## 4. Tạo file `docker-compose.monitoring.yml`

Tạo file đầy đủ:

```bash
cat > docker-compose.monitoring.yml <<'COMPOSE'
version: '3.8'

services:

  # ── API: AuthDemo .NET 8 ────────────────────────────────────────────────────────
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

  # ── cAdvisor: lấy CPU/RAM/network của Docker containers ────────────────────────
  # Quan trọng:
  # - Dùng docker.sock để cAdvisor map được container name/image.
  # - Dùng --docker_only=true để chỉ expose Docker container, tránh toàn system.slice.
  cadvisor:
    image: gcr.io/cadvisor/cadvisor:v0.49.1
    container_name: cadvisor
    privileged: true
    ports:
      - "8080:8080"
    volumes:
      - /:/rootfs:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker/:/var/lib/docker:ro
      - /dev/disk/:/dev/disk:ro
    devices:
      - /dev/kmsg:/dev/kmsg
    command:
      - "--housekeeping_interval=10s"
      - "--docker_only=true"
      - "--disable_metrics=disk,diskIO,hugetlb,referenced_memory,resctrl,cpuLoad,advtcp,process"
    restart: unless-stopped

  # ── Prometheus: scrape API /metrics và cAdvisor /metrics ───────────────────────
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
      api:
        condition: service_healthy
      cadvisor:
        condition: service_started
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
      prometheus:
        condition: service_healthy
      influxdb:
        condition: service_healthy
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

> Nếu `docker compose up` báo lỗi `service api has no healthcheck`, thì Dockerfile API hiện tại chưa có `HEALTHCHECK`. Khi đó đổi phần `depends_on` của Prometheus từ `condition: service_healthy` sang `condition: service_started`. Nếu `docker ps` đang hiện `api (healthy)` thì giữ nguyên như file trên.

---

## 5. Tạo `prometheus/prometheus.yml`

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

  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
PROMETHEUS
```

Prometheus nằm trong Docker network, nên nó gọi API bằng `api:8080`, không dùng `localhost:5000`.

---

## 6. Tạo `grafana/grafana.ini`

```bash
cat > grafana/grafana.ini <<'GRAFANA_INI'
[feature_toggles]
influxdbBackendMigration = false
GRAFANA_INI
```

Lý do: với InfluxDB 1.8 và Grafana mới, tắt backend migration giúp tránh lỗi query InfluxDB v1 bị rỗng.

---

## 7. Tạo datasource InfluxDB

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

## 8. Tạo datasource Prometheus

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

## 9. Down sạch volume và chạy lại

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
cadvisor
prometheus
grafana
```

---

## 10. Verify service bằng curl

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

## 11. Verify datasource Grafana

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

## 12. Verify Prometheus targets

Mở:

```text
http://192.168.1.35:9090/targets
```

Kỳ vọng:

```text
dotnet-api = UP
cadvisor = UP
```

Nếu `dotnet-api` DOWN:

```bash
docker logs api --tail 100
curl http://localhost:5000/metrics | head
```

Nếu `cadvisor` DOWN:

```bash
docker logs cadvisor --tail 100
curl http://localhost:8080/metrics | grep '^container_' | head
```

---

## 13. Verify cAdvisor thấy container Docker thật

Đây là bước rất quan trọng trước khi tạo panel CPU/RAM container.

Chạy:

```bash
curl -s http://localhost:8080/metrics \
  | grep '^container_memory_usage_bytes' \
  | grep -E 'api|docker-api' \
  | head -20
```

Kỳ vọng phải có output. Ví dụ:

```text
container_memory_usage_bytes{...,image="docker-api",name="api",...} 123456789
```

hoặc:

```text
container_memory_usage_bytes{...,image="docker-api",name="/api",...} 123456789
```

Nếu không có output, xem toàn bộ container mà cAdvisor thấy:

```bash
curl -s http://localhost:8080/metrics \
  | grep '^container_memory_usage_bytes' \
  | grep 'image=' \
  | head -50
```

Nếu vẫn chỉ thấy `id="/"` hoặc `id="/system.slice/..."`, nghĩa là cAdvisor chưa đọc được Docker metadata. Kiểm tra lại compose phải có:

```yaml
- /var/run/docker.sock:/var/run/docker.sock:ro
```

và command phải có:

```yaml
- "--docker_only=true"
```

Kiểm tra docker.sock trong container:

```bash
docker exec cadvisor ls -la /var/run/docker.sock
```

Nếu lỗi permission/socket không tồn tại, kiểm tra host:

```bash
ls -la /var/run/docker.sock
```

---

## 14. Chạy k6 để tạo dữ liệu InfluxDB

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

## 15. Tạo dashboard k6 bằng tay trong Grafana

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

## 16. Panel k6 — P95 Response Time

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

Unit:

```text
milliseconds / ms
```

---

## 17. Panel k6 — Average Response Time

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
milliseconds / ms
```

---

## 18. Panel k6 — Requests Per Second

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

## 19. Panel k6 — Virtual Users

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

## 20. Panel k6 — Error Rate %

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

## 21. Panel k6 — Checks Per Second

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

## 22. Lưu dashboard k6

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

## 23. Tạo dashboard Prometheus bằng tay

Tạo dashboard mới:

```text
Dashboards → New → New dashboard → Add visualization → datasource Prometheus
```

---

## 24. Panel Prometheus — API Target UP

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

## 25. Panel Prometheus — API Request Rate

Thử query này:

```promql
sum(rate(http_requests_received_total[1m]))
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

## 26. Panel Prometheus — API P95 Latency

PromQL:

```promql
histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (le))
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

## 27. Panel Prometheus — API Container Memory

Trước khi tạo panel, kiểm tra Prometheus có data không:

```promql
container_memory_usage_bytes
```

Sau đó tìm label của API bằng terminal:

```bash
curl -s http://localhost:8080/metrics \
  | grep '^container_memory_usage_bytes' \
  | grep -E 'api|docker-api' \
  | head -20
```

PromQL khuyên dùng:

```promql
container_memory_usage_bytes{name=~".*api.*|.*docker-api.*", image!=""}
```

Title:

```text
API Container Memory
```

Unit:

```text
bytes
```

Quan trọng: trong Prometheus regex là full-match. Query dưới đây dễ không ra data:

```promql
container_memory_usage_bytes{name=~"api|docker-api"}
```

Vì nó chỉ match chính xác `api` hoặc `docker-api`, không match `/api`, `docker-api:latest`, hoặc label có prefix/suffix. Dùng `.*api.*` an toàn hơn khi học.

---

## 28. Panel Prometheus — API Container CPU

PromQL dạng CPU percent:

```promql
rate(container_cpu_usage_seconds_total{name=~".*api.*|.*docker-api.*", image!=""}[1m]) * 100
```

Title:

```text
API Container CPU %
```

Unit:

```text
percent / %
```

Nếu muốn xem giá trị theo core, không nhân 100:

```promql
rate(container_cpu_usage_seconds_total{name=~".*api.*|.*docker-api.*", image!=""}[1m])
```

Ví dụ `0.25` nghĩa là khoảng 25% của 1 core.

---

## 29. Nếu panel container Memory/CPU không có data

Làm theo thứ tự này.

### Bước 1 — Prometheus có metric container chưa?

Mở Prometheus Graph:

```text
http://192.168.1.35:9090/graph
```

Chạy:

```promql
container_memory_usage_bytes
```

Nếu không có data, Prometheus chưa scrape được cAdvisor hoặc cAdvisor không expose metric.

### Bước 2 — cAdvisor có thấy container API không?

```bash
curl -s http://localhost:8080/metrics \
  | grep '^container_memory_usage_bytes' \
  | grep -E 'api|docker-api' \
  | head -20
```

Nếu không có output, kiểm tra compose cAdvisor.

### Bước 3 — Compose cAdvisor phải là docker.sock + docker_only

Trong `docker-compose.monitoring.yml`, service `cadvisor` phải có:

```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock:ro
command:
  - "--docker_only=true"
```

Không dùng bản containerd này cho lab hiện tại:

```yaml
- /run/containerd/containerd.sock:/run/containerd/containerd.sock:ro
- "--containerd=/run/containerd/containerd.sock"
- "--containerd-namespace=moby"
```

### Bước 4 — Recreate cAdvisor và Prometheus

```bash
cd ~/projects/OpenIdDict_MrGold/docker

docker compose -f docker-compose.monitoring.yml stop cadvisor prometheus
docker compose -f docker-compose.monitoring.yml rm -f cadvisor prometheus
docker compose -f docker-compose.monitoring.yml up -d cadvisor prometheus
```

Đợi 30 giây rồi kiểm tra lại:

```bash
curl -s http://localhost:8080/metrics \
  | grep '^container_memory_usage_bytes' \
  | grep -E 'api|docker-api' \
  | head -20
```

---

## 30. Debug k6 dashboard không có data

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

## 31. Checklist cuối cùng

```text
[ ] docker ps có api, influxdb, cadvisor, prometheus, grafana
[ ] curl localhost:5000/health OK
[ ] curl localhost:5000/metrics có # HELP
[ ] curl -i localhost:8086/ping trả 204
[ ] Prometheus /targets: dotnet-api UP, cadvisor UP
[ ] Grafana datasource có đúng 2 cái: influxdb và Prometheus
[ ] cAdvisor thấy container api/docker-api trong container_memory_usage_bytes
[ ] Chạy k6 với --out influxdb=http://localhost:8086/k6
[ ] InfluxDB SHOW MEASUREMENTS có http_req_duration
[ ] Grafana Explore datasource influxdb query ra data
[ ] Dashboard k6 tự tạo bằng tay có panel P95, RPS, VU, Error Rate
[ ] Dashboard Prometheus có API Target UP, API Container Memory, API Container CPU
```

---

## 32. Lệnh thường dùng

Restart toàn stack:

```bash
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.monitoring.yml restart
```

Xem log API:

```bash
docker logs api --tail 100 -f
```

Xem log cAdvisor:

```bash
docker logs cadvisor --tail 100 -f
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

---

## 33. Ghi nhớ quan trọng

- `down -v` xóa sạch InfluxDB data, nên phải chạy lại k6.
- Grafana datasource có thể provision tự động.
- Dashboard nếu muốn học bản chất thì tự tạo bằng UI.
- Dashboard community như ID `2587` có thể không khớp schema/version, không nên phụ thuộc.
- Query k6 phải dựa trên measurement thật trong InfluxDB: `http_req_duration`, `http_reqs`, `vus`, `checks`, `http_req_failed`.
- Query cAdvisor phải dựa trên label thật mà cAdvisor expose.
- Với Prometheus regex, `name=~"api|docker-api"` là match chính xác. Khi không chắc label, dùng `name=~".*api.*|.*docker-api.*"`.
