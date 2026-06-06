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
- [Phase 6 — Docker Swarm: Deploy một lần, scale toàn cluster](#phase-6--docker-swarm-deploy-một-lần-scale-toàn-cluster)
- [Phase 7 — VM Registry riêng: Tách registry khỏi Manager](#phase-7--vm-registry-riêng-tách-registry-khỏi-manager)
- [Phase 8 — Nginx Proxy Manager (NPM) trong hệ thống công ty](#phase-8--nginx-proxy-manager-npm-trong-hệ-thống-công-ty)

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

##### Cơ chế hoạt động

AlwaysOn AG (Availability Group) là tính năng HA + read scale-out của SQL Server từ phiên bản 2012 trở lên. Cơ chế:

```
[Primary replica]
  ├── Nhận mọi WRITE (INSERT/UPDATE/DELETE/DDL)
  ├── Ghi transaction log
  └── Gửi log stream → Secondary (đồng bộ hoặc bất đồng bộ)

[Secondary replica]  ← đọc từ log stream của Primary
  ├── Apply log liên tục (redo thread)
  ├── Chỉ nhận READ nếu là "readable secondary"
  └── Có thể failover thành Primary nếu Primary chết
```

Có 2 chế độ đồng bộ:

| Chế độ | Độ trễ | An toàn dữ liệu | Dùng khi |
|--------|--------|-----------------|----------|
| **Synchronous** | Cao hơn (~1–5ms thêm vào mỗi write) | Zero data loss — Primary chờ Secondary xác nhận | Secondary cùng datacenter hoặc LAN nhanh |
| **Asynchronous** | Thấp | Có thể mất vài giây data nếu Primary crash đột ngột | Secondary ở datacenter xa, WAN |

Với bài lab này (2 VM cùng mạng LAN) → dùng Synchronous.

##### Yêu cầu phiên bản và Edition

| Edition | AlwaysOn AG | Readable Secondary | Số Secondary tối đa | Ghi chú |
|---------|-------------|-------------------|---------------------|---------|
| **Enterprise** | ✅ Full AG | ✅ Có thể đọc | 8 (sync) + không giới hạn async | Phiên bản duy nhất hỗ trợ read offloading thật sự |
| **Standard** | ✅ Basic AG | ❌ Không readable | 1 Secondary, chỉ failover | Không dùng cho read scale-out được |
| **Developer** | ✅ Giống Enterprise | ✅ Có | 8 | **Miễn phí** nhưng cấm dùng production |
| **Web** | ❌ | ❌ | — | Chỉ qua hosting provider |
| **Express** | ❌ | ❌ | — | DB < 10GB, dev nhỏ |

> **Kết luận thực tế:** Để làm read replica đúng nghĩa (secondary nhận READ query) → **bắt buộc Enterprise Edition** (hoặc Developer cho lab/test).

##### Phiên bản SQL Server được hỗ trợ

AlwaysOn AG có từ SQL Server 2012. Các phiên bản còn được Microsoft support tính đến 2026:

| Phiên bản | Mainstream Support | Extended Support | Ghi chú |
|-----------|-------------------|-----------------|---------|
| SQL Server 2019 | Hết 2025 | Đến 2030 | Vẫn trong extended support |
| **SQL Server 2022** | Đến 2028 | Đến 2033 | Phiên bản mới nhất, khuyến nghị |
| SQL Server 2017 | Đã hết | Đến 2027 | Sắp hết support |

##### Giá license SQL Server 2022 (tính đến 2025)

Microsoft bán license theo 2 mô hình:

**Mô hình 1 — Per Core (phổ biến nhất, recommended):**

Bán theo gói "2-core pack", mỗi server phải mua tối thiểu 4 core (2 gói):

| Edition | Giá / 2-core pack | 1 server 4 core | 1 server 8 core |
|---------|-------------------|-----------------|-----------------|
| **Enterprise** | ~$15,123 USD | ~$30,246 | ~$60,492 |
| **Standard** | ~$1,048 USD | ~$2,096 | ~$4,192 |
| Developer | $0 | $0 | $0 (dev/test only) |

> Giá trên là retail list price của Microsoft. VAR (reseller) thường discount 10–30%. Giá VND dao động theo tỷ giá và đại lý phân phối tại Việt Nam.

**Mô hình 2 — Server + CAL (chỉ Standard, dành cho nội bộ công ty):**
- $3,945 / server + $239 / user CAL hoặc $239 / device CAL
- Không áp dụng nếu user bên ngoài kết nối (web app public)

**License dùng được bao lâu, bao nhiêu máy:**
- License là **perpetual** (vĩnh viễn, mua một lần dùng mãi)
- Mỗi license gắn với **1 server cụ thể** — không dùng chung được
- Muốn chạy trên 2 VM (Primary + Secondary) → phải **mua license cho cả 2 VM**
- Software Assurance (SA) — gói bảo trì tùy chọn: ~25%/năm của giá license → cho quyền upgrade lên version mới

**Ví dụ chi phí thực tế cho lab 2 VM (4 core/VM):**

```
Enterprise (đúng yêu cầu readable secondary):
  VM1 (Primary)  : 2 gói × $15,123 = $30,246
  VM2 (Secondary): 2 gói × $15,123 = $30,246
  Tổng:                              $60,492 USD ≈ 1.5 tỷ VND

Standard (chỉ failover, KHÔNG đọc được):
  VM1 + VM2     : 4 gói × $1,048  = $4,192 USD ≈ 105 triệu VND
  (nhưng secondary không nhận READ — không giải quyết bài toán scale)
```

> **Đây là lý do PostgreSQL phổ biến hơn cho startup và lab:** read replica miễn phí hoàn toàn với Streaming Replication built-in.

##### Primary và Secondary phải ở 2 VM khác nhau — tại sao?

Đây là câu hỏi quan trọng. **Primary và Secondary PHẢI cài trên 2 máy/VM riêng biệt.** Nếu chạy trên cùng 1 VM thì hoàn toàn vô nghĩa về mặt giảm tải:

```
❌ SAI — Primary + Secondary cùng 1 VM:

  [VM1 — 8 core, 16GB RAM]
    ├── SQL Server Primary instance (:1433)   ← xử lý WRITE
    └── SQL Server Secondary instance (:1434) ← xử lý READ

  Kết quả: READ query vẫn tiêu thụ CPU/RAM/Disk I/O của cùng 1 máy vật lý
  → Tổng tải trên VM1 không giảm chút nào
  → Disk I/O: Primary write WAL log + Secondary apply log = I/O còn cao hơn
  → Không đạt mục đích scale-out
```

```
✅ ĐÚNG — Primary và Secondary ở 2 VM riêng:

  [VM1 — Primary]              [VM2 — Secondary]
  SQL Server :1433             SQL Server :1433
  Nhận WRITE từ API       ←─── Nhận log stream từ VM1
                                Nhận READ từ API

  → WRITE tiêu thụ CPU/RAM/Disk của VM1
  → READ tiêu thụ CPU/RAM/Disk của VM2
  → VM1 "nhẹ" hơn vì không phải chạy SELECT nặng
  → VM2 là "đọc-only server" — không bị ảnh hưởng bởi WRITE lock
```

**Kiến trúc đầy đủ sau khi có read replica:**

```
[k6 / Client]
      │
      ▼
[Nginx :80]
      │
  ┌───┴───┐
  ▼       ▼
[API VM1] [API VM2]   ← stateless, round-robin
  │           │
  ├── WRITE ──┤──────────────────────────────────┐
  │           │                                  ▼
  └── READ ───┘──────────────────────┐   [SQL Primary — VM_DB1]
                                     │         │ WAL stream
                                     ▼         ▼
                             [SQL Secondary — VM_DB2]  ← READ only
```

**Cơ chế giảm tải cụ thể:**

Giả sử 1000 request/giây, 80% là READ (SELECT), 20% là WRITE:

```
Không có replica:
  SQL Server VM_DB1: xử lý 800 READ + 200 WRITE = 1000 query/s
  → CPU cao, lock contention giữa read và write

Có replica:
  SQL Primary VM_DB1: xử lý 200 WRITE/s
  SQL Secondary VM_DB2: xử lý 800 READ/s
  → Mỗi server chịu ~1/5 tải ban đầu cho primary, 4/5 cho secondary
  → Không còn lock contention: write không chặn read
```

Ngoài giảm tải, secondary còn loại bỏ "read/write lock contention" — vấn đề mà khi bảng đang bị UPDATE thì SELECT phải chờ, gây tăng latency đột biến ở tải cao.

##### Setup AlwaysOn AG tối giản (lab với 2 VM)

> **Yêu cầu:** Cả 2 VM phải join Windows domain hoặc dùng Workgroup AG (SQL Server 2019+). Với lab Linux/Docker thì AG yêu cầu cấu hình phức tạp hơn — đây là lý do PostgreSQL thực tế hơn cho lab.

**Bước tổng quan (Windows Server + SQL Server Enterprise):**

```
1. Cài Windows Failover Cluster (WSFC) trên cả 2 VM
2. Cài SQL Server Enterprise trên cả 2 VM
3. Enable AlwaysOn AG feature trong SQL Server Configuration Manager
4. Tạo AG trong SSMS:
   - Chọn databases cần replicate
   - Thêm secondary replica (VM2)
   - Chọn Synchronous commit + Readable secondary
5. Tạo AG Listener (virtual IP/DNS name)
6. Cập nhật connection string trong API
```

Trong connection string .NET để tự động route:

```
# Kết nối Primary cho write
Server=192.168.1.xx,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadWrite

# Kết nối Secondary cho read
Server=192.168.1.yy,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadOnly
```

Hoặc dùng AG Listener (DNS name tự route — khuyến nghị):

```
Server=ag-listener,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadOnly
# Listener tự biết chuyển ReadOnly → secondary, ReadWrite → primary
```

##### Lựa chọn thực tế cho lab/startup Việt Nam

| Tình huống | Khuyến nghị |
|-----------|-------------|
| Lab/học tập | SQL Server Developer Edition (miễn phí) hoặc PostgreSQL |
| Startup, budget thấp | PostgreSQL + Streaming Replication (miễn phí) |
| Doanh nghiệp, đang dùng SQL Server | Cân nhắc Azure SQL (subscription) thay vì mua Enterprise |
| Cloud Azure | Azure SQL Geo-Replication / Auto-failover groups — không cần mua license riêng |

> **Trên cloud (Azure SQL Database):** read replica được tính theo DTU/vCore — không cần mua Enterprise license riêng. Đây thường là lựa chọn kinh tế hơn cho production thay vì tự quản lý AlwaysOn AG.

##### Hands-on: Cấu hình AlwaysOn AG trên 2 VM (Docker + Linux)

```
Lab IPs (riêng biệt với App VMs):
  VM_DB1: 192.168.1.40  — SQL Server Primary  (nhận WRITE + READ)
  VM_DB2: 192.168.1.41  — SQL Server Secondary (nhận READ only)
```

**Bước 1 — docker-compose trên cả 2 VM (chỉ khác hostname)**

```yaml
# ~/sqlserver-ag/docker-compose.yml
# VM_DB1: hostname: vm-db1
# VM_DB2: hostname: vm-db2  ← chỉ đổi dòng này
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    hostname: vm-db1
    ports:
      - "1433:1433"
      - "5022:5022"    # HADR mirroring endpoint — bắt buộc
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_ENABLE_HADR=1    # bật AlwaysOn AG feature
    volumes:
      - sqlserver-data:/var/opt/mssql
    restart: unless-stopped
volumes:
  sqlserver-data:
```

```bash
# Chạy trên cả 2 VM
cd ~/sqlserver-ag && docker compose up -d
sleep 30   # chờ SQL Server khởi động

# Tạo thư mục cert/backup trong container (cả 2 VM)
docker exec sqlserver mkdir -p /var/opt/mssql/certs /var/opt/mssql/backup
```

**Bước 2 — T-SQL trên PRIMARY (VM_DB1)**

Kết nối bằng sqlcmd hoặc Azure Data Studio vào `192.168.1.40,1433`:

```sql
USE master;

-- Tạo master key
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MasterKey@AG2025!';

-- Tạo certificate để xác thực endpoint
CREATE CERTIFICATE AG_Cert_Primary
  WITH SUBJECT = 'HADR Endpoint Auth', EXPIRY_DATE = '2035-12-31';

-- Export cert (sẽ copy sang secondary)
BACKUP CERTIFICATE AG_Cert_Primary
  TO FILE = '/var/opt/mssql/certs/ag_cert_primary.cer';

-- Tạo HADR endpoint lắng nghe port 5022
CREATE ENDPOINT AG_Endpoint
  STATE = STARTED
  AS TCP (LISTENER_PORT = 5022)
  FOR DATABASE_MIRRORING (
    AUTHENTICATION = CERTIFICATE AG_Cert_Primary,
    ENCRYPTION = REQUIRED ALGORITHM AES,
    ROLE = ALL
  );

-- Database phải ở FULL recovery mode mới vào được AG
ALTER DATABASE AuthDemoDB SET RECOVERY FULL;
BACKUP DATABASE AuthDemoDB
  TO DISK = '/var/opt/mssql/backup/AuthDemoDB.bak' WITH FORMAT, INIT, COMPRESSION;
BACKUP LOG AuthDemoDB
  TO DISK = '/var/opt/mssql/backup/AuthDemoDB_log.bak' WITH FORMAT, INIT;

-- Tạo Availability Group (CLUSTER_TYPE = NONE = clusterless, không cần WSFC/Pacemaker)
-- SEEDING_MODE = AUTOMATIC: SQL Server tự copy database sang secondary, không restore thủ công
CREATE AVAILABILITY GROUP [AuthDemoAG]
WITH (
  CLUSTER_TYPE = NONE,
  DB_FAILOVER = OFF,
  REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT = 0
)
FOR DATABASE [AuthDemoDB]
REPLICA ON N'vm-db1' WITH (
  ENDPOINT_URL    = N'TCP://192.168.1.40:5022',
  FAILOVER_MODE   = MANUAL,
  AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
  SEEDING_MODE    = AUTOMATIC,
  SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
);
```

**Bước 3 — T-SQL trên SECONDARY (VM_DB2)**

Kết nối vào `192.168.1.41,1433`:

```sql
USE master;

IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MasterKey@AG2025!';

-- Tạo certificate riêng của secondary
CREATE CERTIFICATE AG_Cert_Secondary
  WITH SUBJECT = 'HADR Endpoint Auth Secondary', EXPIRY_DATE = '2035-12-31';

-- Export cert của secondary (để copy sang primary)
BACKUP CERTIFICATE AG_Cert_Secondary
  TO FILE = '/var/opt/mssql/certs/ag_cert_secondary.cer';

-- Tạo HADR endpoint
CREATE ENDPOINT AG_Endpoint
  STATE = STARTED
  AS TCP (LISTENER_PORT = 5022)
  FOR DATABASE_MIRRORING (
    AUTHENTICATION = CERTIFICATE AG_Cert_Secondary,
    ENCRYPTION = REQUIRED ALGORITHM AES,
    ROLE = ALL
  );
```

**Bước 4 — Trao đổi certificate giữa 2 VM**

```bash
# Lấy cert từ container ra host
# Trên VM_DB1:
docker cp sqlserver:/var/opt/mssql/certs/ag_cert_primary.cer ~/ag_cert_primary.cer
# Trên VM_DB2:
docker cp sqlserver:/var/opt/mssql/certs/ag_cert_secondary.cer ~/ag_cert_secondary.cer

# Cross-copy qua scp
# Từ VM_DB1 → gửi cert primary sang VM_DB2:
scp ~/ag_cert_primary.cer bank@192.168.1.41:~/
# Từ VM_DB2 → gửi cert secondary sang VM_DB1:
scp ~/ag_cert_secondary.cer bank@192.168.1.40:~/

# Copy cert vào trong container
# Trên VM_DB1:
docker cp ~/ag_cert_secondary.cer sqlserver:/var/opt/mssql/certs/
# Trên VM_DB2:
docker cp ~/ag_cert_primary.cer sqlserver:/var/opt/mssql/certs/
```

**Bước 5 — Import cert và grant quyền (cả 2 VM)**

Trên PRIMARY (VM_DB1):
```sql
-- Import cert của secondary, tạo login để secondary xác thực vào endpoint của primary
CREATE CERTIFICATE AG_Cert_Secondary_Pub
  FROM FILE = '/var/opt/mssql/certs/ag_cert_secondary.cer';
CREATE LOGIN AG_Login_Secondary WITH PASSWORD = 'AGLogin@2025!';
CREATE USER AG_User_Secondary FOR LOGIN AG_Login_Secondary;
GRANT CONNECT ON ENDPOINT::AG_Endpoint TO AG_Login_Secondary;

-- Thêm secondary vào AG definition
ALTER AVAILABILITY GROUP [AuthDemoAG]
  ADD REPLICA ON N'vm-db2' WITH (
    ENDPOINT_URL    = N'TCP://192.168.1.41:5022',
    FAILOVER_MODE   = MANUAL,
    AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
    SEEDING_MODE    = AUTOMATIC,
    SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
  );
```

Trên SECONDARY (VM_DB2):
```sql
-- Import cert của primary, tạo login để primary xác thực vào endpoint của secondary
CREATE CERTIFICATE AG_Cert_Primary_Pub
  FROM FILE = '/var/opt/mssql/certs/ag_cert_primary.cer';
CREATE LOGIN AG_Login_Primary WITH PASSWORD = 'AGLogin@2025!';
CREATE USER AG_User_Primary FOR LOGIN AG_Login_Primary;
GRANT CONNECT ON ENDPOINT::AG_Endpoint TO AG_Login_Primary;

-- Join AG và cho phép automatic seeding
ALTER AVAILABILITY GROUP [AuthDemoAG] JOIN WITH (CLUSTER_TYPE = NONE);
ALTER AVAILABILITY GROUP [AuthDemoAG] GRANT CREATE ANY DATABASE;
```

**Bước 6 — Verify**

```sql
-- Chạy trên Primary — kiểm tra trạng thái replica
SELECT
  ar.replica_server_name,
  ars.role_desc,
  ars.synchronization_health_desc
FROM sys.dm_hadr_availability_replica_states ars
JOIN sys.availability_replicas ar ON ars.replica_id = ar.replica_id;
-- Kỳ vọng: vm-db1 PRIMARY HEALTHY, vm-db2 SECONDARY HEALTHY

-- Kiểm tra replication lag (nên gần 0 trên LAN)
SELECT ar.replica_server_name,
       drs.log_send_queue_size AS unsent_kb,
       drs.redo_queue_size     AS unapplied_kb
FROM sys.dm_hadr_database_replica_states drs
JOIN sys.availability_replicas ar ON drs.replica_id = ar.replica_id;
```

```bash
# Test write vào primary, đọc từ secondary
sqlcmd -S 192.168.1.40,1433 -U sa -P YourStrong@Passw0rd \
  -Q "INSERT INTO AuthDemoDB.dbo.ReplicationTest VALUES (NEWID(), 'hello from primary')"

sqlcmd -S 192.168.1.41,1433 -U sa -P YourStrong@Passw0rd \
  -Q "SELECT * FROM AuthDemoDB.dbo.ReplicationTest"
# Kỳ vọng: thấy row vừa insert

# Thử write vào secondary → phải fail
sqlcmd -S 192.168.1.41,1433 -U sa -P YourStrong@Passw0rd \
  -Q "INSERT INTO AuthDemoDB.dbo.ReplicationTest VALUES (NEWID(), 'should fail')"
# Kỳ vọng: ERROR - The target database is in a read-only state
```

#### PostgreSQL — Streaming Replication

PostgreSQL native replication đơn giản hơn SQL Server nhiều — không cần license, không cần cert exchange.

```
Lab IPs:
  VM_DB1: 192.168.1.40  — PostgreSQL Primary
  VM_DB2: 192.168.1.41  — PostgreSQL Replica (Read)
```

##### Hands-on: Cấu hình Streaming Replication trên 2 VM (Docker)

**Bước 1 — Config files trên VM_DB1 (Primary)**

```bash
# Trên VM_DB1
mkdir -p ~/postgres-primary/config

cat > ~/postgres-primary/config/postgresql.conf << 'EOF'
listen_addresses = '*'
wal_level = replica       # bắt buộc: ghi WAL đủ để replica đọc
max_wal_senders = 10      # tối đa 10 replica kết nối đồng thời (mặc định đủ cho lab)
wal_keep_size = 256MB     # giữ 256MB WAL trên disk phòng replica bị lag
synchronous_commit = on
EOF

cat > ~/postgres-primary/config/pg_hba.conf << 'EOF'
local   all             all                                 trust
host    all             all             127.0.0.1/32        scram-sha-256
host    all             all             192.168.1.0/24      scram-sha-256
# Cho phép replication user kết nối từ bất kỳ VM nào trong subnet
host    replication     replicator      192.168.1.0/24      scram-sha-256
EOF
```

```yaml
# ~/postgres-primary/docker-compose.yml
services:
  postgres:
    image: postgres:16
    container_name: postgres-primary
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_USER=pgadmin
      - POSTGRES_PASSWORD=Admin@Postgres2025
      - POSTGRES_DB=AuthDemoDB
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./config/postgresql.conf:/etc/postgresql/postgresql.conf
      - ./config/pg_hba.conf:/etc/postgresql/pg_hba.conf
    command: >
      postgres
        -c config_file=/etc/postgresql/postgresql.conf
        -c hba_file=/etc/postgresql/pg_hba.conf
    restart: unless-stopped
volumes:
  pgdata:
```

```bash
# Khởi động Primary
cd ~/postgres-primary && docker compose up -d

# Tạo user replication (sau khi container healthy ~10s)
docker exec postgres-primary psql -U pgadmin -d postgres \
  -c "CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'Replicator@2025';"

# Verify
docker exec postgres-primary psql -U pgadmin \
  -c "SELECT usename, replication FROM pg_user WHERE usename = 'replicator';"
# Kỳ vọng: replicator | t
```

**Bước 2 — Khởi tạo Replica bằng pg_basebackup (VM_DB2)**

`pg_basebackup` sao chép toàn bộ data từ Primary. Flag `-R` tự tạo file `standby.signal` và `postgresql.auto.conf` — PostgreSQL dựa vào đó để biết khởi động ở chế độ standby.

```bash
# Trên VM_DB2 — tạo thư mục data (để TRỐNG, không docker compose up trước)
mkdir -p ~/postgres-replica/data

# Dùng image postgres tạm thời để chạy pg_basebackup
docker run --rm \
  -e PGPASSWORD='Replicator@2025' \
  -v ~/postgres-replica/data:/var/lib/postgresql/data \
  postgres:16 \
  pg_basebackup \
    -h 192.168.1.40 \
    -p 5432 \
    -U replicator \
    -D /var/lib/postgresql/data \
    -Xs -P -R
# -Xs : stream WAL trong lúc backup (không mất transaction nào)
# -P  : hiện progress bar
# -R  : tạo standby.signal + postgresql.auto.conf tự động

# Fix permissions: postgres container chạy bằng uid 999 (debian-based image)
sudo chown -R 999:999 ~/postgres-replica/data
```

**Bước 3 — Khởi động Replica (VM_DB2)**

```yaml
# ~/postgres-replica/docker-compose.yml
# Không cần POSTGRES_USER/DB vì data đã copy từ primary — container chỉ cần chạy postgres
services:
  postgres:
    image: postgres:16
    container_name: postgres-replica
    ports:
      - "5432:5432"
    environment:
      - PGDATA=/var/lib/postgresql/data
      - POSTGRES_PASSWORD=Admin@Postgres2025   # chỉ cho healthcheck nếu cần
    volumes:
      - ~/postgres-replica/data:/var/lib/postgresql/data
    restart: unless-stopped
    # Container tự detect file standby.signal → tự chạy ở standby/read-only mode
```

```bash
cd ~/postgres-replica && docker compose up -d

# Verify replica đang streaming
docker exec postgres-replica psql -U pgadmin -d AuthDemoDB \
  -c "SELECT status, sender_host, received_lsn FROM pg_stat_wal_receiver;"
# Kỳ vọng: status = streaming, sender_host = 192.168.1.40
```

**Bước 4 — Verify từ Primary**

```bash
# Xem replica đang kết nối
docker exec postgres-primary psql -U pgadmin \
  -c "SELECT client_addr, state, write_lag, replay_lag FROM pg_stat_replication;"
# Kỳ vọng: client_addr=192.168.1.41, state=streaming, lag=gần 0

# Test: write primary → đọc được trên replica
docker exec postgres-primary psql -U pgadmin -d AuthDemoDB \
  -c "CREATE TABLE IF NOT EXISTS repl_test (id SERIAL, msg TEXT);
      INSERT INTO repl_test(msg) VALUES ('hello replica');"

docker exec postgres-replica psql -U pgadmin -d AuthDemoDB \
  -c "SELECT * FROM repl_test;"
# Kỳ vọng: thấy row 'hello replica'

# Thử WRITE vào replica → phải fail
docker exec postgres-replica psql -U pgadmin -d AuthDemoDB \
  -c "INSERT INTO repl_test(msg) VALUES ('should fail');"
# Kỳ vọng: ERROR: cannot execute INSERT in a read-only transaction
```

---

#### Scale out: 1 Primary + N Replica (3 VM trở lên)

##### PostgreSQL — Fan-out vs Cascade

**Cách 1 — Fan-out (tất cả replica kéo từ Primary):**

```
Primary (VM_DB1: .40)
  ├── WAL stream ──→ Replica1 (VM_DB2: .41)
  └── WAL stream ──→ Replica2 (VM_DB3: .42)
```

Để thêm Replica2: lặp lại **nguyên xi** Bước 2–4 trên VM_DB3 (192.168.1.42). Không cần thay đổi gì trên Primary hay Replica1. `pg_hba.conf` đã dùng subnet `/24` nên tự động cho phép.

**Cách 2 — Cascade (Replica kéo từ Replica, giảm tải Primary):**

```
Primary (VM_DB1)
  └── WAL stream ──→ Replica1 (VM_DB2)
                        └── WAL stream ──→ Replica2 (VM_DB3)
```

Phù hợp khi có nhiều replica (5+) và không muốn Primary gửi WAL đến tất cả.

Cấu hình Replica1 làm "relay" — thêm vào `postgresql.conf` của Replica1:
```
wal_level = replica     # Replica1 cũng cần ghi WAL để Replica2 đọc
max_wal_senders = 5     # Cho phép downstream replica kết nối
```

Replica2 chạy `pg_basebackup` trỏ vào Replica1 thay vì Primary:
```bash
docker run --rm \
  -e PGPASSWORD='Replicator@2025' \
  -v ~/postgres-replica2/data:/var/lib/postgresql/data \
  postgres:16 \
  pg_basebackup -h 192.168.1.41 ...   # ← trỏ vào Replica1
```

| | Fan-out | Cascade |
|--|---------|---------|
| Độ phức tạp | Đơn giản, lặp lại setup | Phức tạp hơn |
| Tải Primary | Tăng theo số replica | Cố định (chỉ 1 WAL sender) |
| Lag Replica2 | Nhỏ (stream thẳng từ Primary) | Lớn hơn (qua Replica1) |
| Khi Replica1 chết | Replica2 không ảnh hưởng | Replica2 mất replication |
| Dùng khi | Lab, ≤5 replica | Nhiều replica, Primary bị tải |

**Giới hạn thực tế:**
- `max_wal_senders` trên Primary = số replica kết nối đồng thời (mặc định 10, tăng thoải mái)
- Thực tế: >20 replica trực tiếp thì Primary tốn I/O gửi WAL → dùng cascade

##### SQL Server AG — Thêm replica thứ 3 trở đi

Khi đã có AG (2 VM), thêm replica mới chỉ cần:

1. Cài SQL Server trên VM_DB3 với `MSSQL_ENABLE_HADR=1` và port 5022 (giống VM_DB2)
2. Tạo cert + endpoint trên VM_DB3 (y chang bước 3 ở trên, đổi tên `vm-db3`)
3. Trao đổi cert giữa Primary và VM_DB3
4. Chạy T-SQL thêm replica:

```sql
-- Trên Primary — sau khi đã import cert VM_DB3 và tạo login/grant
ALTER AVAILABILITY GROUP [AuthDemoAG]
  ADD REPLICA ON N'vm-db3' WITH (
    ENDPOINT_URL      = N'TCP://192.168.1.42:5022',
    FAILOVER_MODE     = MANUAL,
    AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT,  -- replica thứ 2+ thường dùng async
    SEEDING_MODE      = AUTOMATIC,
    SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
  );
```

```sql
-- Trên VM_DB3
ALTER AVAILABILITY GROUP [AuthDemoAG] JOIN WITH (CLUSTER_TYPE = NONE);
ALTER AVAILABILITY GROUP [AuthDemoAG] GRANT CREATE ANY DATABASE;
```

> Với Enterprise: tối đa **8 synchronous** + không giới hạn **asynchronous** secondary. Replica thứ 2 thường đặt `ASYNCHRONOUS_COMMIT` để không làm chậm write trên Primary.

---

#### Xử lý multiple read replica trong .NET code

##### Cấp 1 — Không cần đổi code: built-in load balancing

Đây là cách tốt nhất — infrastructure tự routing, thêm replica không sửa code.

**SQL Server — AG Listener:**

AG Listener là virtual IP/DNS tự động phân phối `ApplicationIntent=ReadOnly` đến các readable secondary. Khi thêm replica mới, không đổi connection string.

```sql
-- Tạo AG Listener (T-SQL trên Primary)
ALTER AVAILABILITY GROUP [AuthDemoAG]
  ADD LISTENER N'ag-listener' (
    WITH IP ((N'192.168.1.45', N'255.255.255.0')),
    PORT = 1433
  );
```

```json
// appsettings.json
"ConnectionStrings": {
  "Write": "Server=ag-listener,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadWrite",
  "Read":  "Server=ag-listener,1433;Database=AuthDemoDB;...;ApplicationIntent=ReadOnly"
}
```

> AG Listener với clusterless AG cần virtual IP được route đúng trên switch/VMware. Trong lab không có WSFC, dùng thẳng IP của secondary hoặc khai báo tất cả IP trong connection string.

**PostgreSQL — Npgsql multi-host (đơn giản nhất, không cần AG Listener):**

Npgsql hỗ trợ liệt kê nhiều host, tự random-pick standby mỗi lần tạo connection mới.

```json
"ConnectionStrings": {
  "Write": "Host=192.168.1.40;Port=5432;Database=AuthDemoDB;Username=app;Password=...;",
  "Read":  "Host=192.168.1.41,192.168.1.42;Port=5432;Database=AuthDemoDB;Username=app;Password=...;Target Session Attributes=prefer-standby;Load Balance Hosts=true"
}
```

- `Target Session Attributes=prefer-standby`: ưu tiên kết nối standby, nếu tất cả standby down thì fallback về primary
- `Load Balance Hosts=true`: random chọn host trong danh sách mỗi lần connect
- Thêm Replica3: chỉ cần thêm `192.168.1.43` vào string — không đổi code

---

##### Cấp 2 — 2 DbContext (Write + Read)

Pattern đơn giản nhất khi chỉ cần tách write/read, không cần round-robin tự viết:

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Write")));

builder.Services.AddDbContext<ReadDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Read"))
       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
```

```csharp
// ReadDbContext.cs — kế thừa AppDbContext, không cần override gì thêm
public class ReadDbContext : AppDbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options) { }
}
```

```csharp
public class UserService
{
    private readonly AppDbContext _write;
    private readonly ReadDbContext _read;

    public async Task<UserInfo?> GetUserInfoAsync(string userId)
        => await _read.Users
                      .AsNoTracking()
                      .Where(u => u.Id == userId)
                      .Select(u => new UserInfo(u.Id, u.Email))
                      .FirstOrDefaultAsync();

    public async Task UpdateEmailAsync(string userId, string email)
    {
        var user = await _write.Users.FindAsync(userId);
        user!.Email = email;
        await _write.SaveChangesAsync();
    }
}
```

---

##### Cấp 3 — Round-robin N replica (khi không có AG Listener, nhiều replica)

Khi không có AG Listener mà có nhiều replica cần phân tải đều:

```csharp
// ReplicaRouter.cs — singleton, round-robin thread-safe
public sealed class ReplicaRouter
{
    private readonly IReadOnlyList<string> _replicas;
    private int _counter = -1;

    public ReplicaRouter(IConfiguration config)
    {
        _replicas = config.GetSection("DB:ReadReplicas").Get<string[]>()
                    ?? throw new InvalidOperationException("DB:ReadReplicas not configured");
    }

    public string Next()
    {
        // Unsigned modulo tránh âm khi counter overflow
        var i = (uint)Interlocked.Increment(ref _counter) % (uint)_replicas.Count;
        return _replicas[(int)i];
    }
}
```

```csharp
// ReadDbContextFactory.cs — tạo DbContext trỏ đến replica được chọn
public sealed class ReadDbContextFactory
{
    private readonly ReplicaRouter _router;

    public ReadDbContextFactory(ReplicaRouter router) => _router = router;

    public AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_router.Next())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new AppDbContext(options);
    }
}
```

```csharp
// Program.cs
builder.Services.AddSingleton<ReplicaRouter>();        // singleton: counter dùng chung toàn app
builder.Services.AddTransient<ReadDbContextFactory>();  // transient: tạo mới mỗi request
```

```json
// appsettings.json
"DB": {
  "Write": "Server=192.168.1.40,1433;Database=AuthDemoDB;User Id=app;Password=...;",
  "ReadReplicas": [
    "Server=192.168.1.41,1433;Database=AuthDemoDB;User Id=app;Password=...;ApplicationIntent=ReadOnly",
    "Server=192.168.1.42,1433;Database=AuthDemoDB;User Id=app;Password=...;ApplicationIntent=ReadOnly"
  ]
}
```

Dùng trong service:
```csharp
public class UserService
{
    private readonly ReadDbContextFactory _readFactory;
    private readonly AppDbContext _write;

