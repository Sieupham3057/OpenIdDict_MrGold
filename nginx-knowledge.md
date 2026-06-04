# Nginx Knowledge — Technical Leader Guide (Thực Hành Từng Bước)

## Mục lục

- [Phần 1 — Nginx là gì, hoạt động ra sao](#phần-1--nginx-là-gì-hoạt-động-ra-sao)
- [Phần 2 — Nginx vs Nginx Proxy Manager](#phần-2--nginx-vs-nginx-proxy-manager)
- [Phần 3 — Cài Nginx thuần trên Linux (step by step)](#phần-3--cài-nginx-thuần-trên-linux-step-by-step)
- [Phần 4 — Config production từng dòng giải thích](#phần-4--config-production-từng-dòng-giải-thích)
- [Phần 5 — SSL/TLS với Let's Encrypt (step by step)](#phần-5--ssltls-với-lets-encrypt-step-by-step)
- [Phần 6 — Rate Limiting thực hành](#phần-6--rate-limiting-thực-hành)
- [Phần 7 — Monitoring thực hành](#phần-7--monitoring-thực-hành)
- [Phần 8 — Security WAF + Fail2ban](#phần-8--security-waf--fail2ban)
- [Phần 9 — Docker Nginx vs thuần Linux so sánh](#phần-9--docker-nginx-vs-thuần-linux-so-sánh)
- [Phần 10 — Checklist và quyết định cuối cùng](#phần-10--checklist-và-quyết-định-cuối-cùng)

---

## Phần 1 — Nginx là gì, hoạt động ra sao

### Tại sao Nginx mạnh hơn Apache ở tải cao?

**Apache** dùng mô hình **thread-per-request**:
```
Request 1 đến → Tạo Thread 1 (tốn ~8MB RAM)
Request 2 đến → Tạo Thread 2 (tốn ~8MB RAM)
...
1000 request  → 1000 thread → 8GB RAM chỉ để xử lý connection
```

**Nginx** dùng mô hình **event-driven (non-blocking I/O)**:
```
1 Worker Process (1 thread)
    │
    ├── Nhận Request 1 → gửi lên backend → KHÔNG CHỜ → tiếp tục
    ├── Nhận Request 2 → gửi lên backend → KHÔNG CHỜ → tiếp tục
    ├── Nhận Request 3 → gửi lên backend → KHÔNG CHỜ → tiếp tục
    │
    └── Khi backend trả lời → gửi response về client tương ứng
```

Nginx KHÔNG chờ backend xong mới nhận request tiếp. Nó xử lý I/O theo kiểu bất đồng bộ (async). Đây là lý do 1 worker Nginx có thể xử lý 10.000+ connection đồng thời với RAM ít hơn nhiều.

### Số worker_processes nên đặt bao nhiêu?

```
worker_processes auto;   ← auto = số CPU core của máy
```

Ví dụ máy 4 core → Nginx tạo 4 worker process. Mỗi worker độc lập, xử lý song song.

Tính tổng connection tối đa:
```
max_connections = worker_processes × worker_connections
Ví dụ: 4 × 4096 = 16.384 connection đồng thời
```

### 6 vai trò của Nginx

| Vai trò | Ví dụ thực tế |
|---------|---------------|
| Web server | Serve Angular SPA từ `/var/www/html` |
| Reverse proxy | Đứng trước .NET API tại port 5000 |
| Load balancer | Phân phối giữa VM1:5000 và VM2:5000 |
| SSL terminator | Nginx nhận HTTPS:443, forward HTTP:5000 về backend |
| Cache | Cache response 200 của GET /api/products trong 60s |
| API Gateway | Rate limit + Auth + Routing theo path |

### Các thuật toán load balancing — khi nào dùng cái nào

```nginx
upstream api_backend {

    # ── OPTION 1: Round-robin (default, không cần khai báo) ─────────────────
    # Request luân phiên: 1→VM1, 2→VM2, 3→VM1, 4→VM2...
    # Dùng khi: API stateless, 2 server cùng cấu hình
    server 192.168.1.35:5000;
    server 192.168.1.36:5000;

    # ── OPTION 2: Least connections ──────────────────────────────────────────
    # Gửi request đến server đang có ÍT connection NHẤT
    # Dùng khi: login endpoint chậm hơn GET → server bị "ùn" không đều
    # KHUYẾN NGHỊ hơn round-robin cho API có endpoint thời gian khác nhau
    least_conn;
    server 192.168.1.35:5000;
    server 192.168.1.36:5000;

    # ── OPTION 3: IP Hash (sticky session) ───────────────────────────────────
    # Cùng IP → luôn vào cùng 1 server
    # Dùng khi: app lưu session trên server (legacy app, websocket)
    # KHÔNG dùng cho API stateless — mất đi lợi ích load balancing
    ip_hash;
    server 192.168.1.35:5000;
    server 192.168.1.36:5000;

    # ── OPTION 4: Weight ──────────────────────────────────────────────────────
    # Server mạnh hơn nhận nhiều request hơn
    # Dùng khi: VM1 có 8 core, VM2 có 4 core
    server 192.168.1.35:5000 weight=2;   # nhận 2/3 request (~67%)
    server 192.168.1.36:5000 weight=1;   # nhận 1/3 request (~33%)

    # ── OPTION 5: Backup ──────────────────────────────────────────────────────
    # VM2 chỉ nhận traffic khi VM1 DOWN
    # Dùng cho: DR (Disaster Recovery), hot standby
    server 192.168.1.35:5000;
    server 192.168.1.36:5000 backup;
}
```

---

## Phần 2 — Nginx vs Nginx Proxy Manager

### NPM là gì?

NPM (Nginx Proxy Manager) là web UI đặt trên nền Nginx. Bạn click form thay vì viết config text.

```
Nginx thuần                    Nginx Proxy Manager
─────────────────────          ──────────────────────────
Bạn viết nginx.conf            Bạn click form trên web UI
      ↓                                   ↓
nginx đọc file config          NPM tự generate nginx.conf
      ↓                                   ↓
nginx process chạy             nginx process chạy (giống nhau)
```

Performance **như nhau** — vì engine bên dưới đều là nginx.

### Bảng so sánh đầy đủ

| Tiêu chí | Nginx thuần | NPM | Ghi chú |
|----------|-------------|-----|---------|
| Cấu hình | Text file | Web UI | NPM generate config tự động |
| Học curve | Cao | Thấp | Cần biết cú pháp nginx |
| Linh hoạt | 100% | ~60% | NPM có "Advanced Config" tab nhưng hạn chế |
| SSL Let's Encrypt | Tự cài Certbot | 1 click | NPM tự renew cert |
| Performance | Baseline | Như nhau | Cùng nginx engine |
| Version control | Git diff rõ | Lưu trong SQLite | Nginx thuần tốt hơn cho team |
| CI/CD pipeline | Dễ — copy file | Khó — phải dùng API | Nginx thuần phù hợp DevOps |
| Multi-user | Không | Có | NPM có phân quyền |
| Custom module (WAF, Lua) | Dễ | Rất khó | Nginx thuần thắng tuyệt đối |
| Debug | Đọc log thẳng | Xem qua UI | Nginx thuần dễ hơn cho SRE |

### Khi nào dùng cái nào — quyết định thực tế

```
Bạn là:                              Dùng:
─────────────────────────────────────────────────────────
Developer học nginx                → Nginx thuần (hiểu cơ chế)
Homelab / self-hosted cá nhân      → NPM (nhanh, dễ)
SME không có DevOps engineer       → NPM
Startup có DevOps                  → Nginx thuần + Ansible/Terraform
Enterprise production              → Nginx thuần hoặc Nginx Plus
Cần ModSecurity WAF                → Nginx thuần (NPM không hỗ trợ tốt)
K8s cluster                        → Ingress-Nginx Controller (khác cả 2)
Microservices cần API Gateway      → Kong (trên OpenResty) hoặc Traefik
```

### Cài NPM (để tham khảo, không dùng trong lab này)

```bash
# Tạo thư mục
mkdir ~/npm && cd ~/npm

# Tạo docker-compose.yml
cat > docker-compose.yml << 'EOF'
services:
  npm:
    image: jc21/nginx-proxy-manager:latest
    container_name: nginx-proxy-manager
    ports:
      - "80:80"      # HTTP traffic
      - "443:443"    # HTTPS traffic
      - "81:81"      # NPM Admin web UI
    volumes:
      - ./data:/data
      - ./letsencrypt:/etc/letsencrypt
    restart: unless-stopped
EOF

docker compose up -d
```

**Test truy cập:**
```bash
# Truy cập UI tại: http://server-ip:81
# Login mặc định:
#   Email:    admin@example.com
#   Password: changeme
# → Đổi password ngay sau khi login lần đầu!
```

---

## Phần 3 — Cài Nginx thuần trên Linux (step by step)

> **Môi trường:** Ubuntu 22.04 LTS. Thực hiện trên VM3 (192.168.1.37) — máy load balancer trong lab.

### Bước 3.1 — Cài Nginx

```bash
# Cập nhật package list
sudo apt update

# Cài nginx
sudo apt install nginx -y

# Kiểm tra version vừa cài
nginx -v
# Kỳ vọng output: nginx version: nginx/1.18.0 (Ubuntu)
```

### Bước 3.2 — Kiểm tra nginx đang chạy chưa

```bash
# Xem trạng thái service
sudo systemctl status nginx

# Kỳ vọng output:
# ● nginx.service - A high performance web server...
#    Active: active (running) since...    ← phải thấy "active (running)"

# Test nginx có nghe port 80 không
curl http://localhost
# Kỳ vọng: trả về trang HTML "Welcome to nginx!"
```

### Bước 3.3 — Cấu trúc thư mục Nginx (quan trọng, cần biết)

```bash
ls /etc/nginx/
```

Output:
```
/etc/nginx/
├── nginx.conf              ← file config chính (global settings)
├── sites-available/        ← chứa các virtual host config (đang disable)
│   └── default             ← site mặc định
├── sites-enabled/          ← symlink đến sites-available (đang active)
│   └── default -> ../sites-available/default
├── conf.d/                 ← config bổ sung, tự động load
├── modules-available/      ← module có thể bật
├── modules-enabled/        ← module đang bật (symlink)
├── snippets/               ← đoạn config dùng chung (include)
└── mime.types              ← mapping file extension → content-type
```

**Quy trình làm việc chuẩn:**
```
1. Tạo file config trong sites-available/tên-site
2. Tạo symlink vào sites-enabled/ để enable
3. Test syntax: sudo nginx -t
4. Reload: sudo systemctl reload nginx
```

### Bước 3.4 — Tạo config load balancer

```bash
# Tạo file config mới trong sites-available
sudo nano /etc/nginx/sites-available/api-lb
```

Nội dung file (giải thích từng dòng ở Phần 4):
```nginx
upstream api_backend {
    least_conn;
    server 192.168.1.35:5000 max_fails=3 fail_timeout=30s;
    server 192.168.1.36:5000 max_fails=3 fail_timeout=30s;
    keepalive 32;
}

server {
    listen 80;
    server_name _;

    access_log /var/log/nginx/api-lb-access.log;
    error_log  /var/log/nginx/api-lb-error.log;

    location / {
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_connect_timeout 60s;
        proxy_read_timeout    60s;
    }
}
```

### Bước 3.5 — Enable site và test

```bash
# Disable site default (tránh conflict port 80)
sudo rm /etc/nginx/sites-enabled/default

# Enable site api-lb bằng symlink
sudo ln -s /etc/nginx/sites-available/api-lb /etc/nginx/sites-enabled/

# Test syntax — LUÔN làm bước này trước khi reload
sudo nginx -t

# Kỳ vọng output:
# nginx: the configuration file /etc/nginx/nginx.conf syntax is ok
# nginx: configuration file /etc/nginx/nginx.conf test is successful

# Reload nginx (không downtime, không ngắt connection đang có)
sudo systemctl reload nginx
```

**Nếu nginx -t báo lỗi:**
```bash
# Ví dụ lỗi syntax:
# nginx: [emerg] unexpected ";" in /etc/nginx/sites-available/api-lb:5
# → Dòng 5 có dấu ; sai chỗ

# Đọc log lỗi để debug thêm
sudo tail -20 /var/log/nginx/error.log
```

### Bước 3.6 — Test load balancer đang hoạt động

```bash
# Test từ VM3 — gọi đến API qua load balancer
curl http://localhost/health
# Kỳ vọng: {"status":"Healthy"}

# Test từ máy Windows của bạn
curl http://192.168.1.37/health
# Kỳ vọng: {"status":"Healthy"}

# Gọi 6 lần liên tiếp để thấy round-robin
for i in $(seq 1 6); do
    curl -s http://192.168.1.37/health
    echo " ← request $i"
done
```

**Song song, xem log ở VM1 và VM2:**
```bash
# Terminal trên VM1
sudo tail -f /var/log/nginx/access.log  # nếu VM1 dùng nginx thuần
# hoặc
docker logs api --follow                # nếu VM1 dùng Docker

# Terminal trên VM2
docker logs api --follow
```
Cả 2 terminal đều thấy request → load balancer đang phân phối.

### Bước 3.7 — Quản lý service Nginx hàng ngày

```bash
# Xem trạng thái
sudo systemctl status nginx

# Start (nếu chưa chạy)
sudo systemctl start nginx

# Stop
sudo systemctl stop nginx

# Restart (ngắt kết nối đang có, áp dụng config mới)
sudo systemctl restart nginx

# Reload (KHÔNG ngắt kết nối, áp dụng config mới) ← dùng cái này
sudo systemctl reload nginx

# Enable auto-start khi reboot
sudo systemctl enable nginx

# Disable auto-start
sudo systemctl disable nginx

# Test config syntax (làm TRƯỚC khi reload)
sudo nginx -t
```

---

## Phần 4 — Config production từng dòng giải thích

### File /etc/nginx/nginx.conf — phần global

```nginx
# ── DÒNG 1: User chạy nginx worker ──────────────────────────────────────────
user nginx;
# nginx worker process chạy với user "nginx" (không phải root)
# BẮT BUỘC cho security — không bao giờ chạy nginx bằng root
# Kiểm tra: ps aux | grep nginx → thấy "nginx" chứ không phải "root"

# ── DÒNG 2: Số worker process ───────────────────────────────────────────────
worker_processes auto;
# auto = Nginx tự detect số CPU core và tạo đúng số worker
# Ví dụ: máy 4 core → tạo 4 worker process
# Kiểm tra số core: nproc hoặc cat /proc/cpuinfo | grep processor | wc -l

# ── DÒNG 3: Giới hạn file descriptors ───────────────────────────────────────
worker_rlimit_nofile 65535;
# Mỗi connection cần 2 file descriptor (1 từ client, 1 đến backend)
# 65535 = hỗ trợ ~32.000 connection đồng thời per worker
# Phải khớp với system limit: ulimit -n (thường mặc định 1024 — quá thấp)
# Cách tăng system limit:
#   sudo nano /etc/security/limits.conf
#   nginx soft nofile 65535
#   nginx hard nofile 65535

# ── DÒNG 4: Error log ────────────────────────────────────────────────────────
error_log /var/log/nginx/error.log warn;
# warn = log từ mức warn trở lên (warn, error, crit, alert, emerg)
# Các mức log: debug < info < notice < warn < error < crit < alert < emerg
# Production: dùng "warn" (không quá nhiều log, không bỏ sót lỗi quan trọng)
# Debug: đổi thành "debug" tạm thời để xem chi tiết (rất nhiều log)

# ── DÒNG 5: PID file ─────────────────────────────────────────────────────────
pid /var/run/nginx.pid;
# Lưu Process ID của nginx master vào file này
# Dùng để: kill -HUP $(cat /var/run/nginx.pid)  → reload config
# Systemd đọc file này để quản lý service

events {
    # ── Số connection per worker ─────────────────────────────────────────────
    worker_connections 4096;
    # Mỗi worker process xử lý tối đa 4096 connection đồng thời
    # Tổng max connection = worker_processes × worker_connections = 4 × 4096 = 16.384
    # Với lab 3 VM: 1024 là đủ. Production: 4096–65536

    # ── Event model ──────────────────────────────────────────────────────────
    use epoll;
    # epoll = event model tốt nhất trên Linux (kernel 2.6+)
    # Nginx thường tự chọn đúng, nhưng khai báo tường minh = đảm bảo
    # Các event model khác: select (cũ, chậm), poll (cũ), kqueue (macOS/BSD)

    # ── Multi-accept ─────────────────────────────────────────────────────────
    multi_accept on;
    # Worker accept TẤT CẢ connection mới trong 1 lần thay vì từng cái 1
    # Tốt hơn khi có traffic burst đột ngột
    # Mặc định: off
}
```

### Phần http {} — global HTTP settings

```nginx
http {

    # ── MIME types ───────────────────────────────────────────────────────────
    include /etc/nginx/mime.types;
    # File này map đuôi file → Content-Type header
    # Ví dụ: .js → application/javascript, .css → text/css
    # BẮT BUỘC — không có sẽ gửi sai Content-Type

    default_type application/octet-stream;
    # Content-Type mặc định nếu không match mime.types
    # octet-stream = binary file (browser sẽ download thay vì render)

    # ── Sendfile optimization ─────────────────────────────────────────────────
    sendfile on;
    # Kernel-level file transfer — bỏ qua user space copy
    # Tốt hơn khi serve static files (JS, CSS, image)
    # Với reverse proxy thuần (API) thì ít ảnh hưởng

    tcp_nopush on;
    # Gửi HTTP header và file đầu trong 1 packet (Nagle's algorithm)
    # Giảm số packet, tốt hơn cho throughput
    # Chỉ hoạt động khi sendfile on

    tcp_nodelay on;
    # Gửi data ngay lập tức, không chờ đủ packet
    # Tốt cho latency (API response nhỏ)
    # tcp_nopush và tcp_nodelay cùng on là OK — áp dụng ở giai đoạn khác nhau

    # ── Keepalive ─────────────────────────────────────────────────────────────
    keepalive_timeout 65;
    # Client giữ TCP connection open bao lâu (giây) để gửi request tiếp
    # 65s là chuẩn. Tăng lên nếu client gửi nhiều request liên tiếp

    # ── Security headers ──────────────────────────────────────────────────────
    server_tokens off;
    # MẶC ĐỊNH nginx gửi: Server: nginx/1.18.0 trong response header
    # server_tokens off → Server: nginx (ẩn version)
    # Tại sao: attacker biết version → tra CVE → tấn công lỗ hổng đã biết
    # BẮT BUỘC cho production

    add_header X-Frame-Options "SAMEORIGIN" always;
    # Ngăn trang của bạn bị nhúng trong <iframe> của site khác
    # SAMEORIGIN = chỉ cho phép iframe từ cùng domain
    # DENY = không cho phép iframe ở bất kỳ đâu (chặt hơn)
    # Bảo vệ: Clickjacking attack

    add_header X-Content-Type-Options "nosniff" always;
    # Ngăn browser "đoán" Content-Type (MIME sniffing)
    # Ví dụ: file text/plain có nội dung HTML → browser không render như HTML
    # Bảo vệ: MIME confusion attack, XSS qua file upload

    add_header X-XSS-Protection "1; mode=block" always;
    # Bật XSS filter built-in của browser cũ (IE, Chrome cũ)
    # mode=block = block trang khi phát hiện XSS (thay vì sanitize)
    # Lưu ý: Chrome/Firefox mới đã bỏ tính năng này — thay bằng CSP
    # Vẫn nên có cho backward compatibility

    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    # Kiểm soát thông tin Referer gửi khi click link
    # strict-origin-when-cross-origin:
    #   - Same origin: gửi full URL
    #   - Cross origin HTTPS→HTTPS: chỉ gửi origin (domain, không có path)
    #   - Cross origin HTTPS→HTTP: không gửi gì (tránh leak URL qua HTTP)
    # Bảo vệ: tránh leak URL nội bộ (URL có token, ID nhạy cảm)

    add_header Content-Security-Policy "default-src 'self'" always;
    # CSP = chính sách nguồn gốc nội dung
    # default-src 'self' = chỉ load resource từ cùng domain
    # Bảo vệ: XSS (script inject từ domain khác bị block)
    # LƯU Ý: "default-src 'self'" quá strict cho SPA/CDN
    # Với Angular SPA cần thêm: script-src 'self' 'unsafe-inline'
    # Test trước ở mode report-only:
    #   Content-Security-Policy-Report-Only "default-src 'self'"
```

### Phần upstream {} — khai báo backend

```nginx
    upstream api_backend {
        # ── Thuật toán load balancing ─────────────────────────────────────────
        least_conn;
        # Gửi request đến server có ÍT connection nhất
        # TỐT HƠN round-robin cho API có endpoint chậm/nhanh khác nhau
        # Xóa dòng này = round-robin (default)

        # ── Danh sách backend server ──────────────────────────────────────────
        server 192.168.1.35:5000 max_fails=3 fail_timeout=30s;
        # 192.168.1.35:5000 = IP:port của backend
        # max_fails=3       = sau 3 lần fail liên tiếp → đánh dấu server DOWN
        # fail_timeout=30s  = thời gian đánh dấu DOWN (sau 30s thử lại)
        # Nếu không có max_fails: nginx không tự loại server chết ra

        server 192.168.1.36:5000 max_fails=3 fail_timeout=30s;
        # Server thứ 2. Cùng logic như trên.

        # ── Keepalive đến upstream ────────────────────────────────────────────
        keepalive 32;
        # Giữ tối đa 32 idle connection đến mỗi upstream server
        # Tránh tốn chi phí mở TCP connection mới cho mỗi request
        # BẮT BUỘC phải có proxy_http_version 1.1 và proxy_set_header Connection ""
        # khi dùng keepalive (xem phần location bên dưới)
    }
```

### Phần server {} — virtual host

```nginx
    server {
        # ── Listen port ───────────────────────────────────────────────────────
        listen 80;
        # Lắng nghe IPv4 port 80
        # Để lắng nghe cả IPv6: listen [::]:80;
        # HTTPS: listen 443 ssl http2; (xem Phần 5)

        # ── Server name ───────────────────────────────────────────────────────
        server_name _;
        # _ = wildcard, nhận request từ bất kỳ hostname nào
        # Thay bằng tên thật: server_name api.yourdomain.com;
        # Nhiều domain: server_name api.yourdomain.com www.yourdomain.com;
        # Wildcard subdomain: server_name *.yourdomain.com;

        # ── Logging ───────────────────────────────────────────────────────────
        access_log /var/log/nginx/api-lb-access.log combined;
        # combined = format log chuẩn (IP, time, method, URL, status, bytes, referer, UA)
        # Tắt log: access_log off; (tốt cho performance nếu không cần log)
        # Custom format: xem Phần 7

        error_log /var/log/nginx/api-lb-error.log warn;
        # Log lỗi của server block này (thay vì global error_log)

        # ── Location block ────────────────────────────────────────────────────
        location / {
            # / = match TẤT CẢ path (fallback)
            # Các location khác nhau:
            #   location /api/          → match path bắt đầu bằng /api/
            #   location = /health      → match CHÍNH XÁC /health (nhanh nhất)
            #   location ~ \.php$       → regex match
            #   location ~* \.php$      → regex match, case-insensitive

            # ── Forward đến upstream ──────────────────────────────────────────
            proxy_pass http://api_backend;
            # Tên "api_backend" phải khớp với tên trong upstream {}

            # ── HTTP version cho upstream ─────────────────────────────────────
            proxy_http_version 1.1;
            # Bắt buộc là 1.1 khi dùng keepalive trong upstream
            # HTTP/1.0 không hỗ trợ keepalive (mỗi request = 1 TCP connection)

            # ── Connection header ─────────────────────────────────────────────
            proxy_set_header Connection "";
            # Xóa header Connection: close từ client
            # Cần thiết để keepalive đến upstream hoạt động
            # Không có dòng này → upstream sẽ đóng connection sau mỗi request

            # ── Truyền thông tin client về backend ────────────────────────────
            proxy_set_header Host              $host;
            # Truyền hostname mà client gọi (e.g., api.yourdomain.com)
            # Backend cần để biết request đến từ domain nào

            proxy_set_header X-Real-IP         $remote_addr;
            # IP thật của client → backend log được IP đúng
            # Không có dòng này → backend thấy IP của Nginx (192.168.1.37)
            # $remote_addr = IP của kết nối đến Nginx

            proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
            # Chuỗi IP: client-ip, proxy1-ip, proxy2-ip
            # $proxy_add_x_forwarded_for = thêm $remote_addr vào header cũ
            # Dùng khi có nhiều lớp proxy

            proxy_set_header X-Forwarded-Proto $scheme;
            # Truyền giao thức (http hoặc https) về backend
            # Backend dùng để biết client dùng HTTPS hay HTTP
            # Quan trọng khi backend tạo redirect URL

            # ── Timeout ───────────────────────────────────────────────────────
            proxy_connect_timeout 60s;
            # Thời gian chờ kết nối đến backend (TCP handshake)
            # Nếu backend không phản hồi trong 60s → 502 Bad Gateway

            proxy_send_timeout 60s;
            # Thời gian chờ gửi request lên backend
            # Áp dụng khi request body lớn (file upload)

            proxy_read_timeout 60s;
            # Thời gian chờ backend trả response
            # Tăng lên nếu có endpoint xử lý lâu (report, export)
            # Giảm xuống nếu muốn fail fast (3s cho health check)
        }
    }
```

### Phần server {} — status endpoint (internal only)

```nginx
    server {
        listen 8080;
        # Port riêng cho monitoring — KHÔNG expose ra internet
        # Firewall rule: chỉ cho 192.168.1.0/24 access port 8080

        location /nginx-status {
            stub_status on;
            # Bật module stub_status — hiển thị metrics cơ bản

            allow 127.0.0.1;          # localhost
            allow 192.168.1.0/24;     # internal network
            deny all;                 # block tất cả còn lại
            # BẮT BUỘC có allow/deny — nếu không ai cũng xem được metrics
        }
    }
```

**Test stub_status:**
```bash
curl http://192.168.1.37:8080/nginx-status

# Output:
# Active connections: 5
# server accepts handled requests
#  1234 1234 5678
# Reading: 0 Writing: 1 Waiting: 4

# Giải thích:
# Active connections: 5         → 5 connection đang mở
# accepts: 1234                 → tổng connection đã accept từ đầu
# handled: 1234                 → tổng connection đã xử lý (handled = accepts = không drop)
# requests: 5678                → tổng request (1 connection có thể gửi nhiều request)
# Reading: 0                    → đang đọc request header từ 0 client
# Writing: 1                    → đang gửi response cho 1 client
# Waiting: 4                    → 4 connection keepalive đang chờ request tiếp
```

---

## Phần 5 — SSL/TLS với Let's Encrypt (step by step)

> **Điều kiện:** Bạn có domain thật trỏ về IP của máy Nginx. Không dùng được với IP nội bộ 192.168.x.x.
> **Lab không domain thật:** Dùng self-signed cert (xem Bước 5.5).

### Bước 5.1 — Cài Certbot

```bash
# Cài certbot và plugin nginx
sudo apt install certbot python3-certbot-nginx -y

# Kiểm tra version
certbot --version
```

### Bước 5.2 — Mở firewall cho HTTPS

```bash
# Cho phép HTTPS (port 443)
sudo ufw allow 'Nginx Full'   # mở cả 80 và 443
# hoặc
sudo ufw allow 443/tcp

# Kiểm tra
sudo ufw status
```

### Bước 5.3 — Cấp cert từ Let's Encrypt

```bash
# Certbot tự cấu hình Nginx
sudo certbot --nginx -d api.yourdomain.com

# Certbot sẽ:
# 1. Tạo challenge file tại /var/www/html/.well-known/acme-challenge/
# 2. Let's Encrypt server gọi http://api.yourdomain.com/.well-known/... để verify
# 3. Nếu OK → cấp cert → lưu vào /etc/letsencrypt/live/api.yourdomain.com/
# 4. Tự sửa nginx.conf để thêm SSL config
```

**Kỳ vọng output:**
```
Congratulations! You have successfully enabled https://api.yourdomain.com
...
Your certificate and chain have been saved at:
/etc/letsencrypt/live/api.yourdomain.com/fullchain.pem
Your key file has been saved at:
/etc/letsencrypt/live/api.yourdomain.com/privkey.pem
```

### Bước 5.4 — Config HTTPS thủ công (hiểu từng dòng)

Sau khi có cert, config nginx.conf phần HTTPS:

```nginx
server {
    # ── HTTPS port ────────────────────────────────────────────────────────────
    listen 443 ssl http2;
    # 443  = HTTPS standard port
    # ssl  = bật SSL/TLS trên port này
    # http2 = bật HTTP/2 (nhanh hơn HTTP/1.1, multiplexing nhiều request/connection)

    server_name api.yourdomain.com;
    # PHẢI khớp với domain trong cert

    # ── Cert files ────────────────────────────────────────────────────────────
    ssl_certificate /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem;
    # fullchain.pem = cert của bạn + intermediate cert (chuỗi cert đầy đủ)
    # Phải dùng fullchain, không phải cert.pem — thiếu intermediate → browser báo lỗi

    ssl_certificate_key /etc/letsencrypt/live/api.yourdomain.com/privkey.pem;
    # Private key tương ứng với cert
    # BẢO MẬT: file này chỉ root đọc được, không bao giờ expose ra ngoài

    # ── TLS version ───────────────────────────────────────────────────────────
    ssl_protocols TLSv1.2 TLSv1.3;
    # Chỉ cho phép TLS 1.2 và 1.3
    # TLS 1.0 và 1.1 đã bị deprecated (CVE-2014-3566 POODLE, CVE-2011-3389 BEAST)
    # TLS 1.3 nhanh hơn (1-RTT handshake vs 2-RTT của 1.2)
    # BẮT BUỘC tắt TLS 1.0/1.1 cho PCI DSS compliance

    # ── Cipher suites ─────────────────────────────────────────────────────────
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384;
    # Danh sách thuật toán mã hóa được phép
    # ECDHE = Elliptic Curve Diffie-Hellman Ephemeral (Perfect Forward Secrecy)
    # RSA = xác thực server
    # AES128/256-GCM = mã hóa dữ liệu
    # SHA256/384 = hash
    # Thứ tự quan trọng: nginx dùng cipher đầu tiên mà cả 2 phía hỗ trợ
    # Tool check: https://www.ssllabs.com/ssltest/

    ssl_prefer_server_ciphers on;
    # Server ưu tiên cipher của mình hơn của client
    # Tốt khi server có cipher list tốt hơn client cũ

    # ── Session cache ─────────────────────────────────────────────────────────
    ssl_session_cache shared:SSL:10m;
    # Chia sẻ SSL session cache giữa tất cả worker (shared)
    # SSL = tên cache (tùy đặt)
    # 10m = 10MB (~40.000 session)
    # Dùng để resume TLS session mà không cần handshake lại → nhanh hơn

    ssl_session_timeout 10m;
    # Session được cache trong 10 phút
    # Client reconnect trong 10 phút → không cần TLS handshake lại

    # ── HSTS (HTTP Strict Transport Security) ────────────────────────────────
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    # Bảo browser nhớ "luôn dùng HTTPS cho domain này trong 1 năm"
    # max-age=31536000 = 365 ngày
    # includeSubDomains = áp dụng cho *.yourdomain.com luôn
    # Bảo vệ: SSL stripping attack (hacker downgrade HTTPS→HTTP)
    # CẢNH BÁO: chỉ thêm khi đã chắc chắn 100% domain dùng HTTPS
    # Nếu sau này tắt HTTPS → user không access được trong 1 năm!
}

# ── HTTP → HTTPS redirect ─────────────────────────────────────────────────────
server {
    listen 80;
    server_name api.yourdomain.com;

    # Redirect tất cả HTTP sang HTTPS
    return 301 https://$host$request_uri;
    # 301 = Permanent redirect (browser cache)
    # $host = domain trong request
    # $request_uri = path + query string
    # Ví dụ: http://api.yourdomain.com/api/users?page=1
    #       → https://api.yourdomain.com/api/users?page=1
}
```

### Bước 5.5 — Self-signed cert cho lab nội bộ (không cần domain)

```bash
# Tạo thư mục lưu cert
sudo mkdir -p /etc/nginx/ssl

# Tạo self-signed cert (hiệu lực 365 ngày)
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/nginx/ssl/nginx-selfsigned.key \
    -out /etc/nginx/ssl/nginx-selfsigned.crt \
    -subj "/C=VN/ST=HCM/L=HoChiMinh/O=Lab/CN=192.168.1.37"

# Giải thích các flag:
# -x509       = tạo cert thay vì CSR (Certificate Signing Request)
# -nodes      = không encrypt private key bằng password (nginx cần đọc khi start)
# -days 365   = cert valid 1 năm
# -newkey rsa:2048 = tạo RSA key 2048-bit mới
# -keyout     = lưu private key vào file này
# -out        = lưu certificate vào file này
# -subj       = thông tin cert (Country, State, City, Org, Common Name)
# CN=192.168.1.37 = dùng IP vì không có domain

# Kiểm tra cert vừa tạo
openssl x509 -in /etc/nginx/ssl/nginx-selfsigned.crt -text -noout | head -30
```

Config nginx dùng self-signed cert:
```nginx
server {
    listen 443 ssl;
    server_name 192.168.1.37;

    ssl_certificate     /etc/nginx/ssl/nginx-selfsigned.crt;
    ssl_certificate_key /etc/nginx/ssl/nginx-selfsigned.key;
    ssl_protocols       TLSv1.2 TLSv1.3;

    location / {
        proxy_pass http://api_backend;
    }
}
```

**Test SSL:**
```bash
# -k = bỏ qua verify cert (cần thiết cho self-signed)
curl -k https://192.168.1.37/health

# Xem thông tin cert
curl -k -v https://192.168.1.37/health 2>&1 | grep -A5 "Server certificate"
```

### Bước 5.6 — Tự động renew cert Let's Encrypt

```bash
# Certbot tự cài cron job khi cài. Kiểm tra:
sudo systemctl status certbot.timer
# hoặc
cat /etc/cron.d/certbot

# Test renew thủ công (dry-run = không thực sự renew)
sudo certbot renew --dry-run
# Kỳ vọng: "Congratulations, all simulated renewals succeeded"

# Cert hết hạn còn bao lâu:
sudo certbot certificates
```

---

## Phần 6 — Rate Limiting thực hành

### Bước 6.1 — Hiểu cơ chế rate limiting

Nginx dùng **Leaky Bucket** algorithm:

```
Imagine một cái xô có lỗ ở đáy:
- Nước vào (request) có thể vào nhanh (burst)
- Nước ra (xử lý) với tốc độ cố định (rate)
- Xô đầy (burst limit vượt) → nước tràn = request bị reject 429

rate=10r/s, burst=20:
    Request 1-20: vào ngay (dùng burst token)
    Request 21:   nếu burst đầy → 429 Too Many Requests
    Mỗi giây: thêm 10 token vào burst (tối đa 20)
```

### Bước 6.2 — Cấu hình rate limiting cơ bản

```nginx
http {
    # ── Khai báo zone (PHẢI đặt trong http{}, không đặt trong server{}) ───────

    limit_req_zone $binary_remote_addr zone=api_rate:10m rate=100r/s;
    # $binary_remote_addr = key (phân biệt theo IP client)
    #   Dùng $binary_remote_addr thay $remote_addr vì nhỏ hơn (4 bytes vs ~15 bytes)
    # zone=api_rate:10m
    #   api_rate = tên zone (tùy đặt)
    #   10m = 10MB shared memory lưu state (10MB ≈ 160.000 IP)
    # rate=100r/s = 100 request/giây per IP

    limit_req_zone $binary_remote_addr zone=login_rate:10m rate=5r/m;
    # rate=5r/m = 5 request/phút per IP cho login
    # 5r/m = chống brute force password (5 lần thử/phút)

    limit_req_zone $binary_remote_addr zone=register_rate:10m rate=3r/m;
    # Đăng ký tài khoản: rất ít người cần đăng ký nhiều lần

    server {
        location /api/ {
            limit_req zone=api_rate burst=50 nodelay;
            # zone=api_rate = áp dụng rule này
            # burst=50      = cho phép burst 50 request trước khi reject
            # nodelay        = reject NGAY khi burst đầy (không delay/queue)
            #
            # Không có nodelay → nginx queue request và xử lý từ từ
            # nodelay → reject ngay → client thấy 429 nhanh → better UX
            #
            # Không có burst → mỗi request phải đúng 1/rate khoảng cách
            # Ví dụ rate=10r/s → 2 request trong 0.1s → 1 cái 429
            # Với burst=50 → 50 request đồng thời vẫn OK

            limit_req_status 429;
            # HTTP status khi bị rate limit (default: 503)
            # 429 = Too Many Requests (đúng semantic hơn)

            proxy_pass http://api_backend;
        }

        location /connect/token {
            limit_req zone=login_rate burst=3 nodelay;
            limit_req_status 429;
            proxy_pass http://api_backend;
        }
    }
}
```

### Bước 6.3 — Test rate limiting

```bash
# Gửi 10 request liên tiếp nhanh nhất có thể
for i in $(seq 1 10); do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://192.168.1.37/connect/token)
    echo "Request $i: HTTP $STATUS"
done

# Kỳ vọng (nếu login_rate=5r/m, burst=3):
# Request 1: HTTP 200
# Request 2: HTTP 200
# Request 3: HTTP 200
# Request 4: HTTP 429  ← burst đã hết
# Request 5: HTTP 429
# ...
```

**Xem log khi bị rate limit:**
```bash
sudo tail -f /var/log/nginx/error.log | grep "limiting"

# Output khi bị rate limit:
# 2024/01/01 10:00:00 [warn] 1234#0: *567 limiting requests,
#   excess: 2.840 by zone "login_rate", client: 192.168.1.100,
#   server: _, request: "POST /connect/token HTTP/1.1"
```

### Bước 6.4 — Rate limit theo user thay vì IP

```nginx
# Rate limit theo JWT sub (user ID) thay vì IP
# Cần đọc JWT → dùng trong API Gateway (OpenResty/Kong)
# Với Nginx thuần: rate limit theo API key trong header

limit_req_zone $http_x_api_key zone=api_key_rate:10m rate=1000r/s;

server {
    location /api/ {
        limit_req zone=api_key_rate burst=200 nodelay;
        proxy_pass http://api_backend;
    }
}
```

---

## Phần 7 — Monitoring thực hành

### Bước 7.1 — Custom log format (ghi đủ thông tin để phân tích)

```nginx
http {
    log_format detailed
        '$remote_addr '           # IP client
        '- $remote_user '         # user nếu có basic auth (thường là -)
        '[$time_local] '          # timestamp: [01/Jan/2024:10:00:00 +0700]
        '"$request" '             # method + URL + protocol: "GET /api/users HTTP/1.1"
        '$status '                # HTTP status code: 200, 404, 500...
        '$body_bytes_sent '       # bytes gửi về client (không tính header)
        '"$http_referer" '        # URL trang trước (nếu có)
        '"$http_user_agent" '     # browser/client info: "curl/7.68.0"
        '$request_time '          # thời gian xử lý request (giây): 0.123
        '$upstream_response_time '# thời gian backend trả lời: 0.120
        '$upstream_addr '         # backend nào đã serve: 192.168.1.35:5000
        '$upstream_status';       # HTTP status từ backend

    access_log /var/log/nginx/access.log detailed;
}
```

**Ví dụ 1 dòng log:**
```
192.168.1.100 - - [01/Jan/2024:10:00:00 +0700] "POST /connect/token HTTP/1.1" 200 512 "-" "k6/0.46.0" 0.125 0.123 192.168.1.35:5000 200
```

Đọc log này:
- Client `192.168.1.100` gọi POST /connect/token
- Response 200, 512 bytes
- Nginx mất 0.125s, backend mất 0.123s (0.002s = Nginx overhead)
- Backend xử lý là VM1 (192.168.1.35:5000)

### Bước 7.2 — Phân tích log bằng command line

```bash
# ── Tìm IP tấn công / scraper ────────────────────────────────────────────────
awk '{print $1}' /var/log/nginx/access.log | sort | uniq -c | sort -rn | head -10
# uniq -c = đếm số lần xuất hiện
# sort -rn = sort theo số giảm dần
# Output:
# 1523 192.168.1.100    ← IP này gọi 1523 request → đáng nghi
#  234 192.168.1.50
#   12 192.168.1.1

# ── Top URL bị gọi nhiều ─────────────────────────────────────────────────────
awk '{print $7}' /var/log/nginx/access.log | sort | uniq -c | sort -rn | head -10
# $7 = URL field trong log format mặc định
# Hữu ích: biết endpoint nào bị gọi nhiều nhất → tối ưu/cache

# ── Request chậm trên 1 giây ─────────────────────────────────────────────────
awk '$NF > 1 {print $NF, $7, $1}' /var/log/nginx/access.log | sort -rn | head -20
# $NF = field cuối cùng (upstream_response_time nếu dùng detailed format)
# Hữu ích: tìm endpoint bottleneck

# ── Error rate (4xx và 5xx) ───────────────────────────────────────────────────
awk '{print $9}' /var/log/nginx/access.log | sort | uniq -c | sort -rn
# $9 = status code field
# Output:
#  8234 200
#   123 404
#    45 429   ← bị rate limit
#    12 502   ← backend down
#     3 500   ← backend error

# ── Request per minute (traffic spike) ───────────────────────────────────────
awk '{print $4}' /var/log/nginx/access.log | cut -c2-18 | uniq -c
# $4 = timestamp field [01/Jan/2024:10:00
# cut -c2-18 = bỏ dấu [ lấy đến phút
# Hữu ích: thấy spike traffic theo thời gian

# ── Lọc chỉ 5xx error ─────────────────────────────────────────────────────────
grep ' 5[0-9][0-9] ' /var/log/nginx/access.log | tail -20

# ── Theo dõi real-time ────────────────────────────────────────────────────────
sudo tail -f /var/log/nginx/access.log
```

### Bước 7.3 — Cài GoAccess (dashboard trực quan)

GoAccess là tool phân tích log mạnh nhất, chạy được trong terminal hoặc tạo HTML report.

```bash
# Cài GoAccess
sudo apt install goaccess -y

# Xem ngay trong terminal (dạng TUI)
sudo goaccess /var/log/nginx/access.log \
    --log-format=COMBINED \
    --real-time-html=no
# Điều hướng bằng phím mũi tên, q để thoát
# Thấy ngay: Unique visitors, Top URLs, Status codes, Top IPs

# Tạo HTML report tĩnh
sudo goaccess /var/log/nginx/access.log \
    --log-format=COMBINED \
    -o /var/www/html/nginx-report.html
# Mở browser tại http://192.168.1.37/nginx-report.html

# Real-time HTML dashboard (cập nhật liên tục)
sudo goaccess /var/log/nginx/access.log \
    --log-format=COMBINED \
    --real-time-html \
    --ws-url=ws://192.168.1.37:7890 \
    -o /var/www/html/report.html \
    --port=7890 &
# Mở http://192.168.1.37/report.html → dashboard cập nhật mỗi giây
```

**GoAccess hiển thị:**
```
Dashboard GoAccess:
┌─────────────────────────────────────────────────────────────┐
│ Unique visitors: 45   Requests: 12,456   Failed: 23        │
│ Bandwidth: 45.2MB     Log size: 12.3MB                     │
├─────────────────────────────────────────────────────────────┤
│ TOP REQUESTS                                                 │
│  5,234 /api/user/info                                       │
│  3,123 /connect/token                                       │
│    456 /health                                              │
├─────────────────────────────────────────────────────────────┤
│ TOP IPs                                                      │
│  8,234  192.168.1.100  (US)                                 │
│  2,123  192.168.1.50   (VN)                                 │
├─────────────────────────────────────────────────────────────┤
│ HTTP STATUS                                                  │
│  10,234  200 OK                                             │
│   1,234  404 Not Found                                      │
│      23  502 Bad Gateway    ← backend có vấn đề!           │
└─────────────────────────────────────────────────────────────┘
```

### Bước 7.4 — Prometheus nginx-exporter

Tích hợp Nginx metrics vào hệ thống Prometheus/Grafana đã có trong lab:

```bash
# Chạy nginx-prometheus-exporter trên VM3
docker run -d \
    --name nginx-exporter \
    --restart unless-stopped \
    -p 9113:9113 \
    nginx/nginx-prometheus-exporter:latest \
    --nginx.scrape-uri=http://192.168.1.37:8080/nginx-status

# Kiểm tra exporter đang chạy
curl http://192.168.1.37:9113/metrics | head -20

# Kỳ vọng thấy:
# nginx_connections_active 5
# nginx_connections_reading 0
# nginx_connections_waiting 4
# nginx_connections_writing 1
# nginx_http_requests_total 12345
```

**Thêm vào prometheus.yml trên VM1:**
```yaml
scrape_configs:
  - job_name: 'nginx-lb'
    static_configs:
      - targets: ['192.168.1.37:9113']
    relabel_configs:
      - target_label: instance
        replacement: 'nginx-lb-vm3'
```

**Reload Prometheus:**
```bash
curl -X POST http://192.168.1.35:9090/-/reload
```

**PromQL queries hữu ích cho Grafana:**
```promql
# Request rate (req/s)
rate(nginx_http_requests_total[1m])

# Active connections
nginx_connections_active

# Connection drop rate (accepts - handled)
rate(nginx_connections_accepted_total[1m]) - rate(nginx_connections_handled_total[1m])

# Error rate từ upstream (4xx/5xx)
sum(rate(nginx_http_requests_total{status=~"[45].."}[1m]))
  /
sum(rate(nginx_http_requests_total[1m]))
```

**Import Grafana dashboard:** Import ID `12708` → dashboard NGINX by nginxinc.

---

## Phần 8 — Security WAF + Fail2ban

### Bước 8.1 — Fail2ban (tự động block IP tấn công)

Fail2ban đọc log → phát hiện pattern tấn công → gọi iptables block IP.

```bash
# Cài Fail2ban
sudo apt install fail2ban -y

# Kiểm tra trạng thái
sudo systemctl status fail2ban
```

**Tạo config cho Nginx:**
```bash
sudo nano /etc/fail2ban/jail.d/nginx.conf
```

```ini
# /etc/fail2ban/jail.d/nginx.conf

[DEFAULT]
# Thông báo qua email khi ban (optional)
# destemail = your@email.com
# sendername = Fail2ban Alert
# mta = sendmail

# ── Chặn brute force login ────────────────────────────────────────────────────
[nginx-limit-req]
enabled   = true
filter    = nginx-limit-req
# filter = tên file pattern trong /etc/fail2ban/filter.d/
# nginx-limit-req.conf có sẵn sau khi cài fail2ban

logpath   = /var/log/nginx/error.log
# Đường dẫn log nginx để fail2ban theo dõi

maxretry  = 10
# Sau 10 lần trigger rate limit trong findtime → ban

findtime  = 60
# Đếm trong cửa sổ 60 giây

bantime   = 600
# Ban 10 phút (600 giây). Tăng lên 3600 cho production

# ── Chặn scanning/probing ─────────────────────────────────────────────────────
[nginx-botsearch]
enabled   = true
filter    = nginx-botsearch
logpath   = /var/log/nginx/access.log
maxretry  = 2
bantime   = 86400   # ban 24 giờ nếu scan

# ── Chặn 4xx flood ───────────────────────────────────────────────────────────
[nginx-http-auth]
enabled   = true
filter    = nginx-http-auth
logpath   = /var/log/nginx/error.log
maxretry  = 5
bantime   = 3600
```

```bash
# Khởi động lại fail2ban để đọc config mới
sudo systemctl restart fail2ban

# Kiểm tra các jail đang chạy
sudo fail2ban-client status

# Xem chi tiết 1 jail
sudo fail2ban-client status nginx-limit-req

# Output:
# Status for the jail: nginx-limit-req
# |- Filter
# |  |- Currently failed: 2
# |  |- Total failed: 45
# |  `- File list: /var/log/nginx/error.log
# `- Actions
#    |- Currently banned: 1          ← có 1 IP đang bị ban
#    |- Total banned: 3
#    `- Banned IP list: 192.168.1.100   ← IP đang bị ban

# Xem IP đang bị block bởi iptables
sudo iptables -L f2b-nginx-limit-req -n

# Unblock IP thủ công (ví dụ bạn tự block nhầm IP của mình)
sudo fail2ban-client set nginx-limit-req unbanip 192.168.1.100
```

**Test fail2ban hoạt động:**
```bash
# Gửi nhiều request thất bại (login sai) để trigger
for i in $(seq 1 20); do
    curl -s -o /dev/null http://192.168.1.37/connect/token \
        -d "grant_type=password&client_id=wrong&username=x&password=wrong"
done

# Kiểm tra fail2ban đã ban IP chưa
sudo fail2ban-client status nginx-limit-req
```

### Bước 8.2 — ModSecurity WAF (nâng cao)

ModSecurity là WAF chặn các tấn công trong request (SQLi, XSS, path traversal...).

> **Lưu ý:** ModSecurity cần compile vào Nginx hoặc cài nginx-extras. Lab đơn giản dùng Fail2ban là đủ. ModSecurity cho production thực sự.

```bash
# Cài ModSecurity module
sudo apt install libnginx-mod-security2 -y

# Kiểm tra module đã cài
ls /etc/nginx/modules-available/ | grep modsecurity
```

```bash
# Tạo config ModSecurity
sudo cp /etc/modsecurity/modsecurity.conf-recommended /etc/modsecurity/modsecurity.conf

# Ban đầu để DETECTION ONLY (không block, chỉ log)
sudo sed -i 's/SecRuleEngine DetectionOnly/SecRuleEngine On/' /etc/modsecurity/modsecurity.conf
# Khi thấy ít false positive → đổi thành On để thực sự block
```

```nginx
# nginx.conf — bật ModSecurity
load_module modules/ngx_http_modsecurity_module.so;

http {
    modsecurity on;
    modsecurity_rules_file /etc/modsecurity/modsecurity.conf;

    server {
        location / {
            modsecurity on;
            proxy_pass http://api_backend;
        }
    }
}
```

```bash
# Test ModSecurity block SQLi
curl "http://192.168.1.37/api/users?id=1' OR '1'='1"
# Kỳ vọng: 403 Forbidden (nếu SecRuleEngine On)

# Xem ModSecurity audit log
sudo tail -f /var/log/modsec_audit.log
```

### Bước 8.3 — GeoIP blocking

```bash
# Cài GeoIP2 module
sudo apt install libnginx-mod-http-geoip2 -y

# Tải GeoLite2 database (cần đăng ký free tại maxmind.com)
sudo mkdir -p /etc/nginx/geoip
cd /etc/nginx/geoip

# Sau khi tải file GeoLite2-Country.mmdb từ MaxMind
# wget https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-Country&...
```

```nginx
# nginx.conf
load_module modules/ngx_http_geoip2_module.so;

http {
    geoip2 /etc/nginx/geoip/GeoLite2-Country.mmdb {
        $geoip2_data_country_code country iso_code;
        # $geoip2_data_country_code = biến lưu country code (VN, US, CN...)
    }

    # Map country code → allowed (yes/no)
    map $geoip2_data_country_code $allowed_country {
        default no;    # mặc định block tất cả
        VN      yes;   # cho phép Việt Nam
        US      yes;   # cho phép Mỹ
        SG      yes;   # cho phép Singapore
    }

    server {
        location /api/ {
            # Block nếu không phải country được phép
            if ($allowed_country = no) {
                return 403 "Access denied";
            }
            proxy_pass http://api_backend;
        }
    }
}
```

```bash
# Test GeoIP (cần VPN để test từ country khác)
curl http://192.168.1.37/api/test
# Từ IP Việt Nam → 200 OK
# Từ IP bị block → 403 Forbidden

# Kiểm tra GeoIP detect đúng không
curl -H "X-Real-IP: 8.8.8.8" http://localhost/api/test  # US IP
```

---

## Phần 9 — Docker Nginx vs thuần Linux so sánh

### Bảng so sánh chi tiết

| Tiêu chí | Docker Nginx | Nginx thuần (systemd) | Khuyến nghị |
|----------|-------------|----------------------|-------------|
| Isolation | Container isolated | Trực tiếp host OS | Docker nếu nhiều service |
| Version | Pin image tag `nginx:1.25` | `apt upgrade` thay version | Docker kiểm soát tốt hơn |
| Deploy | `docker compose up` | `copy file + reload` | Ngang nhau |
| Config | Mount volume | `/etc/nginx/` trực tiếp | Thuần trực tiếp hơn |
| Log | `docker logs nginx-lb` | `/var/log/nginx/` | Thuần dễ đọc hơn |
| Performance | +~1ms overhead | Tốt nhất | Khác biệt không đáng kể |
| SSL Certbot | Phức tạp hơn | Tích hợp tự nhiên | Thuần dễ hơn |
| Module | Build Dockerfile riêng | `apt install nginx-extras` | Thuần dễ hơn |
| Debug | `docker exec -it nginx-lb sh` | Thẳng vào host | Thuần dễ hơn |
| Rollback | Đổi image tag | Khó | Docker tốt hơn |

### Khi nào dùng Docker Nginx

```bash
# Docker tốt khi:
# 1. CI/CD pipeline — build image → test → deploy
# 2. Nhiều môi trường cần giống nhau (dev/staging/prod)
# 3. Microservice — Nginx là 1 trong nhiều container
# 4. K8s — pod chạy nginx container
# 5. Lab của bạn hiện tại — dễ reset

# Ví dụ Dockerfile custom Nginx (nếu cần ModSecurity)
cat > Dockerfile << 'EOF'
FROM nginx:1.25-alpine
RUN apk add --no-cache \
    libmodsecurity \
    nginx-mod-http-modsecurity
COPY nginx.conf /etc/nginx/nginx.conf
COPY modsecurity.conf /etc/modsecurity/modsecurity.conf
EOF
```

### Khi nào dùng Nginx thuần

```bash
# Nginx thuần tốt khi:
# 1. VPS production không có container orchestration
# 2. Cần Certbot tự động renew Let's Encrypt
# 3. SysAdmin truyền thống, quen với systemd
# 4. Cần custom module không có sẵn trong Docker image
# 5. Debugging trực tiếp trên server

# Cài Nginx với extras (nhiều module hơn)
sudo apt install nginx-extras -y
# nginx-extras gồm: nginx + Lua + ModSecurity + GeoIP + nhiều module khác

# Xem module đã cài
nginx -V 2>&1 | tr -- - '\n' | grep 'module'
```

---

## Phần 10 — Checklist và quyết định cuối cùng

### Checklist cho lab scale (VM3 — Nginx Docker)

```
Setup cơ bản:
[ ] docker compose up -d (nginx container chạy)
[ ] curl http://192.168.1.37/health → {"status":"Healthy"}
[ ] curl http://192.168.1.37:8080/nginx-health → nginx ok
[ ] curl http://192.168.1.37:8080/nginx-status → metrics hiện ra

Verify load balancing:
[ ] Gọi 10 request → log VM1 và VM2 đều thấy request
[ ] Tắt API VM2 → nginx tự route 100% về VM1 (sau max_fails)
[ ] Bật lại VM2 → nginx tự thêm vào rotation

Monitoring:
[ ] nginx-prometheus-exporter chạy port 9113
[ ] Prometheus scrape 192.168.1.37:9113 → target UP
[ ] Grafana dashboard nginx import ID 12708
```

### Checklist cho Nginx thuần production

```
Cài đặt:
[ ] nginx -v → version đúng
[ ] systemctl status nginx → active (running)
[ ] nginx -t → syntax ok

Config:
[ ] server_tokens off → curl -I http://... → không thấy nginx version
[ ] Security headers → curl -I http://... → thấy X-Frame-Options etc.
[ ] SSL/TLS → curl https://... OK, http → redirect 301
[ ] TLS version → openssl s_client -connect domain:443 | grep Protocol
[ ] Rate limiting → test với loop curl

Security:
[ ] fail2ban running → systemctl status fail2ban
[ ] Nginx status endpoint chỉ cho internal IP
[ ] Log rotation → /etc/logrotate.d/nginx
[ ] ufw status → chỉ mở 80, 443, 22

Monitoring:
[ ] GoAccess đọc log OK
[ ] nginx-prometheus-exporter chạy
[ ] Prometheus target UP
[ ] Alert rule nếu nginx_connections_active > threshold
```

### Quyết định cho từng tình huống

```
Tình huống 1: Lab học nginx load balancer
→ Docker Nginx (như hiện tại trong answer_scale.md)
→ Config đơn giản, dễ reset

Tình huống 2: Production VPS 1 server
→ Nginx thuần + systemd + Certbot
→ ModSecurity + Fail2ban
→ Prometheus exporter + Grafana

Tình huống 3: SME, team không có DevOps
→ Nginx Proxy Manager (NPM) trong Docker
→ Dễ quản lý domain/SSL qua web UI

Tình huống 4: Startup có nhiều service
→ Docker Compose hoặc K8s
→ Nginx thuần trong Docker cho gateway
→ Hoặc Traefik (auto-discovery service)

Tình huống 5: Enterprise microservices
→ Ingress-Nginx trên K8s
→ Hoặc Kong API Gateway
→ Service mesh (Istio/Linkerd) cho inter-service

Tình huống 6: Cần bảo vệ mạnh (OWASP Top 10)
→ Nginx + ModSecurity + OWASP CRS
→ Cloudflare WAF (trả phí, dễ hơn)
→ AWS WAF (nếu trên AWS)
```

---

## Appendix — Lệnh debug thường gặp

```bash
# ── Nginx không start ────────────────────────────────────────────────────────
sudo nginx -t                        # check syntax
sudo journalctl -xe | grep nginx     # xem systemd log chi tiết
sudo cat /var/log/nginx/error.log    # xem nginx error log

# ── Port 80 đang bị dùng bởi process khác ───────────────────────────────────
sudo ss -tlnp | grep :80             # xem process nào đang dùng port 80
sudo fuser -k 80/tcp                 # kill process đang dùng port 80 (cẩn thận)

# ── Backend trả 502 Bad Gateway ──────────────────────────────────────────────
# Kiểm tra backend còn sống không
curl http://192.168.1.35:5000/health
curl http://192.168.1.36:5000/health

# Xem nginx error log
sudo tail -50 /var/log/nginx/error.log | grep "connect() failed"
# "connect() failed (111: Connection refused)" → backend không chạy
# "connect() failed (110: Connection timed out)" → backend chạy nhưng không phản hồi

# ── Nginx trả 504 Gateway Timeout ────────────────────────────────────────────
# Backend chạy nhưng xử lý quá lâu
# Tăng proxy_read_timeout trong nginx config

# ── Reload config không có downtime ──────────────────────────────────────────
sudo nginx -t && sudo systemctl reload nginx
# Ghép 2 lệnh: chỉ reload nếu syntax OK

# ── Xem config đang active (sau khi nginx merge tất cả include) ───────────────
sudo nginx -T
# In ra full config đã được nginx parse (hữu ích khi debug include phức tạp)

# ── Kiểm tra certificate hết hạn chưa ────────────────────────────────────────
echo | openssl s_client -connect api.yourdomain.com:443 2>/dev/null | \
    openssl x509 -noout -dates
# notAfter=Jan 1 10:00:00 2025 GMT ← ngày hết hạn

# ── Theo dõi connection real-time ────────────────────────────────────────────
watch -n 1 'curl -s http://192.168.1.37:8080/nginx-status'
```
