# Lab Scale — Nginx Load Balancer + Multi-VM API

## Mục lục

- [Kiến trúc lab](#kiến-trúc-lab)
- [Phase 1 — Tìm điểm chết của single server](#phase-1--tìm-điểm-chết-của-single-server)
- [Phase 2 — Chuẩn bị VM2 chạy API thứ hai](#phase-2--chuẩn-bị-vm2-chạy-api-thứ-hai)
- [Phase 3 — Chuẩn bị VM3 chạy Nginx](#phase-3--chuẩn-bị-vm3-chạy-nginx)
- [Phase 4 — Cập nhật Prometheus scrape cả 2 API](#phase-4--cập-nhật-prometheus-scrape-cả-2-api)
- [Phase 5 — Chạy lại k6 sau khi scale và so sánh](#phase-5--chạy-lại-k6-sau-khi-scale-và-so-sánh)
- [Hiểu kết quả — Đọc dashboard trước và sau scale](#hiểu-kết-quả--đọc-dashboard-trước-và-sau-scale)
- [Khi Database là bottleneck — Scale DB Layer](#khi-database-là-bottleneck--scale-db-layer)
- [Debug thường gặp](#debug-thường-gặp)
- [Checklist cuối cùng](#checklist-cuối-cùng)

---

## Kiến trúc lab

### Trước khi scale (1 server)

```
[k6 — máy của bạn]
        |
        | POST/GET requests
        v
[VM1 — 192.168.1.35]
  ├── api (Docker :5000)          ← chịu toàn bộ tải
  ├── sqlserver (Docker :1433)
  ├── influxdb (Docker :8086)
  ├── prometheus (Docker :9090)
  └── grafana (Docker :3000)
```

### Sau khi scale (3 server)

```
[k6 — máy của bạn]
        |
        | POST/GET requests
        v
[VM3 — 192.168.1.37] Nginx :80
  ├── upstream api1  →  VM1 :5000
  └── upstream api2  →  VM2 :5000
                          |
        ┌─────────────────┘
        |
[VM1 — 192.168.1.35]          [VM2 — 192.168.1.36]
  ├── api (Docker :5000)         └── api (Docker :5000)
  ├── sqlserver (Docker :1433)      (cùng DB ở VM1)
  ├── influxdb (Docker :8086)
  ├── prometheus (Docker :9090)
  └── grafana (Docker :3000)
```

**Điểm mấu chốt:**
- SQL Server vẫn ở VM1, cả 2 API instance đều kết nối vào đây.
- Database là shared — đây là kiến trúc stateless API phổ biến.
- Nginx phân phối request theo round-robin (request 1 → API1, request 2 → API2, ...).
- k6 gửi request đến Nginx, không biết có mấy API phía sau.

---

## Thông tin VM trong lab

| VM | IP | Vai trò | Cài gì |
|---|---|---|---|
| VM1 | `192.168.1.35` | Main server (đã có) | API + SQL Server + Monitoring |
| VM2 | `192.168.1.36` | API instance 2 (mới tạo) | Chỉ cần Docker + API |
| VM3 | `192.168.1.37` | Load balancer (mới tạo) | Chỉ cần Docker + Nginx |

> Chỉnh IP theo thực tế VMware của bạn. Tài liệu này dùng 3 địa chỉ trên.

---

## Chuẩn bị — Checkout branch và khởi động từ đầu trên VM1

Branch `technical_leader_scale` có thay đổi bắt buộc so với branch cũ: API **không còn tự fallback về ephemeral key** — nếu không có cert file thì container crash ngay khi start. Vì vậy phải sinh cert **trước** khi `docker compose up`.

### Bước 0.1 — Checkout branch trên VM1

```bash
cd ~/projects/OpenIdDict_MrGold

git fetch origin
git checkout technical_leader_scale
git pull
```

### Bước 0.2 — Dọn sạch stack cũ

`down -v` dừng tất cả container **và xóa toàn bộ volume** (InfluxDB data, Prometheus data, Grafana dashboards, API logs). Cần làm điều này để tránh conflict config cũ còn sót trong volume.

```bash
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.yaml down -v
```

Verify sạch:

```bash
docker ps -a | grep -E "api|influxdb|prometheus|grafana|cadvisor"
# Kỳ vọng: không còn container nào trong danh sách
```

### Bước 0.3 — Sinh certificate (bắt buộc, làm 1 lần)

Branch này yêu cầu cert file trước khi `docker compose up`. Nếu bỏ qua bước này, container `api` sẽ crash với lỗi `InvalidOperationException: OpenIddict: Biến môi trường 'OpenIddict__CertPath' chưa được cấu hình`.

```bash
cd ~/projects/OpenIdDict_MrGold/docker/certs
chmod +x gen-cert.sh
./gen-cert.sh
```

Verify cert đã tồn tại và có permissions đúng:

```bash
ls -lh ~/projects/OpenIdDict_MrGold/docker/certs/
# Kỳ vọng: thấy openiddict.pfx (~4KB) với permissions -rw-r--r-- (644)

# Verify file PFX hợp lệ (không có output lỗi = tốt)
openssl pkcs12 -in ~/projects/OpenIdDict_MrGold/docker/certs/openiddict.pfx \
  -noout -passin pass:Lab@OpenIddict2025
```

> **Lưu ý permissions:** `gen-cert.sh` tự động đặt `chmod 644` cho file `.pfx` sau khi tạo. Container chạy bằng user `appuser` (uid 1001, không phải `bank`) — nếu file là `600` (owner-only), container không đọc được và crash với lỗi `BIO routines::system lib`. Nếu bạn sinh cert bằng script cũ hoặc bằng tay, chạy thêm:
> ```bash
> chmod 644 ~/projects/OpenIdDict_MrGold/docker/certs/openiddict.pfx
> ```

### Bước 0.4 — Khởi động lại stack

```bash
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.yaml up -d --build
```

Theo dõi quá trình start (đặc biệt container `api`):

```bash
docker compose -f docker-compose.yaml logs -f api
```

Kỳ vọng thấy trong log:
```
Application started. Press Ctrl+C to shut down.
```

Nếu thấy `InvalidOperationException` → cert chưa được mount đúng, kiểm tra lại Bước 0.3.

**Nếu container `api` báo unhealthy và `docker compose up -d` vẫn lỗi dù đã fix cert:**

`docker compose up -d` không tự force-recreate container đang trong restart loop — nó chỉ report lại trạng thái cũ. Cần xóa container cũ và tạo lại:

```bash
docker stop api && docker rm api
docker compose -f docker-compose.yaml up -d
docker logs api --tail 30
```

Verify toàn bộ stack healthy:

```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
# Kỳ vọng: api, influxdb, prometheus, grafana, cadvisor đều Up ... (healthy)

curl http://localhost:5000/health
# Kỳ vọng: {"status":"Healthy"}
```

> **Lưu ý về `-v`:** Vì volume bị xóa, Grafana mất toàn bộ dashboard đã import. Cần import lại 3 dashboard (ID 2587, 10915, 14282) như hướng dẫn trong answer.md Bước 3.3. Datasource InfluxDB và Prometheus được auto-provision lại từ file — không cần tạo lại.

---

## Phase 1 — Tìm điểm chết của single server

Mục tiêu: **ghi lại ngưỡng mà API bắt đầu fail khi chỉ có 1 instance**. Số liệu này là baseline để so sánh sau khi scale.

### Bước 1.1 — Khởi động stack hiện tại trên VM1

```bash
# Trên VM1
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.yaml up -d --build
```

Verify:
```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
curl http://localhost:5000/health
```

### Bước 1.2 — Tạo script stress test tìm ngưỡng chết

Tạo file `k6/stress-breakpoint.js`:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

// Stress test: tăng liên tục đến khi hệ thống fail
export const options = {
  stages: [
    { duration: '2m',  target: 50   },  // khởi động nhẹ
    { duration: '3m',  target: 100  },  // tải nhẹ — baseline
    { duration: '3m',  target: 200  },  // tải vừa
    { duration: '3m',  target: 400  },  // bắt đầu stress
    { duration: '3m',  target: 600  },  // stress nặng
    { duration: '3m',  target: 800  },  // ngưỡng nguy hiểm
    { duration: '3m',  target: 1000 },  // thường die ở đây với 2 core
    { duration: '2m',  target: 0    },  // cool down — xem có recover không
  ],
  thresholds: {
    // Không fail test tự động, để quan sát đến tận cùng
    http_req_duration: ['p(95)<10000'],
    http_req_failed: ['rate<0.5'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://192.168.1.35:5000';

// Danh sách user seed sẵn (smoke test trước để xác nhận user tồn tại)
function getUser(vu) {
  const idx = String(vu % 100 + 1).padStart(3, '0');
  return { username: `loadtest_${idx}@test.com`, password: 'TestPass@123' };
}

export default function () {
  const user = getUser(__VU);

  // Step 1: Login
  const loginRes = http.post(`${BASE_URL}/connect/token`,
    `grant_type=password&client_id=angular-spa&username=${user.username}&password=${user.password}&scope=openid profile email roles`,
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
  );

  const loginOk = check(loginRes, {
    'login 200': (r) => r.status === 200,
    'has token': (r) => r.json('access_token') !== undefined,
  });

  if (!loginOk) {
    sleep(1);
    return;
  }

  const token = loginRes.json('access_token');
  const headers = { Authorization: `Bearer ${token}` };

  // Step 2: Gọi API (thay bằng endpoint thực của bạn)
  const meRes = http.get(`${BASE_URL}/api/user/info`, { headers });
  check(meRes, { 'user info 200': (r) => r.status === 200 });

  sleep(1);
}
```

### Bước 1.3 — Chạy stress test và ghi lại số liệu

```bash
# Chạy từ máy của bạn (hoặc trực tiếp trên VM1)
k6 run \
  --out influxdb=http://192.168.1.35:8086/k6 \
  --env BASE_URL=http://192.168.1.35:5000 \
  k6/stress-breakpoint.js
```

**Trong lúc test chạy, theo dõi song song:**

Terminal 2 — Xem container resource thật:
```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

Grafana — Mở 2 tab:
```
http://192.168.1.35:3000 → Dashboard k6 (ID 2587)   → theo dõi latency, error rate
http://192.168.1.35:3000 → Dashboard .NET (ID 10915) → theo dõi GC, thread pool
```

### Bước 1.4 — Ghi lại kết quả baseline

Sau khi test xong, ghi vào bảng này (nhìn từ k6 terminal output):

```
===== BASELINE — 1 API INSTANCE =====

VU mà error rate bắt đầu tăng:      _____ VU
VU mà p95 > 1 giây:                  _____ VU
VU mà p95 > 3 giây:                  _____ VU
VU mà error rate > 5%:               _____ VU

Tại peak (1000 VU):
  p50 latency:     _____ms
  p95 latency:     _____ms
  p99 latency:     _____ms
  error rate:      _____%
  throughput:      _____ req/s

CPU API container lúc peak:          _____%
RAM API container lúc peak:          _____MB
```

> Thông thường với 2 core + SQL Server chung máy: hệ thống bắt đầu suy giảm ở ~300–500 VU và fail rõ ràng ở 600–1000 VU.

---

## Phase 2 — Chuẩn bị VM2 chạy API thứ hai

### Bước 2.1 — Tạo VM2 trong VMware

Yêu cầu tối thiểu VM2:
- OS: Ubuntu Server 22.04 LTS (giống VM1)
- CPU: 2 core
- RAM: 4 GB (API .NET không cần nhiều khi không có SQL Server)
- Disk: 20 GB
- Network: cùng subnet với VM1 (192.168.1.x)

Sau khi tạo xong, đặt IP tĩnh:
```bash
# Trên VM2 — Ubuntu 22.04
sudo nano /etc/netplan/00-installer-config.yaml
```

```yaml
network:
  version: 2
  ethernets:
    ens33:           # tên interface thay theo thực tế (ip a để xem)
      dhcp4: false
      addresses:
        - 192.168.1.36/24
      gateway4: 192.168.1.1
      nameservers:
        addresses: [8.8.8.8, 8.8.4.4]
```

```bash
sudo netplan apply
ping 192.168.1.35   # verify thông với VM1
```

### Bước 2.2 — Cài Docker trên VM2

```bash
# Trên VM2
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
```

### Bước 2.3 — Sao chép source code sang VM2

**Cách 1 — Dùng scp (đơn giản nhất):**

```bash
# Chạy từ máy Windows của bạn
scp -r /path/to/OpenIdDict_MrGold bank@192.168.1.36:~/projects/
```

**Cách 2 — Git clone (nếu có repo):**

```bash
# Trên VM2
mkdir -p ~/projects
cd ~/projects
git clone <your-repo-url> OpenIdDict_MrGold
```

### Bước 2.4 — Sinh certificate dùng chung và copy sang VM2

File `docker/docker-compose.api-only.yml` đã có sẵn trong repo (được tạo kèm code). File này đã cấu hình đầy đủ cert. Việc cần làm là sinh cert file **1 lần duy nhất** trên VM1 rồi copy sang VM2.

**Trên VM1 — sinh cert:**

```bash
cd ~/projects/OpenIdDict_MrGold/docker/certs
chmod +x gen-cert.sh
./gen-cert.sh
# Sinh ra: certs/openiddict.pfx (password: Lab@OpenIddict2025)
```

**Copy cert sang VM2:**

```bash
# Chạy từ VM1 (thay user/IP cho đúng)
scp ~/projects/OpenIdDict_MrGold/docker/certs/openiddict.pfx \
    bank@192.168.1.36:~/projects/OpenIdDict_MrGold/docker/certs/
```

> **Tại sao phải dùng cùng 1 file cert?** OpenIddict dùng private key trong cert để ký JWT. Token do VM1 ký chỉ được VM2 verify nếu VM2 có cùng public key. File `.pfx` chứa cả 2. Nếu mỗi VM tự sinh cert riêng → token từ VM1 sẽ bị VM2 từ chối với lỗi 401.

Nội dung `docker-compose.api-only.yml` trong repo:

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
      # Trỏ về SQL Server ở VM1 — VM2 không chạy SQL Server riêng
      - ConnectionStrings__DefaultConnection=Server=192.168.1.35,1433;Database=AuthDemoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
      # PHẢI dùng cùng cert với VM1 — token do VM1 ký thì VM2 mới validate được và ngược lại
      - OpenIddict__CertPath=/app/certs/openiddict.pfx
      - OpenIddict__CertPassword=Lab@OpenIddict2025
    volumes:
      - api-logs:/app/logs
      - ./certs/openiddict.pfx:/app/certs/openiddict.pfx:ro
    restart: unless-stopped

volumes:
  api-logs:
```

> VM2 không cài SQL Server. API trên VM2 kết nối về SQL Server ở VM1 qua IP `192.168.1.35:1433`.
> Đây là kiến trúc chuẩn: DB tập trung, API stateless có thể scale ngang.

### Bước 2.5 — Cho phép SQL Server ở VM1 nhận kết nối từ VM2

Trên VM1, kiểm tra firewall:

```bash
# Trên VM1 — kiểm tra port 1433 đang mở chưa
sudo ufw status
sudo ufw allow from 192.168.1.36 to any port 1433
```

Nếu SQL Server chạy trong Docker và port 1433 đã map ra host, thì VM2 đã kết nối được rồi.

Verify từ VM2:
```bash
# Trên VM2 — test kết nối TCP đến SQL Server ở VM1
nc -zv 192.168.1.35 1433
# Kỳ vọng: Connection to 192.168.1.35 1433 port [tcp/*] succeeded!
```

### Bước 2.6 — Build và chạy API trên VM2

```bash
# Trên VM2
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.api-only.yml up -d --build
```

Build lần đầu mất 3–5 phút. Sau khi xong:

```bash
docker ps
curl http://localhost:5000/health
# Kỳ vọng: {"status":"Healthy"}
```

Verify từ VM1 hoặc máy của bạn:
```bash
curl http://192.168.1.36:5000/health
curl http://192.168.1.36:5000/metrics | head -10
```

> **Nếu API trên VM2 fail do migration:** API tự chạy migration khi khởi động. Nếu VM1 đã migrate rồi, VM2 sẽ thấy DB đã đúng schema và chỉ chạy seed (idempotent). Không sao.

---

## Phase 3 — Chuẩn bị VM3 chạy Nginx

### Bước 3.1 — Tạo VM3 trong VMware

Yêu cầu tối thiểu VM3:
- OS: Ubuntu Server 22.04 LTS
- CPU: 1 core (Nginx rất nhẹ)
- RAM: 1 GB
- Disk: 10 GB
- IP: `192.168.1.37`

Cài Docker tương tự VM2:
```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
```

### Bước 3.2 — Tạo cấu hình Nginx trên VM3

```bash
# Trên VM3
mkdir -p ~/nginx-lb
cd ~/nginx-lb
```

Tạo file `nginx.conf`:

```nginx
# ~/nginx-lb/nginx.conf

events {
    # Số connection đồng thời Nginx xử lý được
    # 1024 là đủ cho lab; production thường đặt 4096–65536
    worker_connections 1024;
}

http {

    # ── Định nghĩa upstream (danh sách backend) ──────────────────────────────
    upstream api_backend {
        # Round-robin mặc định: request 1→api1, request 2→api2, request 3→api1...
        server 192.168.1.35:5000;   # API trên VM1
        server 192.168.1.36:5000;   # API trên VM2

        # Keepalive: giữ persistent connection đến backend
        # Tránh tốn chi phí mở TCP connection mới cho mỗi request
        keepalive 32;
    }

    # ── Server block: lắng nghe port 80 ──────────────────────────────────────
    server {
        listen 80;
        server_name _;   # nhận tất cả hostname

        # Timeout: phù hợp với API có thể chậm khi tải cao
        proxy_connect_timeout 60s;
        proxy_send_timeout    60s;
        proxy_read_timeout    60s;

        # Header để API biết IP thật của client (quan trọng cho logging)
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Bật keepalive đến upstream
        proxy_http_version 1.1;
        proxy_set_header Connection "";

        # Tất cả request → forward đến upstream api_backend
        location / {
            proxy_pass http://api_backend;
        }
    }

    # ── Endpoint kiểm tra trạng thái Nginx (dùng cho health check) ───────────
    server {
        listen 8080;
        location /nginx-health {
            return 200 "nginx ok\n";
            add_header Content-Type text/plain;
        }

        # Xem upstream status (tùy chọn — cần Nginx Plus cho chi tiết hơn)
        location /nginx-status {
            stub_status on;
        }
    }
}
```

Tạo file `docker-compose.yml` trên VM3:

```yaml
# ~/nginx-lb/docker-compose.yml
services:
  nginx:
    image: nginx:1.25-alpine
    container_name: nginx-lb
    ports:
      - "80:80"       # Load balancer port — k6 sẽ gọi vào đây
      - "8080:8080"   # Health check + status
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/nginx-health"]
      interval: 10s
      timeout: 5s
      retries: 3
```

### Bước 3.3 — Chạy Nginx trên VM3

```bash
# Trên VM3
cd ~/nginx-lb
docker compose up -d
```

Verify Nginx chạy đúng:

```bash
# Kiểm tra Nginx healthy
curl http://localhost:8080/nginx-health
# Kỳ vọng: nginx ok

# Kiểm tra Nginx phân phối đến API
curl http://localhost/health
# Kỳ vọng: {"status":"Healthy"} (từ một trong 2 API backend)
```

Verify từ máy của bạn:
```bash
curl http://192.168.1.37/health
curl http://192.168.1.37/metrics | head -5
```

### Bước 3.4 — Confirm round-robin đang hoạt động

Cách đơn giản nhất: gọi nhiều lần và xem log của 2 API container để thấy request được phân phối.

Trên VM1:
```bash
docker logs api --follow | grep "Request"
```

Trên VM2:
```bash
docker logs api --follow | grep "Request"
```

Từ máy của bạn, gọi 10 request:
```bash
for i in $(seq 1 10); do curl -s http://192.168.1.37/health; echo " - request $i"; done
```

Cả 2 log sẽ thấy request được ghi xen kẽ → round-robin đang chạy.

---

## Phase 4 — Cập nhật Prometheus scrape cả 2 API

Prometheus hiện chỉ scrape VM1. Sau khi scale, cần scrape cả VM2.

### Bước 4.1 — Cập nhật prometheus.yml trên VM1

Mở file `~/projects/OpenIdDict_MrGold/docker/prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 5s
  evaluation_interval: 5s

scrape_configs:

  # ── Scrape API trên VM1 ─────────────────────────────────────────────────────
  - job_name: 'dotnet-api-vm1'
    static_configs:
      - targets: ['api:8080']   # container name trong Docker network của VM1
    metrics_path: '/metrics'
    relabel_configs:
      - target_label: instance
        replacement: 'vm1'

  # ── Scrape API trên VM2 ─────────────────────────────────────────────────────
  - job_name: 'dotnet-api-vm2'
    static_configs:
      - targets: ['192.168.1.36:5000']   # IP:port của VM2 (Prometheus gọi ra ngoài)
    metrics_path: '/metrics'
    relabel_configs:
      - target_label: instance
        replacement: 'vm2'
```

> **Lý do khác nhau:** API VM1 dùng `api:8080` (Docker service name), còn API VM2 dùng `192.168.1.36:5000` (IP thật) vì Prometheus container chỉ biết Docker network của VM1, không tự động resolve service name của VM2.

### Bước 4.2 — Reload Prometheus config (không cần restart)

```bash
# Trên VM1 — reload config không downtime
curl -X POST http://localhost:9090/-/reload
```

Verify ngay sau đó:
```
Mở http://192.168.1.35:9090/targets
Kỳ vọng thấy:
  dotnet-api-vm1  UP  (api:8080)
  dotnet-api-vm2  UP  (192.168.1.36:5000)
```

### Bước 4.3 — Tạo Grafana dashboard so sánh 2 instance

Tạo dashboard mới trong Grafana, thêm panel với PromQL so sánh cả 2:

**Panel: CPU Usage cả 2 instance**

```promql
rate(process_cpu_seconds_total[1m]) * 100
```

Legend format: `{{instance}}`

Bạn sẽ thấy 2 đường: `vm1` và `vm2`, cho phép so sánh tải phân phối có đều không.

**Panel: Memory cả 2 instance**

```promql
process_resident_memory_bytes
```

Legend format: `{{instance}}`

**Panel: Request rate cả 2 instance**

```promql
sum(rate(http_requests_received_total[1m])) by (instance)
```

---

## Phase 5 — Chạy lại k6 sau khi scale và so sánh

### Bước 5.1 — Tạo script stress test trỏ vào Nginx

Tạo file `k6/stress-after-scale.js` — **giống hệt** `stress-breakpoint.js` nhưng đổi BASE_URL:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  // Dùng ĐÚNG ngưỡng stress như Phase 1 để so sánh công bằng
  stages: [
    { duration: '2m',  target: 50   },
    { duration: '3m',  target: 100  },
    { duration: '3m',  target: 200  },
    { duration: '3m',  target: 400  },
    { duration: '3m',  target: 600  },
    { duration: '3m',  target: 800  },
    { duration: '3m',  target: 1000 },
    { duration: '2m',  target: 0    },
  ],
  thresholds: {
    http_req_duration: ['p(95)<10000'],
    http_req_failed: ['rate<0.5'],
  },
};

// Trỏ vào Nginx thay vì API trực tiếp
const BASE_URL = __ENV.BASE_URL || 'http://192.168.1.37';

function getUser(vu) {
  const idx = String(vu % 100 + 1).padStart(3, '0');
  return { username: `loadtest_${idx}@test.com`, password: 'TestPass@123' };
}

export default function () {
  const user = getUser(__VU);

  const loginRes = http.post(`${BASE_URL}/connect/token`,
    `grant_type=password&client_id=angular-spa&username=${user.username}&password=${user.password}&scope=openid profile email roles`,
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
  );

  const loginOk = check(loginRes, {
    'login 200': (r) => r.status === 200,
    'has token': (r) => r.json('access_token') !== undefined,
  });

  if (!loginOk) {
    sleep(1);
    return;
  }

  const token = loginRes.json('access_token');
  const headers = { Authorization: `Bearer ${token}` };

  const meRes = http.get(`${BASE_URL}/api/user/info`, { headers });
  check(meRes, { 'user info 200': (r) => r.status === 200 });

  sleep(1);
}
```

### Bước 5.2 — Dọn sạch InfluxDB để số liệu không lẫn

```bash
# Trên VM1 — xóa data k6 cũ để dashboard chỉ hiện kết quả mới
curl -XPOST "http://localhost:8086/query?db=k6" --data-urlencode "q=DROP MEASUREMENT http_req_duration"
curl -XPOST "http://localhost:8086/query?db=k6" --data-urlencode "q=DROP MEASUREMENT http_reqs"
curl -XPOST "http://localhost:8086/query?db=k6" --data-urlencode "q=DROP MEASUREMENT http_req_failed"
curl -XPOST "http://localhost:8086/query?db=k6" --data-urlencode "q=DROP MEASUREMENT vus"
```

### Bước 5.3 — Chạy stress test qua Nginx

```bash
k6 run \
  --out influxdb=http://192.168.1.35:8086/k6 \
  --env BASE_URL=http://192.168.1.37 \
  k6/stress-after-scale.js
```

Trong lúc chạy, xem:
- Grafana k6 dashboard → latency, error rate
- Grafana Prometheus dashboard → cả 2 instance vm1 và vm2
- Terminal: `docker stats` trên cả VM1 và VM2

### Bước 5.4 — Ghi lại kết quả sau scale

```
===== SAU KHI SCALE — 2 API INSTANCE + NGINX =====

VU mà error rate bắt đầu tăng:      _____ VU   (so với trước: _____)
VU mà p95 > 1 giây:                  _____ VU   (so với trước: _____)
VU mà p95 > 3 giây:                  _____ VU   (so với trước: _____)

Tại peak (1000 VU):
  p50 latency:     _____ms    (trước: _____ms)
  p95 latency:     _____ms    (trước: _____ms)
  p99 latency:     _____ms    (trước: _____ms)
  error rate:      _____%     (trước: _____%  )
  throughput:      _____ req/s (trước: _____ req/s)

CPU VM1 API lúc peak:    _____% (trước: ____%)
CPU VM2 API lúc peak:    _____%
```

---

## Hiểu kết quả — Đọc dashboard trước và sau scale

### Điều kỳ vọng thấy sau khi scale

**1. Latency giảm**

```
Trước: p95 tại 600 VU = 3500ms (gần chết)
Sau:   p95 tại 600 VU = 800ms  (thoải mái)
```

Lý do: mỗi API instance chỉ phải xử lý ~300 VU thay vì 600 VU.

**2. CPU của mỗi instance giảm đôi**

```
Trước: VM1 API = 90% CPU tại 600 VU
Sau:   VM1 API ≈ 45%, VM2 API ≈ 45% tại 600 VU
```

Nginx phân phối đều → tổng tải vẫn vậy nhưng mỗi máy chịu một nửa.

**3. Throughput (req/s) tăng**

```
Trước: 200 req/s tại 1000 VU (nhiều request timeout/fail)
Sau:   350+ req/s tại 1000 VU (ít timeout hơn)
```

**4. SQL Server vẫn là bottleneck**

Scaling API không giúp nếu SQL Server là cổ chai. Khi cả 2 API cùng gọi DB, số connection tăng gấp đôi. Xem trong SSMS:

```sql
SELECT DB_NAME(dbid) AS db, COUNT(*) AS connections
FROM sys.sysprocesses WHERE dbid > 0
GROUP BY dbid ORDER BY connections DESC;
```

Nếu SQL Server là bottleneck (latency vẫn cao, CPU SQL Server cao), giải pháp tiếp theo là:
- Thêm index
- Redis cache cho GET endpoint
- Read replica SQL Server

### Biểu đồ kỳ vọng

```
Latency p95 (ms)
     |
5000 |  ╭──── 1 instance
4000 |  │
3000 |  │         ╭──── 2 instances
2000 |  │         │
1000 |──┤         │
  0  └──┴─────────┴────────────────→ VU
      100  300  500  700  1000

       ↑                  ↑
   Ngưỡng bão hòa     Ngưỡng bão hòa
   1 instance          sau scale
   (~400 VU)           (~800 VU)
```

---

## Khi Database là bottleneck — Scale DB Layer

> **Bối cảnh:** API đã scale ngang được (stateless, nhiều instance). Database là shared state — không thể scale ngang đơn giản như API. Phần này áp dụng khi DB ở server riêng (không chung VM với API) và bắt đầu quá tải.

### Nhận biết DB đang là bottleneck

Dấu hiệu trên Grafana / monitoring:

```
✗ API latency cao nhưng CPU API thấp → thời gian chờ ở DB, không phải CPU API
✗ CPU DB server cao (>80%) liên tục
✗ Connection wait time tăng trong DB metrics
✗ Throughput API không tăng dù thêm instance → cổ chai không phải ở API
```

Kiểm tra nhanh số connection đang mở (SQL Server):

```sql
-- Chạy trên SQL Server
SELECT DB_NAME(dbid) AS db, COUNT(*) AS connections
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid
ORDER BY connections DESC;

-- Xem query nào đang chạy lâu nhất
SELECT TOP 10
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_ms,
    qs.execution_count,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(qt.text)
          ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY avg_elapsed_ms DESC;
```

Kiểm tra nhanh (PostgreSQL):

```sql
-- Số connection hiện tại
SELECT count(*), state FROM pg_stat_activity GROUP BY state;

-- Query chạy lâu nhất
SELECT pid, now() - pg_stat_activity.query_start AS duration, query, state
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '1 second'
ORDER BY duration DESC;
```

---

### Giải pháp 1 — Tune Connection Pool (làm đầu tiên, miễn phí)

**Vấn đề:** Mỗi API instance giữ một pool connection riêng. 2 instance × max 100 connection = 200 connection đến DB. Khi tải cao, pool bị cạn → request phải chờ connection rảnh → latency tăng.

**Trong connection string của .NET**, thêm các tham số pool:

```
# SQL Server
Server=192.168.1.xx,1433;Database=AuthDemoDB;
User Id=sa;Password=...;TrustServerCertificate=True;
Max Pool Size=50;           # tối đa connection per instance (default 100)
Min Pool Size=5;            # giữ sẵn connection (tránh overhead tạo mới)
Connection Timeout=30;      # giây chờ lấy connection trước khi throw exception
```

```
# PostgreSQL
Host=192.168.1.xx;Port=5432;Database=mydb;Username=myuser;Password=...;
Maximum Pool Size=50;
Minimum Pool Size=5;
Connection Timeout=30;
```

**Tính toán Max Pool Size hợp lý:**

```
DB max connections = 200 (ví dụ)
Số API instance     = 2
Reserve cho admin   = 10

Max Pool Size per instance = (200 - 10) / 2 = 95
→ Đặt Max Pool Size=90 để an toàn
```

> Với SQL Server, giới hạn connection mặc định rất cao (32767). Với PostgreSQL, mặc định chỉ 100 — quan trọng hơn nhiều.

---

### Giải pháp 2 — Redis Cache (giảm số lần đọc DB)

**Nguyên lý:** Phần lớn request là READ (GET user info, GET danh sách, ...). Nếu cache kết quả ở Redis, DB chỉ bị gọi lần đầu — các request tiếp theo lấy từ cache.

```
Không cache:  1000 request/s → 1000 DB query/s
Có cache:     1000 request/s → ~10 DB query/s (chỉ cache miss mới gọi DB)
```

**Kiến trúc sau khi thêm Redis:**

```
[k6] → [Nginx :80] → [API VM1 :5000] ─┐
                    → [API VM2 :5000] ─┼→ [Redis :6379] → (cache hit → trả về ngay)
                                        └→ [SQL Server :1433] → (cache miss → query DB → lưu Redis)
```

**Thêm Redis vào docker-compose trên VM DB server (hoặc VM riêng):**

```yaml
services:
  redis:
    image: redis:7-alpine
    container_name: redis
    ports:
      - "6379:6379"
    command: redis-server --maxmemory 512mb --maxmemory-policy allkeys-lru
    restart: unless-stopped
```

**Trong .NET — cài package và dùng:**

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    // ví dụ: "192.168.1.xx:6379"
});
```

```csharp
// Trong controller/service — pattern cache-aside
public async Task<UserInfo> GetUserInfoAsync(string userId)
{
    var cacheKey = $"user:info:{userId}";

    // Thử lấy từ cache trước
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
        return JsonSerializer.Deserialize<UserInfo>(cached);

    // Cache miss → query DB
    var user = await _db.Users.FindAsync(userId);

    // Lưu vào cache, TTL 5 phút
    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    return user;
}
```

> **Lưu ý invalidation:** Khi user cập nhật thông tin, phải xóa cache tương ứng (`_cache.RemoveAsync(cacheKey)`), nếu không user thấy data cũ.

---

### Giải pháp 3 — Read Replica (scale đọc ngang)

**Nguyên lý:** DB chia làm 2 vai trò:
- **Primary (master):** nhận tất cả WRITE (INSERT, UPDATE, DELETE)
- **Replica (slave):** nhận tất cả READ (SELECT) — sync data từ primary liên tục

```
[API] → Write request → [DB Primary :5432/1433]
                                │
                          replication
                                │
[API] → Read request  → [DB Replica :5432/1433]
```

Vì ~80–90% request thường là READ → replica gánh phần lớn tải → primary nhẹ hơn nhiều.

#### SQL Server — AlwaysOn Availability Group

```
Yêu cầu: SQL Server Enterprise hoặc Developer Edition
         (Standard Edition chỉ hỗ trợ Basic AG, 1 database per group)

Kiến trúc tối giản:
  Primary: 192.168.1.xx  — nhận read + write
  Secondary: 192.168.1.yy — nhận read-only (readable secondary)
```

Trong connection string .NET để tự động route:

```
# Kết nối Primary cho write
Server=192.168.1.xx,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadWrite

# Kết nối Secondary cho read
Server=192.168.1.yy,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadOnly
```

Hoặc dùng AG Listener (DNS name tự route):

```
Server=ag-listener,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadOnly
# Listener tự biết chuyển ReadOnly → secondary, ReadWrite → primary
```

#### PostgreSQL — Streaming Replication

PostgreSQL native replication đơn giản hơn SQL Server, không cần license đặc biệt.

**Trên Primary server** — cho phép replication:

```bash
# postgresql.conf
wal_level = replica
max_wal_senders = 3
wal_keep_size = 256MB   # giữ WAL đủ lâu cho replica đồng bộ
```

```bash
# pg_hba.conf — cho phép replica user kết nối
host  replication  replicator  192.168.1.yy/32  md5
```

**Trên Replica server** — pull data từ primary:

```bash
# Lấy base backup từ primary
pg_basebackup -h 192.168.1.xx -U replicator -D /var/lib/postgresql/data -P -Xs -R
# -R tự tạo file standby.signal và postgresql.auto.conf
```

```bash
# postgresql.auto.conf (tự sinh bởi -R)
primary_conninfo = 'host=192.168.1.xx port=5432 user=replicator password=...'
```

Replica sẽ ở chế độ read-only tự động — mọi SELECT đều chạy được, INSERT/UPDATE sẽ báo lỗi.

**Trong .NET — dùng Npgsql với read/write routing:**

```csharp
// Cài package
// dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

// Cấu hình 2 connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Primary")));

// DbContext thứ 2 cho read-only operations
builder.Services.AddDbContext<ReadOnlyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Replica")));
```

```
# appsettings.json
"ConnectionStrings": {
  "Primary": "Host=192.168.1.xx;Database=mydb;Username=app;Password=...;",
  "Replica": "Host=192.168.1.yy;Database=mydb;Username=app;Password=...;Target Session Attributes=read-only;"
}
```

---

### Giải pháp 4 — PgBouncer (bắt buộc với PostgreSQL ở tải cao)

> **Chỉ cần cho PostgreSQL.** SQL Server dùng thread-based model nên xử lý nhiều connection tốt hơn. PostgreSQL dùng process-based model — mỗi connection tạo 1 process riêng → 500 connection = 500 process OS → tốn RAM và context switch cực nhiều.

**PgBouncer là connection pooler** đứng giữa API và PostgreSQL:

```
[API instance 1]  ─┐
[API instance 2]  ─┼→ [PgBouncer :5432] → [PostgreSQL :5432]
[API instance 3]  ─┘    (pool 20 conn)      (chỉ nhận 20 conn thật)
  (mỗi cái 100 conn)
  = 300 conn đến PgBouncer
```

PgBouncer nhận 300 connection từ API nhưng chỉ giữ 20 connection thật đến PostgreSQL — multiplexing request.

**Docker Compose cho PgBouncer:**

```yaml
services:
  pgbouncer:
    image: edoburu/pgbouncer:latest
    container_name: pgbouncer
    ports:
      - "5432:5432"     # API kết nối vào đây thay vì PostgreSQL trực tiếp
    environment:
      - DB_HOST=192.168.1.xx   # IP PostgreSQL thật
      - DB_PORT=5432
      - DB_USER=myuser
      - DB_PASSWORD=mypassword
      - DB_NAME=mydb
      - POOL_MODE=transaction   # transaction pooling — hiệu quả nhất
      - MAX_CLIENT_CONN=1000    # số connection từ API đến PgBouncer
      - DEFAULT_POOL_SIZE=20    # số connection thật từ PgBouncer đến PostgreSQL
    restart: unless-stopped
```

**3 chế độ pooling của PgBouncer:**

| Mode | Mô tả | Dùng khi |
|------|-------|----------|
| `session` | 1 client = 1 server connection suốt session | ứng dụng dùng session-level features |
| `transaction` | 1 server connection chỉ bị giữ trong duration của transaction | **khuyến nghị** cho stateless API |
| `statement` | Pool lại sau mỗi statement | ít dùng, không hỗ trợ transaction nhiều statement |

> **Với stateless API như AuthDemo:** dùng `transaction` mode. Sau khi transaction commit/rollback, connection được trả lại pool ngay — không chờ HTTP request kết thúc.

**Sau khi có PgBouncer** — đổi connection string trong API:

```
# Trước: kết nối thẳng PostgreSQL
Host=192.168.1.xx;Port=5432;Database=mydb;...

# Sau: kết nối qua PgBouncer
Host=192.168.1.pgbouncer;Port=5432;Database=mydb;...
# (Hoặc IP của server chạy PgBouncer)
```

---

### So sánh SQL Server vs PostgreSQL trong bài toán scaling

| | SQL Server | PostgreSQL |
|---|---|---|
| **Connection model** | Thread-based — 1 connection = 1 thread | Process-based — 1 connection = 1 OS process |
| **Max connections thực tế** | Vài nghìn connection thoải mái | >200–300 connection bắt đầu tốn RAM/CPU đáng kể |
| **Connection pooler bắt buộc?** | Không (có thì tốt, nhưng không critical) | **Có** — PgBouncer gần như bắt buộc ở production |
| **Read replica** | AlwaysOn AG (cần Enterprise/Developer) | Streaming Replication built-in, miễn phí |
| **Sharding** | Cần giải pháp bên ngoài | Citus extension, partition native |
| **License** | Trả phí (Developer Edition miễn phí nhưng chỉ dev) | Miễn phí hoàn toàn |
| **Managed cloud** | Azure SQL, AWS RDS SQL Server | AWS Aurora PostgreSQL, GCP Cloud SQL, Supabase |

**Kết luận thực tế:**
- Đang dùng **SQL Server**: tune connection pool + thêm Redis cache là đủ cho hầu hết bài toán scale vừa. Read replica khi cần thiết nhưng cần license.
- Đang dùng **PostgreSQL**: bắt buộc thêm PgBouncer khi >100 concurrent connection. Read replica miễn phí và dễ cấu hình hơn SQL Server.

---

### Thứ tự ưu tiên khi DB quá tải

```
Bước 1 (30 phút):  Tune Max Pool Size trong connection string
                   → không tốn tiền, không thay đổi code

Bước 2 (vài giờ): Thêm Redis cache cho GET endpoints
                   → giảm 70–90% số lần gọi DB đối với read-heavy app

Bước 3 (1–2 ngày): Thêm PgBouncer (PostgreSQL) hoặc tune SQL Server
                    → giải quyết connection exhaustion

Bước 4 (vài ngày): Thêm Read Replica
                    → scale read throughput ngang

Bước 5 (phức tạp): Sharding / partitioning
                    → khi data quá lớn, 1 DB không chứa hết
```

> **Nguyên tắc:** làm từ đơn giản đến phức tạp. Phần lớn hệ thống vừa dừng ở Bước 2–3. Sharding chỉ cần khi data > vài trăm GB và vẫn không đủ sau khi đã có replica + cache.

---

## Cấu hình Nginx nâng cao (tùy chọn sau khi lab cơ bản OK)

### Least connections thay vì round-robin

Khi API có endpoint khác nhau về thời gian xử lý (login chậm hơn GET), `least_conn` tốt hơn round-robin vì nó gửi request mới đến server đang rảnh nhất:

```nginx
upstream api_backend {
    least_conn;   # thêm dòng này
    server 192.168.1.35:5000;
    server 192.168.1.36:5000;
    keepalive 32;
}
```

### Health check tự động (Nginx Plus hoặc nginx_upstream_check_module)

Nginx open-source không có active health check built-in. Workaround đơn giản — dùng `max_fails` và `fail_timeout`:

```nginx
upstream api_backend {
    server 192.168.1.35:5000 max_fails=3 fail_timeout=30s;
    server 192.168.1.36:5000 max_fails=3 fail_timeout=30s;
    keepalive 32;
}
```

Nếu VM2 chết (3 lần fail liên tiếp), Nginx tự loại VM2 ra trong 30 giây, sau đó thử lại.

### Giới hạn rate (bảo vệ backend khỏi bị flood)

```nginx
http {
    # Định nghĩa zone rate limiting: 10MB lưu trữ, 100 req/s per IP
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=100r/s;

    server {
        listen 80;

        location / {
            limit_req zone=api_limit burst=200 nodelay;
            proxy_pass http://api_backend;
        }
    }
}
```

---

## Debug thường gặp

### Nginx trả 502 Bad Gateway

Nghĩa là Nginx không kết nối được đến backend.

```bash
# Trên VM3 — kiểm tra Nginx log
docker logs nginx-lb --tail 50

# Thường thấy: "connect() failed (111: Connection refused)"
# Nghĩa là API backend không chạy hoặc port sai

# Verify từ VM3 → VM1
curl http://192.168.1.35:5000/health

# Verify từ VM3 → VM2
curl http://192.168.1.36:5000/health
```

Nếu không ping được từ VM3 → VM1/VM2, kiểm tra firewall:

```bash
# Trên VM1
sudo ufw status
sudo ufw allow from 192.168.1.37 to any port 5000
```

### VM2 API không kết nối được SQL Server ở VM1

```bash
# Trên VM2 — test kết nối TCP
nc -zv 192.168.1.35 1433

# Nếu fail: check firewall VM1
# Trên VM1
sudo ufw allow from 192.168.1.36 to any port 1433
```

Cũng kiểm tra SQL Server Docker trên VM1 đã map port 1433 ra host:

```bash
# Trên VM1
docker ps | grep sql
# Kỳ vọng thấy 0.0.0.0:1433->1433/tcp
```

### Container api crash ngay khi start: `BIO routines::system lib`

**Triệu chứng:** Log hiện `Interop+Crypto+OpenSslCryptographicException: error:10080002:BIO routines::system lib` và container liên tục restart.

**Nguyên nhân:** `openssl pkcs12 -export` sinh file `.pfx` với permissions `600` (owner-only). Container chạy bằng `appuser` (uid 1001), không phải user sinh cert → OpenSSL không mở được file.

**Fix:**

```bash
# Kiểm tra permissions
ls -la ~/projects/OpenIdDict_MrGold/docker/certs/openiddict.pfx
# Nếu thấy -rw------- (600) → đây là vấn đề

# Sửa permissions
chmod 644 ~/projects/OpenIdDict_MrGold/docker/certs/openiddict.pfx

# Xóa container cũ (docker compose up -d không tự restart container đang trong loop)
docker stop api && docker rm api
docker compose -f docker-compose.yaml up -d
```

> `gen-cert.sh` trong repo đã được cập nhật để tự động `chmod 644` sau khi tạo cert. Lỗi này chỉ xảy ra nếu dùng script cũ hoặc tạo cert thủ công.

### OpenIddict certificate lỗi khi API thứ hai khởi động

**Vấn đề đã được fix trong code và docker-compose.** Phần này mô tả triệu chứng và cách verify fix đã hoạt động.

**Triệu chứng nếu cert chưa đúng:** Login thành công (request đến VM1), nhưng lần gọi tiếp theo rơi vào VM2 trả 401 Unauthorized. Lỗi xảy ra ngẫu nhiên vì Nginx round-robin — lúc 200, lúc 401.

**Cơ chế:** Code đã được cập nhật tại `AuthDemo.Api/Extensions/OpenIddictExtensions.cs` — nếu `OpenIddict__CertPath` không được cấu hình hoặc file không tồn tại, API sẽ **throw exception ngay khi khởi động** (không âm thầm fallback về ephemeral key nữa). Container sẽ crash với message rõ ràng.

**Kiểm tra API đã load cert đúng chưa:**

```bash
# Trên VM1 — nếu startup log không có error → cert đã load OK
docker logs api --tail 50 | grep -i "openiddict\|cert\|error"

# Verify file cert đã được mount vào container
docker exec api ls -la /app/certs/
# Kỳ vọng: thấy openiddict.pfx
```

**Verify token từ VM1 được VM2 chấp nhận:**

```bash
# Lấy token từ VM1 trực tiếp (không qua Nginx)
TOKEN=$(curl -s -X POST http://192.168.1.35:5000/connect/token \
  -d "grant_type=password&client_id=angular-spa&username=loadtest_001@test.com&password=TestPass@123&scope=openid profile email roles" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

# Dùng token đó gọi thẳng vào VM2 (không qua Nginx)
curl -H "Authorization: Bearer $TOKEN" http://192.168.1.36:5000/api/user/info
# Kỳ vọng: 200 OK — nếu 401 → cert chưa đồng bộ
```

**Nếu API crash khi khởi động với lỗi cert:**

```bash
docker logs api --tail 20
# Sẽ thấy message dạng:
# InvalidOperationException: OpenIddict: Biến môi trường 'OpenIddict__CertPath' chưa được cấu hình...
# hoặc:
# InvalidOperationException: OpenIddict: Không tìm thấy certificate file tại '/app/certs/openiddict.pfx'...
```

Nguyên nhân thường gặp:
- Chưa chạy `gen-cert.sh` → file `.pfx` chưa tồn tại
- Quên copy file sang VM2 trước khi `docker compose up`
- Volume mount path sai trong docker-compose

Fix:
```bash
# VM1: sinh cert (nếu chưa có)
cd ~/projects/OpenIdDict_MrGold/docker/certs
chmod +x gen-cert.sh && ./gen-cert.sh

# VM2: copy cert sang (chạy từ VM1)
scp ~/projects/OpenIdDict_MrGold/docker/certs/openiddict.pfx \
    bank@192.168.1.36:~/projects/OpenIdDict_MrGold/docker/certs/

# Khởi động lại API
docker compose -f docker-compose.api-only.yml up -d --build
```

### Prometheus không scrape được VM2

```bash
# Verify từ VM1 — test kết nối đến VM2:5000
curl http://192.168.1.36:5000/metrics | head -5

# Nếu không được: check firewall VM2
# Trên VM2
sudo ufw allow from 192.168.1.35 to any port 5000
```

---

## Checklist cuối cùng

### Trước Phase 1 (baseline)
```
[ ] VM1: docker compose up → api, influxdb, prometheus, grafana đang healthy
[ ] curl 192.168.1.35:5000/health → OK
[ ] Prometheus target dotnet-api → UP
[ ] Grafana datasource InfluxDB và Prometheus → working
[ ] k6 smoke test PASS (2 VU, 0 error)
[ ] Ghi lại thời điểm bắt đầu test
```

### Trước Phase 5 (sau scale)
```
[ ] VM1: api vẫn chạy
[ ] VM2: api chạy → curl 192.168.1.36:5000/health OK
[ ] VM3: nginx chạy → curl 192.168.1.37/health OK → response từ API
[ ] VM3: round-robin verify → log VM1 và VM2 đều thấy request
[ ] Prometheus targets: dotnet-api-vm1 UP VÀ dotnet-api-vm2 UP
[ ] OpenIddict cert: token từ VM1 được VM2 accept (quan trọng!)
[ ] k6 smoke test qua Nginx PASS (2 VU, 0 error)
[ ] InfluxDB đã clear data cũ
[ ] Mở terminal docker stats trên cả VM1 và VM2 song song
```

### Sau khi hoàn thành cả 2 test
```
[ ] Điền đầy đủ 2 bảng số liệu (trước/sau)
[ ] Grafana screenshot lúc peak trước scale
[ ] Grafana screenshot lúc peak sau scale
[ ] Ghi nhận: SQL Server có phải bottleneck không?
[ ] Ghi nhận: Ngưỡng bão hòa tăng thêm bao nhiêu VU?
```

---

## Tóm tắt lộ trình

```
Ngày 1:
  Phase 1: Chạy stress test → tìm breaking point → ghi baseline
  Phase 2: Tạo VM2, clone code, chạy API thứ hai

Ngày 2:
  Phase 3: Tạo VM3, cấu hình Nginx load balancer
  Phase 4: Cập nhật Prometheus scrape 2 instance
  Phase 5: Chạy lại stress test → so sánh trước/sau

Quan sát chính:
  → Ngưỡng bão hòa tăng lên (thường ~1.8x, không phải 2x vì SQL Server là shared)
  → CPU mỗi instance giảm ~50%
  → Throughput tăng đáng kể ở mức VU cao
  → SQL Server vẫn là điểm giới hạn cuối cùng
```

> **Bài học thực tế:** Scale API dễ vì API là stateless. Bottleneck thật sự thường là database. Để scale tiếp sau bài này: thêm Redis cache cho GET endpoint — 1 request DB có thể phục vụ 1000 request API tiếp theo từ cache.