    public async Task<List<UserInfo>> GetUsersAsync()
    {
        // Mỗi lần gọi lấy 1 replica theo round-robin
        await using var ctx = _readFactory.Create();
        return await ctx.Users
                        .AsNoTracking()
                        .Select(u => new UserInfo(u.Id, u.Email))
                        .ToListAsync();
    }
}
```

---

##### Tóm tắt: chọn pattern nào?

| Scenario | Giải pháp | Ghi chú |
|----------|-----------|---------|
| SQL Server + AG Listener | `ApplicationIntent=ReadOnly` trong conn string | Không đổi code khi thêm replica |
| PostgreSQL | Npgsql multi-host + `Load Balance Hosts=true` | Không đổi code, chỉ thêm IP vào config |
| Không có Listener, 1 replica | 2 DbContext (Write + Read) | Đơn giản nhất |
| Không có Listener, N replica | `ReplicaRouter` + `ReadDbContextFactory` | Round-robin tự viết |
| Production phức tạp | ProxySQL (MySQL/MSSQL) / PgPool-II (PG) | Infrastructure routing, code chỉ biết 1 host |

> **Nguyên tắc:** code chỉ cần biết 2 connection string (Write vs Read). Khi thêm replica, chỉ đổi config — không sửa code.

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

---

## Phase 6 — Docker Swarm: Deploy một lần, scale toàn cluster

### Tại sao cần Swarm?

Với cách manual ở Phase 2–3, mỗi lần có thay đổi bạn phải:

```
Thay đổi code:
  VM1: git pull → docker compose build → docker compose up -d
  VM2: git pull → docker compose build → docker compose up -d  ← phải làm lại

Thay đổi config (env var, connection string):
  VM1: sửa docker-compose.yml → docker compose up -d
  VM2: sửa docker-compose.api-only.yml → docker compose up -d  ← phải làm lại

Thêm VM3 API instance:
  VM3: cài Docker → clone code → copy cert → cấu hình compose → chạy
  VM1/Nginx: cập nhật nginx.conf thêm upstream → reload
```

**Với Docker Swarm**, toàn bộ những bước trên thành một lệnh duy nhất chạy trên VM1 (manager):

```bash
# Deploy hoặc update toàn bộ cluster
docker stack deploy -c docker-stack.yml authdemo

# Scale thêm instance (không cần vào VM nào)
docker service scale authdemo_api=3

# Update image mới (rolling update tự động)
docker service update --image 192.168.1.35:5050/authdemo-api:v2 authdemo_api
```

### Kiến trúc Swarm (so với cách manual)

| | Manual | Docker Swarm |
|---|---|---|
| **Deploy** | SSH vào từng VM, chạy compose | 1 lệnh từ manager |
| **Update code** | Làm trên từng VM | Build 1 lần → push registry → `docker stack deploy` |
| **Update config** | Sửa file trên từng VM | Sửa stack file → `docker stack deploy` |
| **Scale thêm** | Tạo VM mới, cài tay, cập nhật Nginx | `docker service scale` + `docker swarm join` |
| **Cert management** | Copy thủ công sang từng VM | Docker Secret — Swarm tự phân phối |
| **Rolling update** | Không có, manual restart | Tự động (start-first, zero downtime) |
| **Health check** | Tự xử lý | Swarm tự restart container fail |

```
                        [k6 — máy bạn]
                               |
                        POST/GET :80
                               |
                               v
                   ┌─────────────────────┐
                   │   VM1 — Manager     │
                   │   192.168.1.35      │
                   │                     │
                   │ nginx-lb (:80)      │  ← Swarm service, 1 replica
                   │ sqlserver (:1433)   │  ← stack hoặc compose riêng
                   │ prometheus (:9090)  │  ← monitoring stack riêng
                   │ grafana (:3000)     │
                   │ registry (:5050)    │  ← local image registry
                   └────────┬────────────┘
                            │ Overlay network (authdemo_app-net)
                  ┌─────────┴──────────┐
                  │                    │
        ┌─────────▼──────┐   ┌─────────▼──────┐
        │  VM2 — Worker  │   │  VM3 — Worker  │
        │  192.168.1.36  │   │  192.168.1.37  │
        │                │   │                │
        │  api (replica1)│   │  api (replica2)│
        └────────────────┘   └────────────────┘
```

**Điểm khác biệt quan trọng với manual:**
- Nginx không dùng IP cứng của VM2/VM3 nữa — dùng DNS tên service `tasks.api` (Swarm tự resolve ra IP container thực tế)
- Khi thêm replica thứ 3, Nginx tự thấy — không cần sửa nginx.conf
- Cert được lưu dưới dạng Docker Secret, Swarm encrypt và mount vào container trên mọi node

---

### Bước 6.1 — Khởi tạo Swarm trên VM1 (Manager)

```bash
# Trên VM1
docker swarm init --advertise-addr 192.168.1.35
```

Output sẽ cho bạn 1 lệnh join token dạng:
```
Swarm initialized: current node (xxx) is now a manager.

To add a worker to this swarm, run the following command:

    docker swarm join --token SWMTKN-1-xxx... 192.168.1.35:2377
```

Lưu lại lệnh `docker swarm join` này. Nếu quên, lấy lại bằng:
```bash
docker swarm join-token worker
```

Verify manager đang chạy:
```bash
docker node ls
# Kỳ vọng: 1 node, STATUS=Ready, MANAGER STATUS=Leader
```

---

### Bước 6.2 — Join VM2 và VM3 vào Swarm

```bash
# Trên VM2 — dán lệnh join-token từ bước trên
docker swarm join --token SWMTKN-1-xxx... 192.168.1.35:2377

# Trên VM3
docker swarm join --token SWMTKN-1-xxx... 192.168.1.35:2377
```

Verify trên VM1:
```bash
docker node ls
# Kỳ vọng:
# ID       HOSTNAME  STATUS  AVAILABILITY  MANAGER STATUS
# xxx *    vm1       Ready   Active        Leader
# yyy      vm2       Ready   Active
# zzz      vm3       Ready   Active
```

Nếu muốn node chỉ chạy worker role (không scheduling lên manager):
```bash
# Trên VM1 — đặt drain cho manager để workload chỉ chạy trên worker
docker node update --availability drain vm1
# Worker vẫn nhận task, manager chỉ orchestrate
```

> **Lưu ý port firewall:** Swarm dùng port `2377/tcp` (quản lý cluster), `7946/tcp+udp` (node discovery), `4789/udp` (overlay network). Mở các port này nếu VM dùng UFW:
> ```bash
> sudo ufw allow 2377/tcp
> sudo ufw allow 7946
> sudo ufw allow 4789/udp
> ```

---

### Bước 6.3 — Cài local registry trên VM1

Swarm cần pull image từ registry khi distribute task xuống worker. Trong lab không có Docker Hub private hay registry cloud, dùng local registry chạy ngay trên VM1.

```bash
# Trên VM1 — chạy registry container (đứng ngoài Swarm, không cần HA)
docker run -d \
  --name registry \
  --restart always \
  -p 5050:5000 \
  -v registry-data:/var/lib/registry \
  registry:2

# Verify
curl http://localhost:5050/v2/_catalog
# Kỳ vọng: {"repositories":[]}
```

**Cấu hình VM2 và VM3 để cho phép pull từ insecure registry** (HTTP thay vì HTTPS):

```bash
# Trên VM2 và VM3 — thêm insecure registry
sudo nano /etc/docker/daemon.json
```

```json
{
  "insecure-registries": ["192.168.1.35:5050"]
}
```

```bash
# Restart Docker daemon để áp dụng
sudo systemctl restart docker

# Verify (có thể pull từ registry của VM1)
docker pull 192.168.1.35:5050/hello-world || echo "registry chưa có image — OK"
```

> **Tại sao cần registry?** Khi Swarm schedule API container xuống VM2 và VM3, Docker daemon trên từng worker tự động pull image về. Nếu không có registry, worker không biết image ở đâu mà pull. Local registry là giải pháp lab nhanh nhất — production thường dùng Docker Hub private hoặc GitLab/GitHub Container Registry.

---

### Bước 6.4 — Build image và push lên local registry

Chỉ cần làm trên VM1 (manager). VM2 và VM3 tự pull khi Swarm schedule task.

```bash
# Trên VM1
cd ~/projects/OpenIdDict_MrGold

# Build image với tag trỏ vào local registry
docker build \
  -f AuthDemo.Api/Dockerfile \
  -t 192.168.1.35:5050/authdemo-api:latest \
  .

# Push lên local registry
docker push 192.168.1.35:5050/authdemo-api:latest

# Verify image đã có trong registry
curl http://localhost:5050/v2/_catalog
# Kỳ vọng: {"repositories":["authdemo-api"]}

curl http://localhost:5050/v2/authdemo-api/tags/list
# Kỳ vọng: {"name":"authdemo-api","tags":["latest"]}
```

---

### Bước 6.5 — Tạo Docker Secret (cert) và Docker Config (nginx.conf)

**Docker Secret** lưu dữ liệu nhạy cảm (cert, password) được encrypt ở Swarm store và mount vào container tại `/run/secrets/<tên>`. Không cần copy file tay sang từng VM.

```bash
# Trên VM1 — sinh cert (nếu chưa có)
cd ~/projects/OpenIdDict_MrGold/docker/certs
chmod +x gen-cert.sh && ./gen-cert.sh

# Tạo secret từ file cert
docker secret create openiddict_cert ./openiddict.pfx

# Verify secret đã tạo (Swarm chỉ cho xem tên, không cho đọc nội dung)
docker secret ls
# NAME              CREATED
# openiddict_cert   X seconds ago
```

**Docker Config** lưu file cấu hình không nhạy cảm (nginx.conf) — tương tự Secret nhưng không encrypt, có thể xem lại.

Tạo file nginx.conf cho Swarm (dùng **service name** thay vì IP cứng):

```bash
# Trên VM1
mkdir -p ~/swarm-stack
cat > ~/swarm-stack/nginx-swarm.conf << 'EOF'
events {
    worker_connections 1024;
}

http {
    upstream api_backend {
        # tasks.api: Swarm DNS đặc biệt — resolve ra IP của TẤT CẢ task đang chạy
        # Khi scale api từ 2 → 3, Nginx tự thấy replica mới mà không cần reload
        server tasks.api:8080;
        keepalive 32;
    }

    server {
        listen 80;
        server_name _;

        proxy_connect_timeout 60s;
        proxy_send_timeout    60s;
        proxy_read_timeout    60s;

        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        proxy_http_version 1.1;
        proxy_set_header Connection "";

        location / {
            proxy_pass http://api_backend;
        }
    }

    server {
        listen 8080;
        location /nginx-health {
            return 200 "nginx ok\n";
            add_header Content-Type text/plain;
        }
        location /nginx-status {
            stub_status on;
        }
    }
}
EOF

# Tạo Docker Config từ file
docker config create nginx_conf ~/swarm-stack/nginx-swarm.conf

# Verify
docker config ls
# NAME         CREATED
# nginx_conf   X seconds ago
```

> **`tasks.api` vs `api`:**
> - `api` → VIP (Virtual IP) của service — Swarm/IPVS phân phối round-robin. Nginx chỉ thấy 1 IP ảo.
> - `tasks.api` → DNS trả về danh sách IP thật của từng replica. Nginx tự load balance theo thuật toán của mình (least_conn, round-robin...).
>
> Dùng `tasks.api` để Nginx có thể áp dụng `least_conn` và thấy được từng backend thật.

---

### Bước 6.6 — Tạo docker-stack.yml

```bash
cat > ~/swarm-stack/docker-stack.yml << 'EOF'
version: '3.8'

services:

  # ── API service — chạy trên Worker nodes ────────────────────────────────────
  api:
    image: 192.168.1.35:5050/authdemo-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=192.168.1.35,1433;Database=AuthDemoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
      # Cert được mount từ Docker Secret tại /run/secrets/openiddict_cert
      - OpenIddict__CertPath=/run/secrets/openiddict_cert
      - OpenIddict__CertPassword=Lab@OpenIddict2025
    secrets:
      - openiddict_cert
    networks:
      - app-net
    deploy:
      replicas: 2
      placement:
        constraints:
          - node.role == worker     # chỉ chạy trên VM2 và VM3
      update_config:
        parallelism: 1              # rolling update: stop 1 → start 1 tại 1 thời điểm
        delay: 10s                  # chờ 10s sau mỗi replica update
        order: start-first          # start replica mới TRƯỚC khi stop cái cũ → zero downtime
        failure_action: rollback    # nếu replica mới unhealthy → tự rollback về version cũ
      rollback_config:
        parallelism: 1
        delay: 5s
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 30s             # cho phép 30s khởi động trước khi health check

  # ── Nginx load balancer — chạy trên Manager ─────────────────────────────────
  nginx:
    image: nginx:1.25-alpine
    ports:
      - "80:80"       # k6 gọi vào đây
      - "8080:8080"   # health + status
    configs:
      - source: nginx_conf
        target: /etc/nginx/nginx.conf
    networks:
      - app-net
    deploy:
      replicas: 1
      placement:
        constraints:
          - node.role == manager    # chạy trên VM1
      restart_policy:
        condition: on-failure

secrets:
  openiddict_cert:
    external: true    # đã tạo bằng: docker secret create openiddict_cert ./openiddict.pfx

configs:
  nginx_conf:
    external: true    # đã tạo bằng: docker config create nginx_conf nginx-swarm.conf

networks:
  app-net:
    driver: overlay   # overlay: container trên các node khác nhau giao tiếp được
EOF
```

> **Overlay network:** Driver `overlay` tạo một mạng ảo trải dài qua tất cả node trong Swarm. Container `nginx` trên VM1 có thể gọi thẳng đến container `api` trên VM2/VM3 qua tên service, như thể chúng cùng 1 máy.

---

### Bước 6.7 — Deploy stack từ VM1 (lần đầu và các lần sau)

```bash
# Trên VM1 — 1 lệnh deploy toàn bộ cluster
cd ~/swarm-stack
docker stack deploy -c docker-stack.yml authdemo
```

Swarm sẽ:
1. Schedule `api` service: 1 replica → VM2, 1 replica → VM3
2. Schedule `nginx` service: 1 replica → VM1 (manager)
3. Phân phối Secret `openiddict_cert` đến VM2 và VM3 (encrypt qua mTLS)
4. Mount Config `nginx_conf` vào Nginx container

Theo dõi quá trình deploy:
```bash
# Xem trạng thái các service
docker stack services authdemo
# NAME             MODE         REPLICAS   IMAGE
# authdemo_api     replicated   2/2        192.168.1.35:5050/authdemo-api:latest
# authdemo_nginx   replicated   1/1        nginx:1.25-alpine

# Xem từng task (container) đang chạy trên node nào
docker stack ps authdemo
# ID       NAME               NODE  DESIRED STATE  CURRENT STATE
# xxx      authdemo_api.1     vm2   Running        Running 30s ago
# yyy      authdemo_api.2     vm3   Running        Running 30s ago
# zzz      authdemo_nginx.1   vm1   Running        Running 30s ago
```

Verify từ máy của bạn:
```bash
curl http://192.168.1.35/health
# Kỳ vọng: {"status":"Healthy"} (từ một trong 2 API replica qua Nginx)

# Verify round-robin — gọi 6 lần
for i in $(seq 1 6); do
  curl -s http://192.168.1.35/health
  echo " - request $i"
done
```

---

### Bước 6.8 — Workflow khi có thay đổi

#### Thay đổi code (build và deploy version mới)

Tất cả làm trên VM1, VM2 và VM3 không cần động vào:

```bash
# 1. Kéo code mới
cd ~/projects/OpenIdDict_MrGold
git pull

# 2. Build image với tag version (dùng git hash để dễ rollback)
VERSION=$(git rev-parse --short HEAD)
docker build \
  -f AuthDemo.Api/Dockerfile \
  -t 192.168.1.35:5050/authdemo-api:${VERSION} \
  -t 192.168.1.35:5050/authdemo-api:latest \
  .

# 3. Push lên registry
docker push 192.168.1.35:5050/authdemo-api:${VERSION}
docker push 192.168.1.35:5050/authdemo-api:latest

# 4. Rolling update — Swarm update từng replica 1, zero downtime
docker service update \
  --image 192.168.1.35:5050/authdemo-api:${VERSION} \
  authdemo_api

# Theo dõi quá trình rolling update
docker service ps authdemo_api
# Sẽ thấy: replica cũ shutdown SAU KHI replica mới healthy
```

Nếu version mới có vấn đề, rollback về version trước:
```bash
docker service rollback authdemo_api
# Swarm tự đổi lại image của từng replica về version cũ
```

#### Thay đổi environment variable

```bash
# Cập nhật env var trực tiếp, không cần rebuild image
docker service update \
  --env-add "ConnectionStrings__DefaultConnection=Server=new-db,1433;..." \
  authdemo_api
# Swarm rolling restart từng replica với env mới
```

#### Thay đổi nginx.conf

```bash
# 1. Xóa config cũ và tạo config mới (Docker Config immutable — không edit được)
docker config rm nginx_conf

# Sửa file nginx-swarm.conf
nano ~/swarm-stack/nginx-swarm.conf

# Tạo config version mới
docker config create nginx_conf_v2 ~/swarm-stack/nginx-swarm.conf

# 2. Update service Nginx trỏ vào config mới
docker service update \
  --config-rm nginx_conf \
  --config-add source=nginx_conf_v2,target=/etc/nginx/nginx.conf \
  authdemo_nginx
```

> **Lưu ý:** Docker Config và Secret là immutable — một khi tạo xong không sửa được nội dung. Muốn đổi phải tạo config/secret mới với tên khác và update service trỏ vào cái mới.

---

### Bước 6.9 — Scale service

```bash
# Scale API từ 2 → 3 replica (Swarm tự chọn node phù hợp)
docker service scale authdemo_api=3

# Xem Swarm schedule replica mới lên node nào
docker stack ps authdemo
# Replica thứ 3 có thể lên VM2 hoặc VM3 tùy tải hiện tại

# Scale ngược lại về 2
docker service scale authdemo_api=2

# Khi có VM4 mới join Swarm
docker swarm join --token ... 192.168.1.35:2377  # (chạy trên VM4)
# Scale lên 4 — replica thứ 4 sẽ tự xuống VM4
docker service scale authdemo_api=4
```

> **Nginx `tasks.api` tự cập nhật:** Khi scale api từ 2 → 3, DNS record của `tasks.api` tự thêm IP của replica mới. Nginx (với `resolver` hoặc `keepalive` đủ thấp) sẽ tự nhận thêm backend mà không cần sửa config hay reload.

---

### Bước 6.10 — Monitoring Swarm

```bash
# Xem tất cả node trong cluster
docker node ls

# Xem detail 1 node (tải, resource)
docker node inspect vm2 --pretty

# Xem tất cả service đang chạy
docker service ls

# Xem log của service (gộp từ tất cả replica)
docker service logs authdemo_api --follow --tail 50

# Xem resource usage (cần chạy trên từng node hoặc dùng Portainer)
docker stats $(docker ps -q)

# Xem lịch sử task (restart, failure)
docker service ps authdemo_api --no-trunc
```

**Portainer — UI quản lý Swarm (tùy chọn):**

```bash
# Trên VM1 — chạy Portainer agent trên tất cả node
docker stack deploy -c ~/swarm-stack/portainer-agent.yml portainer
```

```yaml
# ~/swarm-stack/portainer-agent.yml
version: '3.8'
services:
  agent:
    image: portainer/agent:latest
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - /var/lib/docker/volumes:/var/lib/docker/volumes
    networks:
      - agent-net
    deploy:
      mode: global    # 1 agent trên MỌI node, tự động
  portainer:
    image: portainer/portainer-ce:latest
    command: -H tcp://tasks.agent:9001 --tlsskipverify
    ports:
      - "9000:9000"
    volumes:
      - portainer-data:/data
    networks:
      - agent-net
    deploy:
      placement:
        constraints:
          - node.role == manager
networks:
  agent-net:
    driver: overlay
    attachable: true
volumes:
  portainer-data:
```

```bash
docker stack deploy -c ~/swarm-stack/portainer-agent.yml portainer
# Mở http://192.168.1.35:9000 → xem toàn bộ Swarm qua UI
```

---

### Bước 6.11 — Xử lý SQL Server và Monitoring (nằm ngoài Swarm)

SQL Server và monitoring stack (Prometheus, Grafana, InfluxDB) **không nên đặt trong Swarm** vì:
- SQL Server cần volume persistent, không được migrate sang node khác giữa chừng
- Monitoring cần access vào Docker socket và metadata của host

Vẫn chạy chúng bằng `docker compose` trực tiếp trên VM1 như cũ:

```bash
# Trên VM1 — monitoring stack vẫn dùng compose bình thường
cd ~/projects/OpenIdDict_MrGold/docker
docker compose -f docker-compose.monitoring.yml up -d
```

Tách biệt rõ ràng:
- **Swarm** quản lý: `api` service (stateless, có thể chạy ở bất kỳ node nào)
- **Compose** quản lý: SQL Server, Prometheus, Grafana, InfluxDB (stateful, gắn với VM1)

---

### So sánh: Manual Deploy vs Docker Swarm

| Tình huống | Manual (Phase 2–3) | Docker Swarm (Phase 6) |
|---|---|---|
| **Lần đầu deploy** | SSH từng VM, compose up | `docker stack deploy` từ VM1 |
| **Update code** | Build + up từng VM | Build 1 lần, `docker service update` |
| **Update env var** | Sửa file từng VM + restart | `docker service update --env-add` |
| **Update nginx.conf** | SSH vào VM3, sửa + reload | `docker config create` + `service update` |
| **Scale 2→3 replica** | Tạo VM, cài tay, cập nhật Nginx | `docker service scale authdemo_api=3` |
| **Cert quản lý** | Copy thủ công, dễ quên | Docker Secret — Swarm phân phối tự động |
| **Rolling update** | Không có, downtime | Zero downtime — start-first |
| **Rollback** | Không có | `docker service rollback` |
| **Node health** | Không tự xử lý | Swarm tự restart task khi node fail |
| **Quan sát cluster** | `docker ps` từng VM | `docker service ps` từ 1 nơi |

### Khi nào nên dùng Swarm vs Kubernetes?

| | Docker Swarm | Kubernetes |
|---|---|---|
| **Độ phức tạp** | Thấp — built-in Docker, ít khái niệm | Cao — nhiều abstraction (Pod, Deployment, Service, Ingress...) |
| **Thời gian setup** | 30 phút | Vài giờ đến vài ngày |
| **Phù hợp** | Lab, startup nhỏ-vừa, team ít người | Production lớn, nhiều service, nhiều team |
| **Ecosystem** | Hạn chế | Rất phong phú (Helm, Istio, ArgoCD...) |
| **Managed cloud** | Không có (tự quản) | EKS, GKE, AKS |
| **Kết luận** | **Đúng với bài lab này** | Overkill cho 3 VM |

> **Thực tế:** Docker Swarm đủ dùng cho team 5–20 người với vài chục service. Nhiều startup thành công vận hành production trên Swarm nhiều năm mà không cần migrate sang Kubernetes. Chỉ chuyển K8s khi thật sự cần: auto-scaling theo metric (HPA), multi-region, CI/CD pipeline phức tạp, hoặc team đủ lớn để vận hành.

---

### Nginx placement trong production — KHÔNG đặt trên Manager

Cách lab này đặt Nginx trên manager (`placement: constraints: node.role == manager`) là **chấp nhận được cho học tập** nhưng sai nguyên tắc production vì 3 lý do:

**1. Manager phải được bảo vệ, không expose ra internet**

Manager giữ Raft consensus state của toàn cluster. Nếu manager bị tấn công hoặc quá tải vì xử lý traffic, toàn bộ Swarm mất control plane — không deploy được, không scale được, không xem được trạng thái cluster.

**2. Single point of failure cho traffic**

Nginx trên manager chết = toàn bộ traffic chết, dù API worker vẫn healthy hoàn toàn. Đây là SPOF không cần thiết.

**3. Resource contention**

Swarm management overhead + Nginx xử lý hàng nghìn connection/s trên cùng 1 node → cả 2 đều bị ảnh hưởng lẫn nhau.

---

#### 3 pattern production phổ biến

**Pattern 1 — Dedicated edge worker (on-premise, bare metal)**

Đây là cách phổ biến nhất khi tự vận hành Swarm:

```
Internet
    │
    ▼
[Edge Worker VM2]  [Edge Worker VM3]   ← Nginx chạy ở đây
    │                    │
    └────────┬───────────┘
             │ overlay network
             ▼
    [Worker pool — API replicas]
             │
    [VM4, VM5 — Managers × 3 hoặc 5]  ← KHÔNG nhận traffic
```

Cấu hình trong stack file: gắn label cho node và dùng `placement.constraints`:

```bash
# Trên VM1 (manager) — gắn label cho edge worker
docker node update --label-add role=edge vm2
docker node update --label-add role=edge vm3
```

```yaml
# docker-stack.yml
services:
  nginx:
    image: nginx:1.25-alpine
    ports:
      - "80:80"
    deploy:
      replicas: 2             # 2 replica — 1 trên VM2, 1 trên VM3 → HA
      placement:
        constraints:
          - node.labels.role == edge   # chạy trên edge worker, không phải manager
      update_config:
        parallelism: 1
        order: start-first

  api:
    image: 192.168.1.35:5050/authdemo-api:latest
    deploy:
      replicas: 4
      placement:
        constraints:
          - node.labels.role != edge   # API không chạy trên edge worker
          - node.role == worker
```

Với `replicas: 2` cho Nginx và 2 edge node → Swarm đặt 1 replica trên VM2, 1 trên VM3 — Nginx vừa được load balance vừa không SPOF.

---

**Pattern 2 — External Load Balancer + Swarm routing mesh (cloud)**

Khi deploy trên cloud (AWS, GCP, Azure), external LB đứng trước cluster, traffic đến bất kỳ worker nào, Swarm routing mesh tự route đến container đúng — không cần Nginx trong Swarm:

```
Internet
    │
    ▼
[AWS ALB / GCP Load Balancer]   ← cloud-managed, HA tự động
    │
    ├──→ Worker VM2 :80
    ├──→ Worker VM3 :80
    └──→ Worker VM4 :80
         │
         Swarm ingress routing mesh
         (bất kỳ node nào cũng nhận được và route đến container đúng)
```

Swarm mở port 80 trên **mọi node** khi bạn publish port trong stack. Cloud LB chỉ cần trỏ vào IP của tất cả worker — không cần biết container đang chạy trên node nào.

```yaml
services:
  api:
    image: ...
    ports:
      - "80:8080"   # publish port 80 trên mọi node trong Swarm
    deploy:
      replicas: 3
      # Không cần placement constraint — Swarm tự distribute
```

Với pattern này hoàn toàn không cần Nginx container nào trong Swarm. Cloud LB xử lý TLS termination, health check, và phân phối traffic.

---

**Pattern 3 — Traefik thay Nginx (dynamic routing, nhiều service)**

Khi có nhiều service cần routing theo path (`/api/v1`, `/api/v2`, `/admin`...) hoặc hostname, Traefik là lựa chọn phổ biến hơn Nginx trong Swarm vì nó tự động detect service mới qua Docker labels — không cần reload config:

```yaml
services:
  traefik:
    image: traefik:v3
    command:
      - "--providers.swarm=true"                    # auto-discover Swarm services
      - "--providers.swarm.exposedbydefault=false"
      - "--entrypoints.web.address=:80"
    ports:
      - "80:80"
      - "8080:8080"   # Traefik dashboard
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    deploy:
      placement:
        constraints:
          - node.labels.role == edge   # vẫn đặt trên edge worker

  api:
    image: 192.168.1.35:5050/authdemo-api:latest
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.api.rule=PathPrefix(`/api`) || PathPrefix(`/connect`)"
      - "traefik.http.services.api.loadbalancer.server.port=8080"
    deploy:
      replicas: 3
      # Không cần ports — Traefik tự forward
```

Khi deploy service mới hoặc scale, Traefik tự cập nhật routing mà không cần restart hay reload.

---

#### Tóm tắt: Nginx đặt ở đâu?

| Môi trường | Nginx/LB đặt ở đâu | Ghi chú |
|---|---|---|
| **Lab 3 VM** | Manager (chấp nhận được) | Đơn giản, đủ để học |
| **On-premise production** | Dedicated edge worker với label | Tách biệt traffic khỏi control plane |
| **Cloud (AWS/GCP/Azure)** | External LB của cloud | Không cần Nginx trong Swarm |
| **Nhiều service, routing phức tạp** | Traefik trên edge worker | Auto-discover, không cần reload config |

> **Nguyên tắc bất biến:** Manager không nhận traffic từ internet. Dù dùng Swarm hay Kubernetes, node control plane luôn được isolate khỏi data plane (traffic path).

---

## Phase 7 — VM Registry riêng: Tách registry khỏi Manager

### Tại sao cần VM registry riêng?

Trong Phase 6, registry chạy trên VM1 (manager) là shortcut cho lab. Vấn đề khi để lâu dài:

```
Registry trên Manager:
  ✗ Image storage chiếm disk của manager
  ✗ Registry push/pull tạo network I/O cạnh tranh với Swarm management traffic
  ✗ Nếu manager restart → registry downtime → mọi deploy/pull bị block
  ✗ Không scale được — 1 registry trên 1 node
```

```
Registry trên VM riêng:
  ✓ Disk riêng, không ảnh hưởng manager
  ✓ Có thể restart/upgrade độc lập
  ✓ Dễ backup (chỉ cần backup 1 volume trên VM này)
  ✓ Sau này có thể nâng lên Harbor (enterprise registry) mà không đụng cluster
```

### Kiến trúc sau khi thêm VM_REG

```
[k6 — máy bạn]
      │
      ▼
[VM3 / Edge Worker — Nginx :80]
      │
      │ overlay network
      ▼
[VM2 / Worker — api replica]    [VM4 / Worker — api replica]

[VM1 — Manager]                 [VM_REG — 192.168.1.38]
  ├── SQL Server (:1433)          └── registry (:5000)
  ├── prometheus (:9090)              └── /var/lib/registry  ← image storage
  └── grafana (:3000)

Luồng deploy:
  Developer → git push
  VM1 (manager): git pull → docker build → docker push → VM_REG:5000
  VM2, VM3 (workers): docker pull ← VM_REG:5000  (Swarm tự kéo khi deploy)
```

| VM | IP | Vai trò |
|---|---|---|
| VM1 | `192.168.1.35` | Swarm Manager + SQL Server + Monitoring |
| VM2 | `192.168.1.36` | Swarm Worker |
| VM3 | `192.168.1.37` | Swarm Worker (edge) |
| **VM_REG** | **`192.168.1.38`** | **Docker Registry** |

---

### Bước 7.1 — Tạo VM_REG

Yêu cầu tối thiểu:
- OS: Ubuntu Server 22.04 LTS
- CPU: 1–2 core (registry không tốn CPU, chủ yếu I/O)
- RAM: 2 GB
- Disk: **50 GB trở lên** — image layer tích lũy nhanh (mỗi build ~200–500 MB)
- IP: `192.168.1.38`

```bash
# Trên VM_REG — đặt IP tĩnh
sudo nano /etc/netplan/00-installer-config.yaml
```

```yaml
network:
  version: 2
  ethernets:
    ens33:
      dhcp4: false
      addresses:
        - 192.168.1.38/24
      gateway4: 192.168.1.1
      nameservers:
        addresses: [8.8.8.8, 8.8.4.4]
```

```bash
sudo netplan apply

# Cài Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
```

---

### Bước 7.2 — Chọn chế độ: HTTP (lab) hay HTTPS (production-like)

| | HTTP (insecure) | HTTPS self-signed |
|---|---|---|
| **Setup** | 5 phút | 15 phút |
| **Cấu hình client** | Thêm `insecure-registries` vào daemon.json của mọi VM | Copy cert CA vào mọi VM |
| **Bảo mật** | Không mã hóa | Mã hóa TLS |
| **Dùng khi** | Lab nội bộ, không ra internet | Gần production hơn, vẫn là private network |

---

### Bước 7.3A — Cách HTTP (insecure) — đơn giản, đủ cho lab

**Trên VM_REG — chạy registry:**

```bash
mkdir -p ~/registry
cat > ~/registry/docker-compose.yml << 'EOF'
services:
  registry:
    image: registry:2
    container_name: registry
    ports:
      - "5000:5000"
    environment:
      # Tắt delete mặc định — bật để garbage collect được
      - REGISTRY_STORAGE_DELETE_ENABLED=true
    volumes:
      - registry-data:/var/lib/registry
    restart: unless-stopped

volumes:
  registry-data:
EOF

cd ~/registry
docker compose up -d

# Verify
curl http://localhost:5000/v2/
# Kỳ vọng: {}
```

**Trên VM1, VM2, VM3 — cấu hình Docker daemon chấp nhận insecure registry:**

```bash
# Chạy trên TỪNG VM (VM1, VM2, VM3)
sudo nano /etc/docker/daemon.json
```

```json
{
  "insecure-registries": ["192.168.1.38:5000"]
}
```

```bash
# Restart Docker daemon để áp dụng (trên từng VM)
sudo systemctl restart docker

# Verify — test pull từ registry
docker pull 192.168.1.38:5000/hello-world 2>&1 | head -3
# Kỳ vọng: "Error response from daemon: manifest unknown" hoặc similar
# (lỗi này là đúng — chỉ confirm Docker đã kết nối được đến registry, không phải lỗi config)
```

> **Lưu ý khi restart Docker trên Swarm worker:** Swarm tự restart container sau khi Docker daemon khởi động lại. Có thể mất vài giây downtime. Làm lần lượt từng worker, không làm đồng thời.

---

### Bước 7.3B — Cách HTTPS self-signed — gần production hơn

#### Trên VM_REG — sinh self-signed certificate

```bash
mkdir -p ~/registry/certs

# Sinh cert với SAN (Subject Alternative Name) — bắt buộc từ Docker 20.x trở đi
# Nếu dùng IP, phải có subjectAltName=IP:... thì Docker mới tin
openssl req -newkey rsa:4096 -nodes -sha256 \
  -keyout ~/registry/certs/registry.key \
  -x509 -days 3650 \
  -out ~/registry/certs/registry.crt \
  -subj "/C=VN/ST=HCM/L=HoChiMinh/O=Lab/CN=192.168.1.38" \
  -addext "subjectAltName=IP:192.168.1.38"

ls -la ~/registry/certs/
# Kỳ vọng: registry.crt (~2KB) và registry.key (~3KB)
```

**Chạy registry với TLS:**

```bash
cat > ~/registry/docker-compose.yml << 'EOF'
services:
  registry:
    image: registry:2
    container_name: registry
    ports:
      - "5000:5000"
    environment:
      - REGISTRY_HTTP_TLS_CERTIFICATE=/certs/registry.crt
      - REGISTRY_HTTP_TLS_KEY=/certs/registry.key
      - REGISTRY_STORAGE_DELETE_ENABLED=true
    volumes:
      - ./certs:/certs:ro
      - registry-data:/var/lib/registry
    restart: unless-stopped

volumes:
  registry-data:
EOF

cd ~/registry
docker compose up -d

# Verify TLS đang chạy
curl https://192.168.1.38:5000/v2/ --cacert ~/registry/certs/registry.crt
# Kỳ vọng: {}
```

#### Copy cert CA sang tất cả VM để Docker tin tưởng

Docker có cơ chế riêng để trust cert: thư mục `/etc/docker/certs.d/<registry-host>/ca.crt`. Không cần thêm vào system CA store, không cần restart Docker daemon.

```bash
# Trên VM_REG — copy cert ra host để scp
# (cert đã ở ~/registry/certs/registry.crt)

# Từ VM_REG, push cert sang VM1, VM2, VM3
for HOST in 192.168.1.35 192.168.1.36 192.168.1.37; do
  ssh bank@${HOST} "sudo mkdir -p /etc/docker/certs.d/192.168.1.38:5000"
  scp ~/registry/certs/registry.crt \
      bank@${HOST}:/tmp/registry-ca.crt
  ssh bank@${HOST} "sudo mv /tmp/registry-ca.crt /etc/docker/certs.d/192.168.1.38:5000/ca.crt"
done

# Verify trên VM1 (không cần restart Docker)
ssh bank@192.168.1.35 "docker pull 192.168.1.38:5000/hello-world 2>&1 | head -3"
# Kỳ vọng: lỗi "manifest unknown" — Docker đã kết nối HTTPS thành công
```

> **Tại sao không cần restart Docker?** Docker đọc `/etc/docker/certs.d/` mỗi khi tạo kết nối mới — không cache vào memory như system CA store.

---

### Bước 7.4 — Thêm Basic Authentication (tùy chọn, khuyến nghị)

Không có auth → bất kỳ ai trong mạng LAN đều push/pull được. Trong lab thì không sao, nhưng staging/production nên bật.

```bash
# Trên VM_REG — tạo file password
mkdir -p ~/registry/auth

# Dùng htpasswd (httpd:2 image có sẵn tool này)
docker run --rm httpd:2 \
  htpasswd -Bbn admin Admin@Registry2025 \
  > ~/registry/auth/htpasswd

cat ~/registry/auth/htpasswd
# Kỳ vọng: admin:$2y$05$...  (bcrypt hash)
```

Cập nhật `docker-compose.yml` trên VM_REG (thêm auth vào environment, chọn 1 trong 2 cách dưới):

```yaml
# Thêm vào environment của registry service:
environment:
  - REGISTRY_HTTP_TLS_CERTIFICATE=/certs/registry.crt
  - REGISTRY_HTTP_TLS_KEY=/certs/registry.key
  - REGISTRY_STORAGE_DELETE_ENABLED=true
  - REGISTRY_AUTH=htpasswd
  - REGISTRY_AUTH_HTPASSWD_REALM=Private Registry
  - REGISTRY_AUTH_HTPASSWD_PATH=/auth/htpasswd

# Thêm vào volumes:
volumes:
  - ./certs:/certs:ro
  - ./auth:/auth:ro
  - registry-data:/var/lib/registry
```

```bash
# Restart registry với auth
cd ~/registry
docker compose up -d --force-recreate
```

**Login từ VM1, VM2, VM3:**

```bash
# Chạy trên từng VM — login 1 lần, credentials được lưu vào ~/.docker/config.json
docker login 192.168.1.38:5000
# Username: admin
# Password: Admin@Registry2025
# Login Succeeded

# Verify credentials đã lưu
cat ~/.docker/config.json
# Kỳ vọng: thấy "192.168.1.38:5000" với auths entry
```

**Khi dùng với Docker Swarm** — worker cần credentials để pull image, truyền qua flag `--with-registry-auth`:

```bash
# Trên VM1 — deploy stack với registry auth
docker stack deploy \
  --with-registry-auth \
  -c docker-stack.yml authdemo
# Swarm manager tự phân phối credentials xuống từng worker node
```

---

### Bước 7.5 — Cập nhật docker-stack.yml trỏ vào VM_REG

Chỉ đổi image tag từ `192.168.1.35:5050` (registry cũ trên manager) thành `192.168.1.38:5000`:

```yaml
services:
  api:
    image: 192.168.1.38:5000/authdemo-api:latest   # ← đổi sang VM_REG
    # ... phần còn lại giữ nguyên
```

---

### Bước 7.6 — Build và push image lên VM_REG

```bash
# Trên VM1 — build và push lên registry mới
cd ~/projects/OpenIdDict_MrGold

VERSION=$(git rev-parse --short HEAD)

docker build \
  -f AuthDemo.Api/Dockerfile \
  -t 192.168.1.38:5000/authdemo-api:${VERSION} \
  -t 192.168.1.38:5000/authdemo-api:latest \
  .

docker push 192.168.1.38:5000/authdemo-api:${VERSION}
docker push 192.168.1.38:5000/authdemo-api:latest

# Verify image đã có trong registry
curl http://192.168.1.38:5000/v2/_catalog
# Kỳ vọng: {"repositories":["authdemo-api"]}

curl http://192.168.1.38:5000/v2/authdemo-api/tags/list
# Kỳ vọng: {"name":"authdemo-api","tags":["latest","abc1234"]}
```

---

### Bước 7.7 — Verify worker pull được từ VM_REG

```bash
# Trên VM2 — test pull thủ công
docker pull 192.168.1.38:5000/authdemo-api:latest
# Kỳ vọng: Pull thành công, thấy digest của image

# Deploy stack — Swarm tự kéo image xuống worker
# Trên VM1:
docker stack deploy -c docker-stack.yml authdemo

# Theo dõi task — xác nhận image được pull từ VM_REG
docker service ps authdemo_api
# Cột IMAGE phải hiển thị: 192.168.1.38:5000/authdemo-api:latest
```

---

### Bước 7.8 — Dọn dẹp image cũ (Garbage Collection)

Registry tích lũy layer orphan sau nhiều lần push. Chạy garbage collect định kỳ để giải phóng disk:

```bash
# Trên VM_REG — chạy garbage collect (registry phải dừng hoặc ở read-only mode)
# Cách an toàn nhất: đặt registry read-only, GC, rồi bật lại

# Bước 1: Đặt registry vào read-only mode (từ chối push mới, vẫn cho pull)
docker exec registry \
  registry garbage-collect --dry-run /etc/docker/registry/config.yml
# --dry-run: chỉ liệt kê sẽ xóa gì, chưa xóa thật

# Bước 2: Nếu dry-run OK, chạy thật
docker exec registry \
  registry garbage-collect /etc/docker/registry/config.yml

# Xem dung lượng trước/sau
docker exec registry du -sh /var/lib/registry
```

**Xóa tag cụ thể trước khi GC:**

```bash
# Lấy digest của image muốn xóa
DIGEST=$(curl -s -H "Accept: application/vnd.docker.distribution.manifest.v2+json" \
  http://192.168.1.38:5000/v2/authdemo-api/manifests/v1.0.0 \
  -I | grep Docker-Content-Digest | awk '{print $2}' | tr -d '\r')

# Xóa manifest (cần REGISTRY_STORAGE_DELETE_ENABLED=true)
curl -X DELETE \
  "http://192.168.1.38:5000/v2/authdemo-api/manifests/${DIGEST}"

# Sau đó chạy GC để giải phóng layer thật sự
docker exec registry \
  registry garbage-collect /etc/docker/registry/config.yml
```

---

### Debug thường gặp với Registry

**`x509: cannot validate certificate for 192.168.1.38`**

```
Docker không tìm thấy CA cert để verify TLS của registry.
```

Fix:
```bash
# Trên VM bị lỗi — kiểm tra cert đã đúng chỗ chưa
ls -la /etc/docker/certs.d/192.168.1.38:5000/
# Kỳ vọng: thấy ca.crt

# Nếu chưa có, copy từ VM_REG
scp bank@192.168.1.38:~/registry/certs/registry.crt \
    /tmp/registry-ca.crt
sudo mkdir -p /etc/docker/certs.d/192.168.1.38:5000
sudo mv /tmp/registry-ca.crt /etc/docker/certs.d/192.168.1.38:5000/ca.crt
# Không cần restart Docker
```

---

**`http: server gave HTTP response to HTTPS client`**

```
Docker mặc định dùng HTTPS nhưng registry đang chạy HTTP.
```

Fix: Thêm vào `/etc/docker/daemon.json` trên VM bị lỗi:
```json
{
  "insecure-registries": ["192.168.1.38:5000"]
}
```
```bash
sudo systemctl restart docker
```

---

**`unauthorized: authentication required` khi Swarm worker pull**

```
Worker không có credentials để pull từ registry có auth.
```

Fix: Deploy stack với flag `--with-registry-auth`:
```bash
# Trên VM1 (manager) — phải login trước, rồi deploy với flag này
docker login 192.168.1.38:5000
docker stack deploy --with-registry-auth -c docker-stack.yml authdemo
```

Swarm manager sẽ phân phối credentials (được mã hóa) xuống từng worker tự động.

---

**Push thành công nhưng Swarm worker vẫn dùng image cũ**

```
Docker cache image local — Swarm không tự pull lại nếu tag không đổi.
```

Fix: Dùng digest thay vì tag `latest`, hoặc force update:
```bash
# Cách 1: Force pull khi update service
docker service update \
  --force \
  --image 192.168.1.38:5000/authdemo-api:latest \
  authdemo_api
# --force: bắt buộc recreate task dù image tag không đổi

# Cách 2 (best practice): Luôn dùng version tag cụ thể
docker service update \
  --image 192.168.1.38:5000/authdemo-api:${VERSION} \
  authdemo_api
```

---

### Tổng kết kiến trúc hoàn chỉnh

```
[Developer — máy Windows]
    │
    │ git push
    ▼
[Git repo]
    │
    │ git pull (CI hoặc thủ công)
    ▼
[VM1 — Manager 192.168.1.35]
    │ docker build
    │ docker push
    ▼
[VM_REG — Registry 192.168.1.38:5000]
    │                   ↑
    │ image layers       │ Swarm worker tự pull
    ▼                   │
[VM2 — Worker .36]   [VM3 — Worker .37]
  api replica 1         api replica 2
    │                        │
    └─────────┬──────────────┘
              │ SQL Server connection
              ▼
    [VM1 — SQL Server :1433]

Monitoring (VM1): Prometheus scrape VM2, VM3 → Grafana dashboard
```

**Mỗi component có thể restart/upgrade độc lập:**
- VM_REG down → deploy bị block nhưng cluster vẫn chạy bình thường (worker đã có image)
- VM1 manager down → cluster tiếp tục chạy, nhưng không deploy/scale được
- Worker down → Swarm tự reschedule replica sang worker còn lại

---

## Phase 8 — Nginx Proxy Manager (NPM) trong hệ thống công ty

### NPM và Nginx của project — 2 tầng khác nhau, không cạnh tranh

Câu hỏi thường gặp: "Công ty đã có NPM rồi, deploy project mới thì Nginx trong Swarm/VM còn cần không?"

Trả lời: **cần cả 2**, vì chúng làm 2 việc hoàn toàn khác nhau.

| | **NPM (Nginx Proxy Manager)** | **Project Nginx (trong Swarm/VM)** |
|---|---|---|
| **Vai trò** | Edge proxy — cổng vào từ ngoài | Internal LB — phân phối giữa các API replica |
| **Quản lý** | Toàn bộ công ty/team (nhiều project) | 1 project cụ thể |
| **SSL/TLS** | Có — Let's Encrypt tự động gia hạn | Không cần — nội mạng không cần mã hóa |
| **Domain** | Ánh xạ domain → project cụ thể | Không biết domain, chỉ biết upstream |
| **Scope** | Biết tất cả project | Chỉ biết API replicas của project mình |

```
[Browser / Team / Internet]
           │
           │ HTTPS (SSL do NPM quản lý)
           ▼
  ┌────────────────────┐
  │  NPM — :443        │   ← VM NPM của công ty (đã có sẵn)
  │  project-a.dev     │──→ http://192.168.1.37:80   (Project A)
  │  project-b.dev     │──→ http://192.168.1.50:80   (Project B)
  │  admin.dev         │──→ http://192.168.1.60:80   (Admin)
  └────────────────────┘
           │
           │ HTTP (nội mạng, không cần SSL)
           ▼
  ┌────────────────────┐
  │  Project Nginx :80 │   ← Nginx của project này (Swarm service hoặc VM riêng)
  │  upstream api      │
  └────────┬───────────┘
           │
      ┌────┴────┐
      ▼         ▼
  [API VM2]  [API VM3]     ← API replicas
```

**Lý do traffic nội bộ (NPM → Project Nginx) dùng HTTP, không cần HTTPS:**
- Đây là traffic trong private network (LAN/VLAN nội bộ) — không đi qua internet
- SSL/TLS tốn CPU để encrypt/decrypt mà không tăng thêm bảo mật khi đã trên nội mạng
- NPM đã terminate SSL ở edge — đây là pattern "SSL termination at edge" chuẩn

---

### 3 pattern tích hợp với NPM

**Pattern 1 — NPM → Project Nginx → API (khuyến nghị)**

Đây là pattern chuẩn khi project cần load balancing nâng cao (least_conn, health check, header injection):

```
NPM ──HTTP──→ Project Nginx :80 ──→ API replica 1
                                ──→ API replica 2
                                ──→ API replica 3
```

**Cấu hình trong NPM:**

Vào NPM UI → Proxy Hosts → Add Proxy Host:

```
Domain Names:     project-a.yourdomain.com
Scheme:           http
Forward Hostname: 192.168.1.37          ← IP của VM chạy Nginx, hoặc IP bất kỳ worker nếu dùng Swarm
Forward Port:     80
Cache Assets:     OFF
Block Common Exploits: ON

SSL tab:
  SSL Certificate: Let's Encrypt (NPM tự xin và gia hạn)
  Force SSL: ON
  HTTP/2 Support: ON
```

Project Nginx giữ nguyên config như Phase 3 hoặc Phase 6 — không đổi gì.

---

**Pattern 2 — NPM → API trực tiếp (không có project Nginx)**

Khi project chỉ có 1 API instance, hoặc NPM đủ làm LB:

```
NPM ──HTTP──→ API VM1 :5000
          ──→ API VM2 :5000   (NPM tự round-robin nếu cấu hình upstream)
```

Với NPM cơ bản (open-source), upstream chỉ là 1 host. Muốn round-robin nhiều host cần NPM phiên bản có hỗ trợ upstream, hoặc dùng custom Nginx config.

> **Giới hạn:** NPM open-source không có UI để cấu hình upstream nhiều host. Nếu cần LB thực sự, vẫn cần Project Nginx.

---

**Pattern 3 — NPM → Swarm routing mesh (không cần Project Nginx riêng)**

Khi dùng Docker Swarm với routing mesh, bất kỳ worker node nào cũng có thể nhận traffic và tự route vào đúng container. NPM chỉ cần trỏ vào 1 trong các worker:

```
NPM ──HTTP──→ 192.168.1.36:5000 (worker VM2, port được Swarm publish)
              Swarm routing mesh tự route → API replica 1, 2, 3
```

**Cấu hình trong NPM:**

```
Forward Hostname: 192.168.1.36      ← IP bất kỳ worker trong Swarm
Forward Port:     5000              ← port Swarm đang publish
```

Swarm đảm bảo bất kể request vào worker nào, đều được forward đến replica đang available.

> **Nhược điểm:** NPM chỉ biết 1 worker → nếu worker đó chết, NPM không tự failover. Workaround: dùng HAProxy hoặc keepalived trước NPM để có VIP, hoặc cấu hình NPM upstream nhiều host (nếu version hỗ trợ).

---

### Khi nào KHÔNG routing qua NPM

Một số trường hợp project **không** cần đi qua NPM:

| Trường hợp | Xử lý |
|---|---|
| API chỉ cho internal service gọi (service-to-service) | Gọi thẳng IP:port nội mạng, không cần domain, không cần SSL |
| Staging/dev environment trong LAN | Truy cập qua IP:port trực tiếp, không cần domain |
| Database, Redis, message queue | Không bao giờ expose qua NPM — chỉ cho phép internal |
| Monitoring (Grafana, Prometheus) | Có thể qua NPM nếu cần truy cập từ ngoài, không bắt buộc |

---

### Checklist khi thêm project mới vào NPM

```
[ ] 1. Project Nginx/Swarm đã chạy và healthy (curl http://internal-ip/health → OK)
[ ] 2. Kiểm tra port không conflict với project khác trong NPM
[ ] 3. Tạo Proxy Host trong NPM:
        - Domain: project.yourdomain.com
        - Forward: http://internal-ip:port
        - SSL: Let's Encrypt
[ ] 4. Verify domain resolve đúng (dig project.yourdomain.com)
[ ] 5. Test HTTPS từ trình duyệt
[ ] 6. Test API qua HTTPS:
        curl https://project.yourdomain.com/health
[ ] 7. Kiểm tra NPM không cache response API (Cache Assets: OFF)
[ ] 8. Cấu hình timeout phù hợp nếu API có endpoint chạy lâu:
```

**Cấu hình timeout trong NPM (Advanced tab — custom Nginx config):**

```nginx
# Dán vào ô "Advanced" của Proxy Host trong NPM UI
proxy_connect_timeout 60s;
proxy_send_timeout    300s;   # tăng nếu có endpoint xử lý lâu (export, report...)
proxy_read_timeout    300s;
proxy_buffering       off;    # tắt buffering nếu dùng SSE/WebSocket/streaming
```

**Nếu project dùng WebSocket (SignalR):**

```nginx
# Advanced tab trong NPM
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
proxy_set_header Host $host;
proxy_cache_bypass $http_upgrade;
```

---

### Tóm tắt: Dùng NPM như thế nào?

```
                    ┌─────────────────────────────────────────────┐
                    │              NPM (1 VM, toàn công ty)        │
                    │                                              │
                    │  project-a.dev → http://192.168.1.37:80     │
                    │  project-b.dev → http://192.168.1.50:80     │
                    │  admin.dev     → http://192.168.1.60:5000   │
                    └────────────────────┬────────────────────────┘
                                         │ HTTP (nội mạng)
                         ┌───────────────┼───────────────┐
                         ▼               ▼               ▼
               [Project A Nginx]  [Project B API]  [Admin API]
               (Swarm service)    (single VM)      (single VM)
                    │
               ┌────┴────┐
           [API VM2]  [API VM3]
```

> **Nguyên tắc:**
> - NPM = cổng vào duy nhất từ ngoài → mọi project đều qua đây để có SSL + domain
> - Project Nginx = load balancer nội bộ → chỉ project nào có nhiều replica mới cần
> - Traffic NPM → backend luôn là HTTP plain (nội mạng) — không setup SSL 2 lần
> - NPM không biết bên trong project có bao nhiêu replica — đó là việc của Project Nginx/Swarm
