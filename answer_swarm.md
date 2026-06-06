# Hướng dẫn Scale Lab: Nginx + DB Replication + Docker Swarm + Private Registry + Monitoring

## Mục lục

- [Kiến trúc tổng quan](#kiến-trúc-tổng-quan)
- [Part 1 — Nginx Load Balancer (192.168.1.37)](#part-1--nginx-load-balancer-192168137)
  - [Giải quyết nhược điểm: Keepalived HA + Nginx Proxy Manager + SSL](#giải-quyết-nhược-điểm-nginx)
  - [Keepalived Virtual IP — chống Single PoF](#nhược-điểm-1--single-point-of-failure--keepalived--virtual-ip)
  - [Nginx Proxy Manager — Web UI quản lý](#nhược-điểm-2--không-có-ui--nginx-proxy-manager-npm)
  - [SSL Termination — Let's Encrypt và Self-Signed](#nhược-điểm-3--ssl-termination--2-cách)
- [Part 2 — Database Replication (192.168.1.38 / .39)](#part-2--database-replication-1921681381921681339)
  - [SQL Server AlwaysOn AG](#sql-server-alwayson-ag)
  - [PostgreSQL Streaming Replication](#postgresql-streaming-replication)
  - [So sánh SQL Server vs PostgreSQL](#so-sánh-sql-server-vs-postgresql-replication)
- [Part 3 — Docker Swarm (.40 / .41 / .42)](#part-3--docker-swarm-192168140-41-42)
  - [Khởi tạo Swarm](#bước-31--cài-docker-trên-cả-3-vm)
  - [Docker Secrets — bảo mật credentials](#docker-secrets--bảo-mật-credentials-trong-swarm)
  - [Deploy Stack với Secrets](#bước-37--deploy-stack-với-secrets)
  - [Scaling và Rolling Update](#bước-38--scaling-và-rolling-update)
  - [Drain Node và Monitoring Swarm](#bước-39--drain-và-maintain-node)
- [Part 4 — Private Registry (192.168.1.50)](#part-4--private-registry-1921681500)
  - [Cấp 1: Docker Registry cơ bản](#cấp-1--docker-registry-cơ-bản)
  - [Cấp 2: Registry + TLS + Basic Auth](#cấp-2--registry--tls--basic-auth)
  - [Cấp 3: Harbor — Enterprise Registry](#cấp-3--harbor-enterprise-grade-registry)
  - [Quy trình CI/CD với Registry + Swarm](#quy-trình-cicd-đầy-đủ-với-private-registry--swarm)
- [Part 5 — Monitoring Stack: Prometheus + Grafana + InfluxDB + k6 (192.168.1.51)](#part-5--monitoring-stack-prometheus--grafana--influxdb--k6-192168151)
  - [Có cần VM riêng không? 1 VM cho 10 dự án được không?](#trả-lời-câu-hỏi-thực-tế-trước)
  - [Kiến trúc tổng quan Monitoring](#kiến-trúc-tổng-quan-monitoring)
  - [MON.1 — Tạo VM 192.168.1.51](#bước-mon1--tạo-vm-và-cài-docker-trên-192168151)
  - [MON.2 — Cấu trúc thư mục](#bước-mon2--tạo-cấu-trúc-thư-mục)
  - [MON.3 — Cấu hình prometheus.yml](#bước-mon3--viết-prometheusyml-cấu-hình-scrape-targets)
  - [MON.5 — Grafana provisioning datasource](#bước-mon5--tạo-grafana-provisioning-datasource-tự-động)
  - [MON.6 — Docker Compose Monitoring Stack](#bước-mon6--tạo-docker-compose-cho-monitoring-stack)
  - [MON.7 — Node Exporter + cAdvisor trên tất cả VM](#bước-mon7--cài-node-exporter-và-cadvisor-trên-tất-cả-các-vm-còn-lại)
  - [MON.8 — Nginx Prometheus Exporter](#bước-mon8--cài-nginx-prometheus-exporter-trên-vm-37-và-43)
  - [MON.9–10 — Verify Prometheus + Import Grafana Dashboard](#bước-mon9--verify-prometheus-đang-scrape)
  - [MON.11 — Cấu hình InfluxDB cho k6](#bước-mon11--cấu-hình-influxdb-cho-k6)
  - [MON.12 — Chạy k6 với output InfluxDB](#bước-mon12--cài-k6-và-chạy-load-test-với-monitoring)
  - [MON.13 — Đọc và giải thích kết quả k6](#bước-mon13--đọc-kết-quả-và-giải-thích-metric-quan-trọng)
  - [MON.14 — Multi-project: 1 VM cho 10 dự án](#bước-mon14--multi-project-1-vm-monitoring-cho-10-dự-án)
  - [MON.15–16 — Production setup + Checklist](#bước-mon15--cập-nhật-docker-compose-cho-production-like-setup)
- [Part 6 — Alertmanager: Cấu hình và gửi thông báo (192.168.1.51)](#part-6--alertmanager-cấu-hình-và-gửi-thông-báo-1921681351)
  - [Alertmanager là gì và tại sao cần?](#alertmanager-là-gì-và-tại-sao-cần)
  - [AM.0 — Cập nhật prometheus.yml để kết nối Alertmanager](#bước-am0--cập-nhật-prometheusyml-để-kết-nối-alertmanager)
  - [AM.1 — Tạo cấu trúc thư mục](#bước-am1--tạo-cấu-trúc-thư-mục-alertmanager)
  - [AM.2 — Viết alertmanager.yml (global, routing, receivers)](#bước-am2--viết-alertmanageryml)
  - [AM.3 — Tạo Email Template tùy chỉnh](#bước-am3--tạo-email-template-tùy-chỉnh)
  - [AM.4 — Thêm Alertmanager vào docker-compose.yml](#bước-am4--thêm-alertmanager-vào-docker-composeyml)
  - [AM.5 — Verify Alertmanager hoạt động](#bước-am5--verify-alertmanager-hoạt-động)
  - [AM.6 — Quản lý Silence (tắt alert trong maintenance)](#bước-am6--quản-lý-silence-tắt-alert-trong-maintenance)
  - [AM.7 — Viết alerting rules (basic.yml + swarm.yml)](#bước-am7--viết-alerting-rules)
  - [AM.8 — Reload config không cần restart](#bước-am8--reload-config-không-cần-restart)
  - [Luồng end-to-end khi có sự cố](#luồng-hoạt-động-end-to-end-khi-có-sự-cố)
- [Part 7 — Thực hành Nginx Load Balancing (192.168.1.60)](#part-7--thực-hành-nginx-load-balancing-192168160--làm-quen-trước-khi-setup-vip)
  - [NX.1 — Tạo VM và cài Docker + Nginx](#bước-nx1--tạo-vm-192168160-và-cài-docker--nginx)
  - [NX.2 — Cấu hình Nginx cơ bản + Upstream](#bước-nx2--cấu-hình-nginx-cơ-bản--upstream)
  - [NX.3 — Test Load Balancing với curl, ab, wrk](#bước-nx3--test-load-balancing-với-curl-ab-và-wrk)
  - [NX.4 — Thử các thuật toán phân tải + passive health check](#bước-nx4--thử-các-thuật-toán-phân-tải)
  - [NX.5 — Advanced: Upload file lớn (client_max_body_size + timeout)](#nx5-advanced--upload-file-lớn-client_max_body_size--timeout)
  - [NX.6 — Advanced: Timeout tuning cho API chậm + WebSocket](#nx6-advanced--timeout-tuning-cho-api-chậm)
  - [NX.7 — Advanced: Rate Limiting (chặn brute-force + DDoS cơ bản)](#nx7-advanced--rate-limiting-chặn-ddos-cơ-bản)
  - [NX.8 — Advanced: Buffer Tuning](#nx8-advanced--buffer-tuning)
  - [NX.9 — Advanced: Gzip Compression](#nx9-advanced--gzip-compression)
  - [NX.10 — Advanced: Proxy Cache](#nx10-advanced--proxy-cache)
  - [NX.11 — Advanced: Security Headers + Connection Limits](#nx11-advanced--security-headers--connection-limits)
  - [NX.12 — Cheatsheet Tech Lead](#nx12--cheatsheet-tech-lead-nginx-directives-quan-trọng)
- [So sánh tổng hợp và khi nào dùng gì](#so-sánh-tổng-hợp)
- [Troubleshooting thường gặp](#troubleshooting-thường-gặp)

---

## Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────────┐
│                        TOÀN BỘ LAB                                  │
│                                                                      │
│   [Client / k6]                                                      │
│         │                                                            │
│         ▼                                                            │
│   ┌─────────────┐                                                    │
│   │ Nginx LB    │  192.168.1.37  ← load balancing                   │
│   └──────┬──────┘                                                    │
│          │ round-robin / least_conn                                  │
│    ┌─────┼─────┐                                                     │
│    ▼     ▼     ▼                                                     │
│   .40   .41   .42  ← Docker Swarm cluster (3 nodes)                 │
│    │     │     │                                                     │
│    └──┬──┘     │                                                     │
│       │ shared backend                                               │
│    ┌──┴──────────────┐                                               │
│    │  DB Layer        │                                              │
│    │  .38 Primary     │  SQL Server / PostgreSQL                     │
│    │  .39 Secondary   │  (replication)                               │
│    └──────────────────┘                                              │
│                                                                      │
│   192.168.1.50 ← Private Registry (image store cho Swarm)           │
└─────────────────────────────────────────────────────────────────────┘
```

| VM IP | Vai trò | Cài gì |
|---|---|---|
| 192.168.1.36 | Virtual IP (VIP) | Keepalived float — client dùng IP này |
| 192.168.1.37 | Nginx Master | Nginx + Keepalived |
| 192.168.1.43 | Nginx Backup | Nginx + Keepalived (tạo thêm VM) |
| 192.168.1.38 | DB Primary | SQL Server / PostgreSQL |
| 192.168.1.39 | DB Secondary/Replica | SQL Server / PostgreSQL |
| 192.168.1.40 | Swarm Manager | Docker |
| 192.168.1.41 | Swarm Worker 1 | Docker |
| 192.168.1.42 | Swarm Worker 2 | Docker |
| 192.168.1.50 | Private Registry | Docker Registry / Harbor |

> Tất cả VM dùng Ubuntu Server 22.04 LTS, cùng subnet 192.168.1.0/24.

---

## Part 1 — Nginx Load Balancer (192.168.1.37)

### Tại sao cần Nginx Load Balancer?

**Vấn đề:** Khi chạy nhiều instance API (Docker Swarm), client không biết gọi vào IP nào.

**Giải pháp:** Nginx đứng trước làm "cổng vào duy nhất" — client gọi 1 địa chỉ, Nginx phân phối.

```
Client → 192.168.1.37:80 (Nginx)
                │
        ┌───────┼───────┐
        ▼       ▼       ▼
      .40      .41     .42   ← Swarm nodes
```

**Ưu điểm:**
- Đơn giản, hiệu năng cao, cấu hình bằng text
- Health check tự động — tự loại backend chết
- Nhiều thuật toán phân tải
- Tách biệt với ứng dụng — không phụ thuộc framework

**Nhược điểm:**
- Single point of failure nếu không HA
- Không có UI quản lý (cần cấu hình file)
- SSL termination cần cấu hình thêm

---

### Giải quyết nhược điểm Nginx

#### Nhược điểm 1 — Single Point of Failure → Keepalived + Virtual IP

**Nguyên lý VRRP (Virtual Router Redundancy Protocol):**
- Chạy 2 Nginx trên 2 VM khác nhau
- 1 Virtual IP (VIP) "trôi nổi" giữa 2 máy — client chỉ biết VIP này
- Master giữ VIP và gửi heartbeat mỗi 1 giây
- Khi Master chết (không gửi heartbeat), Backup nhận VIP trong ~2 giây
- Toàn bộ quá trình transparent với client

```
[Client] → 192.168.1.36 (VIP — luôn alive)
                │
        ┌───────┴────────────┐
        ▼                    ▼
  [Nginx Master           [Nginx Backup
   192.168.1.37]           192.168.1.43]     ← tạo thêm VM này trong VMware
   VRRP priority=100       VRRP priority=90
   (đang giữ VIP)          (standby, monitor Master)

Khi Master .37 chết:
  → Backup .43 không nhận heartbeat từ Master
  → Backup tự promote lên MASTER, nhận VIP .36
  → Client tiếp tục gọi .36 không biết gì đã xảy ra
  → Downtime < 2 giây
```

**Bước KA.1 — Tạo VM Nginx Backup (192.168.1.43)**

Clone từ VM .37 trong VMware (Linked Clone để nhanh), sau đó đổi IP:

```bash
# Trên VM .43 sau khi clone
sudo hostnamectl set-hostname nginx-backup

# Đổi IP trong netplan
sudo nano /etc/netplan/00-installer-config.yaml
# Sửa address từ .37 thành .43

sudo netplan apply
ip a   # verify IP mới
```

**Bước KA.2 — Cài Docker + Nginx + Keepalived trên CẢ 2 VM (.37 và .43)**

Cả 2 VM phải chạy cùng một Nginx container để khi VIP chuyển sang Backup, nó xử lý được request giống hệt Master.

**Bước KA.2.1 — Cài Docker (chạy trên CẢ 2 VM)**

```bash
# Chạy lần lượt trên 192.168.1.37 VÀ 192.168.1.43
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
docker --version   # Kỳ vọng: Docker version 24.x hoặc cao hơn
```

**Bước KA.2.2 — Tạo cấu hình Nginx (chạy trên CẢ 2 VM)**

```bash
# Chạy trên 192.168.1.37 VÀ 192.168.1.43 — cùng cấu hình giống nhau
mkdir -p ~/nginx-lb
```

```bash
cat > ~/nginx-lb/nginx.conf << 'EOF'
# ════════════════════════════════════════════════════════════════════
# PHẦN 1 — WORKER (bắt buộc)
# ════════════════════════════════════════════════════════════════════

# Số process Nginx chạy song song để xử lý request.
# "auto" = tự detect số CPU core của VM.
# VM 1 CPU → 1 worker, VM 4 CPU → 4 worker.
worker_processes auto;

events {
    # [BẮT BUỘC] Mỗi worker xử lý tối đa bao nhiêu connection cùng lúc.
    # Tổng concurrent connections = worker_processes × worker_connections.
    # 1024 là đủ cho lab; production thường dùng 4096–65536.
    worker_connections 1024;

    # [TÙY CHỌN] Dùng epoll — model xử lý I/O hiệu quả nhất trên Linux.
    # Nginx trên Linux mặc định đã dùng epoll, ghi ra cho rõ ý định.
    use epoll;

    # [TÙY CHỌN] Cho phép 1 worker nhận nhiều connection trong 1 lần xử lý.
    # Tăng throughput khi có nhiều request đến cùng lúc.
    multi_accept on;
}

# ════════════════════════════════════════════════════════════════════
# PHẦN 2 — HTTP BLOCK (bắt buộc, bọc toàn bộ cấu hình web)
# ════════════════════════════════════════════════════════════════════
http {

    # [BẮT BUỘC] Cho Nginx biết đuôi file nào là loại content gì.
    # Ví dụ: .jpg → image/jpeg, .js → application/javascript.
    # Thiếu dòng này Nginx không biết trả Content-Type đúng cho browser.
    include       /etc/nginx/mime.types;

    # [BẮT BUỘC] Loại content mặc định khi không khớp mime.types nào.
    default_type  application/octet-stream;

    # [TÙY CHỌN] OS copy file thẳng từ kernel → socket, không qua user space.
    # Tăng tốc đáng kể khi serve file tĩnh (HTML, JS, ảnh...).
    # Với reverse proxy thuần thì lợi ích ít hơn nhưng vẫn nên bật.
    sendfile on;

    # [TÙY CHỌN] Gom nhiều packet nhỏ thành 1 packet lớn trước khi gửi.
    # Giảm số lần gọi send() → giảm overhead TCP header.
    tcp_nopush on;

    # [TÙY CHỌN] Gửi data ngay lập tức, không chờ gom packet (tắt Nagle algorithm).
    # Kết hợp tcp_nopush + tcp_nodelay: throughput tốt mà latency cũng thấp.
    tcp_nodelay on;

    # [TÙY CHỌN] Giữ connection HTTP keep-alive tối đa 65 giây.
    # Tránh phải mở TCP connection mới cho mỗi request của cùng 1 client.
    keepalive_timeout 65;

    # [TÙY CHỌN — nên bật] Ẩn version Nginx trong response header.
    # Mặc định Nginx trả: "Server: nginx/1.25.3" → hacker biết version để exploit.
    # Sau khi bật: chỉ trả "Server: nginx".
    server_tokens off;

    # ──────────────────────────────────────────────────────────────
    # LOGGING — ghi log request để debug và theo dõi load balancing
    # ──────────────────────────────────────────────────────────────

    # [TÙY CHỌN nhưng rất hữu ích] Định nghĩa format của dòng log.
    # Các biến quan trọng:
    #   $remote_addr      = IP của client gọi vào Nginx
    #   $time_local       = thời gian request
    #   $request          = "GET /api/users HTTP/1.1"
    #   $status           = HTTP status code trả về (200, 404, 500...)
    #   $upstream_addr    = IP của Swarm node nào thực sự xử lý request
    #                       → dùng để xem Nginx có phân phối đều không
    #   $request_time     = tổng thời gian từ lúc nhận request đến lúc trả response
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" upstream="$upstream_addr" rt=$request_time';

    # Ghi access log (mỗi request) theo format "main" vừa định nghĩa.
    access_log /var/log/nginx/access.log main;

    # Ghi error log, chỉ ghi từ mức "warn" trở lên (warn/error/crit/alert/emerg).
    error_log  /var/log/nginx/error.log warn;

    # ──────────────────────────────────────────────────────────────
    # UPSTREAM — danh sách backend server (BẮT BUỘC, phần cốt lõi)
    # ──────────────────────────────────────────────────────────────

    # Khai báo nhóm backend tên "app_backend".
    # Nginx sẽ phân phối request đến các server trong nhóm này.
    # Mặc định dùng round-robin: req1→.40, req2→.41, req3→.42, req4→.40...
    upstream app_backend {
        # 3 Swarm node — Nginx gửi đến port 80 của mỗi node.
        # Docker Swarm ingress tự routing đến đúng container bên trong.
        # weight=1: cả 3 ngang nhau, mỗi cái nhận 1/3 tổng request.
        # Nếu .40 mạnh hơn: đặt weight=2 → .40 nhận 2/4, .41 và .42 mỗi cái 1/4.
        server 192.168.1.40:80 weight=1;
        server 192.168.1.41:80 weight=1;
        server 192.168.1.42:80 weight=1;

        # [TÙY CHỌN] Giữ sẵn tối đa 32 connection TCP đến mỗi backend.
        # Tránh tốn thời gian TCP handshake mỗi lần có request mới.
        # Phải dùng kết hợp với proxy_http_version 1.1 bên dưới mới có tác dụng.
        keepalive 32;
    }

    # ──────────────────────────────────────────────────────────────
    # SERVER BLOCK PORT 80 — nhận request HTTP từ client (BẮT BUỘC)
    # ──────────────────────────────────────────────────────────────
    server {
        # Lắng nghe port 80 (HTTP chuẩn).
        listen 80;

        # Nhận request từ bất kỳ hostname hoặc IP nào.
        # Dù client gọi vào 192.168.1.37 hay 192.168.1.36 (VIP) đều nhận.
        server_name _;

        # [BẮT BUỘC khi dùng keepalive upstream] Dùng HTTP/1.1 để giao tiếp với backend.
        # HTTP/1.0 không hỗ trợ keep-alive → keepalive 32 ở trên vô tác dụng nếu thiếu dòng này.
        proxy_http_version 1.1;

        # [BẮT BUỘC khi dùng keepalive upstream] Xóa header "Connection: close" của HTTP/1.0.
        # Nếu không xóa, backend sẽ đóng connection sau mỗi request → keepalive không hoạt động.
        proxy_set_header Connection "";

        # Gửi hostname gốc lên backend (backend có thể cần để phân biệt virtual host).
        proxy_set_header Host              $host;

        # Gửi IP thật của client lên backend.
        # Không có dòng này → backend chỉ thấy IP của Nginx (.37), không biết client thật là ai.
        # Quan trọng cho: logging, rate limiting, geo-blocking trong ứng dụng.
        proxy_set_header X-Real-IP         $remote_addr;

        # Chuỗi proxy chain: nếu đã qua nhiều proxy thì thêm IP vào cuối chuỗi.
        # Ví dụ: "client_ip, proxy1_ip, nginx_ip".
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;

        # Cho backend biết client dùng http hay https để redirect đúng.
        proxy_set_header X-Forwarded-Proto $scheme;

        # [BẮT BUỘC] Chuyển toàn bộ request (mọi path bắt đầu bằng /) sang upstream.
        location / {
            proxy_pass http://app_backend;
        }

        # [TÙY CHỌN — cần cho Keepalived] Endpoint để Keepalived kiểm tra Nginx còn sống không.
        # Keepalived chạy script: curl http://localhost/nginx-health
        # Nếu trả về 200 → Nginx OK, giữ VIP.
        # Nếu không trả về → Nginx chết, chuyển VIP sang Backup.
        location /nginx-health {
            access_log off;              # không ghi log cho health check, tránh spam log
            return 200 "healthy\n";      # trả về 200 OK với body "healthy"
            add_header Content-Type text/plain;
        }
    }

    # ──────────────────────────────────────────────────────────────
    # SERVER BLOCK PORT 8080 — xem thống kê nội bộ (TÙY CHỌN)
    # Dùng để: debug thủ công + Prometheus scrape metric
    # Có thể bỏ toàn bộ block này nếu không cần monitoring
    # ──────────────────────────────────────────────────────────────
    server {
        listen 8080;

        # Trang thống kê built-in của Nginx (stub_status module).
        # Truy cập: curl http://192.168.1.37:8080/nginx-status
        # Kết quả trả về:
        #   Active connections: 5
        #   server accepts handled requests: 100 100 200
        #   Reading: 0 Writing: 1 Waiting: 4
        # Chỉ cho phép IP trong mạng lab (.0/24) xem, chặn IP ngoài.
        location /nginx-status {
            stub_status on;
            access_log off;
            allow 192.168.1.0/24;   # chỉ VM trong lab mới xem được
            deny all;               # chặn tất cả IP khác
        }

        # Health check endpoint cho port 8080 — Prometheus exporter dùng để verify Nginx up.
        location /nginx-health {
            access_log off;
            return 200 "ok\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF
```

```bash
cat > ~/nginx-lb/docker-compose.yml << 'EOF'
services:
  nginx:
    # Image Nginx bản Alpine — nhỏ gọn (~40MB), đủ dùng cho reverse proxy.
    image: nginx:1.25-alpine
    # Đặt tên cố định cho container. Keepalived dùng tên này để check:
    #   docker inspect --format='{{.State.Running}}' nginx-lb
    container_name: nginx-lb
    ports:
      - "80:80"       # HTTP: ánh xạ port 80 VM → port 80 container
      - "8080:8080"   # Status/health: kiểm tra nội bộ và Prometheus scrape
    volumes:
      # Mount file nginx.conf từ VM vào container, :ro = read-only.
      # Khi sửa nginx.conf trên VM → reload không downtime:
      #   docker exec nginx-lb nginx -s reload
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      # Lưu log ra volume riêng để không mất khi restart container.
      - nginx-logs:/var/log/nginx
    # Tự restart nếu crash, trừ khi bị stop thủ công (docker stop).
    restart: unless-stopped
    healthcheck:
      # Docker tự kiểm tra Nginx còn sống không.
      # wget gọi /nginx-health; trả 200 = healthy, không trả = fail.
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/nginx-health"]
      interval: 10s   # kiểm tra mỗi 10 giây
      timeout: 5s     # không trả lời trong 5s = fail 1 lần
      retries: 3      # fail 3 lần liên tiếp → container = unhealthy

volumes:
  # Volume tên nginx-logs — Docker tự tạo và quản lý, dữ liệu tồn tại độc lập.
  nginx-logs:
EOF
```

```bash
# Khởi động Nginx
cd ~/nginx-lb
docker compose up -d

# Verify Nginx đang chạy
docker ps
curl http://localhost:8080/nginx-health   # Kỳ vọng: ok
```

**Bước KA.2.3 — Cài Keepalived (chạy trên CẢ 2 VM)**

```bash
# Trên 192.168.1.37 VÀ 192.168.1.43
sudo apt update
sudo apt install -y keepalived
keepalived --version   # Kỳ vọng: Keepalived v2.x
```

**Bước KA.3 — Cấu hình Keepalived trên Master (192.168.1.37)**

```bash
sudo nano /etc/keepalived/keepalived.conf
```

<pre style="background:#282c34;color:#abb2bf;padding:16px;border-radius:6px;overflow-x:auto;font-size:0.875em;line-height:1.6;font-family:monospace;">
<span style="color:#61afef;"># /etc/keepalived/keepalived.conf — MASTER</span>

vrrp_script check_nginx {
    script "docker inspect --format='{{.State.Running}}' nginx-lb | grep -q true"
    interval 2        <span style="color:#61afef;"># kiểm tra mỗi 2 giây</span>
    weight -30        <span style="color:#61afef;"># nếu nginx chết → priority giảm 30 → Backup (90) thắng Master (100-30=70)</span>
    fall 2            <span style="color:#61afef;"># fail 2 lần liên tiếp mới tính là down</span>
    rise 2            <span style="color:#61afef;"># pass 2 lần liên tiếp mới tính là up lại</span>
}

vrrp_instance VI_NGINX {
    state MASTER
    interface ens33          <span style="color:#61afef;"># tên interface — kiểm tra: ip a | grep "^[0-9]"</span>
    virtual_router_id 51     <span style="color:#61afef;"># ID nhóm VRRP — phải giống nhau trên cả 2 node</span>
                             <span style="color:#61afef;"># Chọn số 1-255, không trùng với VRRP khác trong mạng</span>
    priority 100             <span style="color:#61afef;"># Master cao hơn Backup</span>

    advert_int 1             <span style="color:#61afef;"># gửi VRRP advertisement mỗi 1 giây</span>
    preempt_delay 10         <span style="color:#61afef;"># chờ 10s sau khi recover trước khi giành lại VIP</span>
                             <span style="color:#61afef;"># Tránh VIP nhảy liên tục khi Master vừa khởi động</span>

    authentication {
        auth_type PASS
        auth_pass Lab@VRRP2025   <span style="color:#61afef;"># password chung, phải giống ở 2 node</span>
    }

    virtual_ipaddress {
        192.168.1.36/24 dev ens33   <span style="color:#61afef;"># VIP — cùng subnet với .37 và .43</span>
    }

    track_script {
        check_nginx    <span style="color:#61afef;"># theo dõi trạng thái nginx, giảm priority nếu fail</span>
    }

    <span style="color:#61afef;"># Thông báo khi VIP thay đổi (tùy chọn)</span>
    notify_master "/bin/bash -c 'echo MASTER &gt; /tmp/keepalived-state'"
    notify_backup "/bin/bash -c 'echo BACKUP &gt; /tmp/keepalived-state'"
    notify_fault  "/bin/bash -c 'echo FAULT  &gt; /tmp/keepalived-state'"
}
</pre>

**Bước KA.4 — Cấu hình Keepalived trên Backup (192.168.1.43)**

```bash
sudo nano /etc/keepalived/keepalived.conf
```

<pre style="background:#282c34;color:#abb2bf;padding:16px;border-radius:6px;overflow-x:auto;font-size:0.875em;line-height:1.6;font-family:monospace;">
<span style="color:#61afef;"># /etc/keepalived/keepalived.conf — BACKUP (192.168.1.43)</span>
<span style="color:#61afef;"># Nội dung gần giống Master (.37), CHỈ KHÁC 2 dòng: state và priority.</span>

<span style="color:#61afef;"># Script kiểm tra Nginx container còn chạy không — giống hệt Master.</span>
vrrp_script check_nginx {
    script "docker inspect --format='{{.State.Running}}' nginx-lb | grep -q true"
    interval 2     <span style="color:#61afef;"># kiểm tra mỗi 2 giây</span>
    weight -30     <span style="color:#61afef;"># nếu nginx chết → priority giảm 30 (90-30=60) → thua Master (100) luôn</span>
    fall 2         <span style="color:#61afef;"># fail 2 lần liên tiếp mới tính là down</span>
    rise 2         <span style="color:#61afef;"># pass 2 lần liên tiếp mới tính là up lại</span>
}

vrrp_instance VI_NGINX {
    state BACKUP             <span style="color:#61afef;"># ← KHÁC Master: khởi động ở trạng thái BACKUP (chờ)</span>
    interface ens33          <span style="color:#61afef;"># tên network interface — kiểm tra bằng: ip a</span>
    virtual_router_id 51     <span style="color:#61afef;"># PHẢI giống Master (51) — để 2 node nhận ra nhau là 1 nhóm VRRP</span>
    priority 90              <span style="color:#61afef;"># ← KHÁC Master: thấp hơn (90 &lt; 100) → Master thắng khi cả 2 sống</span>
    advert_int 1             <span style="color:#61afef;"># gửi heartbeat mỗi 1 giây để phát hiện Master còn sống không</span>
    preempt_delay 10         <span style="color:#61afef;"># sau khi Master recover, chờ 10s rồi mới trả VIP về Master</span>
                             <span style="color:#61afef;"># tránh VIP nhảy liên tục khi Master vừa khởi động</span>

    authentication {
        auth_type PASS
        auth_pass Lab@VRRP2025   <span style="color:#61afef;"># PHẢI giống Master — để 2 node xác thực nhau qua mạng</span>
    }

    virtual_ipaddress {
        192.168.1.36/24 dev ens33   <span style="color:#61afef;"># VIP sẽ được gán vào đây khi Master chết</span>
    }

    track_script {
        check_nginx   <span style="color:#61afef;"># nếu nginx-lb container chết → priority tụt → không tranh VIP với Master</span>
    }

    <span style="color:#61afef;"># Ghi trạng thái ra file để debug dễ: cat /tmp/keepalived-state</span>
    notify_master "/bin/bash -c 'echo MASTER &gt; /tmp/keepalived-state'"
    notify_backup "/bin/bash -c 'echo BACKUP &gt; /tmp/keepalived-state'"
}
</pre>

**Bước KA.5 — Khởi động và verify**

```bash
# Trên CẢ 2 VM
sudo systemctl enable keepalived
sudo systemctl start keepalived
sudo systemctl status keepalived   # phải Active (running)

# Verify VIP nằm trên Master (.37)
ip addr show ens33 | grep "192.168.1.36"
# Kỳ vọng trên .37: thấy VIP
# Kỳ vọng trên .43: không thấy gì

# Test failover: dừng nginx trên Master
docker stop nginx-lb
sleep 5

# Trên .43 — VIP phải chuyển sang đây
ip addr show ens33 | grep "192.168.1.36"
# Kỳ vọng: thấy VIP đã chuyển sang .43

# Khởi động lại nginx trên .37 → VIP trở về .37 (preempt sau 10s)
docker start nginx-lb

# Xem log Keepalived
sudo journalctl -u keepalived -f
```

```bash
# Mở port VRRP trong firewall (protocol 112, không phải TCP/UDP)
sudo ufw allow proto vrrp from 192.168.1.0/24
sudo ufw reload
```

> **Lưu ý quan trọng:** Cả 2 VM Nginx phải có **cùng cấu hình nginx.conf** — khi Backup tiếp nhận VIP, nó phải xử lý được request giống Master. Dùng `rsync` hoặc git để đồng bộ config:
> ```bash
> # Chạy trên Master — đồng bộ config sang Backup
> rsync -avz ~/nginx-lb/ bank@192.168.1.43:~/nginx-lb/
> # Sau đó reload nginx trên Backup
> ssh bank@192.168.1.43 "docker exec nginx-lb nginx -s reload"
> ```

---

#### Nhược điểm 2 — Không có UI → Nginx Proxy Manager (NPM)

**Nginx Proxy Manager** = Nginx được bọc thêm Web UI đầy đủ. Thay thế hoàn toàn cho cấu hình file thủ công khi không cần custom nâng cao.

**Khi nào dùng NPM thay Nginx thuần:**
- Team không quen cú pháp Nginx config
- Muốn thêm/sửa proxy host mà không cần reload thủ công
- Cần Let's Encrypt tự động (1 click)
- Cần quản lý nhiều domain/subdomain

**Khi nào giữ Nginx thuần:**
- Cần custom nâng cao: Lua, complex header manipulation, rate limiting chi tiết
- CI/CD tự động update config từ code → file config phù hợp hơn UI
- Hiệu năng tối đa (NPM thêm overhead nhỏ)

```bash
# Trên 192.168.1.37 (thay thế docker-compose.yml cũ)
mkdir -p ~/npm
cat > ~/npm/docker-compose.yml << 'EOF'
services:
  npm:
    image: jc21/nginx-proxy-manager:latest
    container_name: npm
    ports:
      - "80:80"     # HTTP traffic
      - "443:443"   # HTTPS traffic
      - "81:81"     # Web UI của NPM
    environment:
      DB_SQLITE_FILE: "/data/database.sqlite"
      # Tắt IPv6 nếu VM không dùng
      DISABLE_IPV6: "true"
    volumes:
      - npm-data:/data
      - npm-letsencrypt:/etc/letsencrypt
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "/usr/bin/check-health"]
      interval: 10s
      timeout: 3s

volumes:
  npm-data:
  npm-letsencrypt:
EOF

cd ~/npm
docker compose up -d
```

```
Truy cập UI: http://192.168.1.37:81

Đăng nhập lần đầu:
  Email:    admin@example.com
  Password: changeme
→ Đổi ngay sau khi đăng nhập
```

**Thêm Proxy Host qua UI:**

```
Hosts → Proxy Hosts → Add Proxy Host

Tab Details:
  Domain Names:           api.lab.local   (hoặc để trống, dùng IP)
  Scheme:                 http
  Forward Hostname / IP:  192.168.1.40    ← Swarm Manager
  Forward Port:           5000
  ☑ Cache Assets
  ☑ Block Common Exploits
  ☑ Websockets Support    (nếu app dùng WebSocket)

Tab SSL: (nếu có domain và Let's Encrypt)
  SSL Certificate: Request a new SSL Certificate
  ☑ Force SSL
  ☑ HTTP/2 Support
  ☑ HSTS Enabled
  → Save → NPM tự gọi Let's Encrypt, tự renew

Tab Advanced: (nếu cần custom Nginx config)
  Custom Nginx Configuration:
    proxy_read_timeout 120s;
    add_header X-Custom-Header "lab";
```

**Load balancing qua NPM** — dùng tab Advanced để thêm upstream:

```nginx
# Dán vào ô "Custom Nginx Configuration" trong tab Advanced
upstream swarm_nodes {
    least_conn;
    server 192.168.1.40:5000;
    server 192.168.1.41:5000;
    server 192.168.1.42:5000;
    keepalive 32;
}
```

Sau đó đặt Forward Hostname = `swarm_nodes` (tên upstream vừa định nghĩa).

---

#### Nhược điểm 3 — SSL Termination → 2 cách

**SSL Termination là gì?** Nginx giải mã HTTPS tại đây, forward HTTP thuần sang backend. Backend không cần xử lý SSL — đơn giản hóa ứng dụng và giảm overhead.

```
[Client] ──HTTPS──▶ [Nginx .37] ──HTTP──▶ [Swarm :5000]
          (mã hóa)  (giải mã TLS)  (nội bộ, không cần mã hóa)
```

##### Cách A — Let's Encrypt (domain public, miễn phí, tự gia hạn)

**Yêu cầu:** Phải có domain thật trỏ về IP public (không dùng được với IP LAN 192.168.x.x).

Nếu dùng **Nginx Proxy Manager**: chỉ cần tick "Request new SSL Certificate" → NPM tự lo.

Nếu dùng **Nginx thuần** với Certbot:

```bash
# Cài Certbot trên VM .37 (không trong Docker)
sudo apt install -y certbot python3-certbot-nginx

# Tạm dừng Nginx để Certbot dùng port 80
docker stop nginx-lb

# Lấy cert (thay yourdomain.com bằng domain thật)
sudo certbot certonly --standalone \
  -d yourdomain.com \
  -d www.yourdomain.com \
  --email admin@yourdomain.com \
  --agree-tos \
  --no-eff-email

# Cert được lưu tại:
# /etc/letsencrypt/live/yourdomain.com/fullchain.pem
# /etc/letsencrypt/live/yourdomain.com/privkey.pem

# Khởi động lại Nginx
docker start nginx-lb
```

Cập nhật `nginx.conf` để dùng cert:

```nginx
server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate     /certs/fullchain.pem;
    ssl_certificate_key /certs/privkey.pem;

    # Chỉ dùng TLS 1.2 và 1.3 (vô hiệu TLS 1.0/1.1 đã lỗi thời)
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    # HSTS — buộc browser dùng HTTPS trong 1 năm
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # OCSP Stapling — giảm latency khi verify cert
    ssl_stapling on;
    ssl_stapling_verify on;
    ssl_trusted_certificate /certs/fullchain.pem;

    location / {
        proxy_pass http://app_backend;
    }
}

# Redirect HTTP → HTTPS
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$host$request_uri;
}
```

Mount cert vào container:

```yaml
# docker-compose.yml — thêm volume cert
volumes:
  - ./nginx.conf:/etc/nginx/nginx.conf:ro
  - /etc/letsencrypt/live/yourdomain.com:/certs:ro   # thêm dòng này
```

Auto-renew (Let's Encrypt hết hạn mỗi 90 ngày, Certbot tự gia hạn):

```bash
# Kiểm tra cron job tự gia hạn đã được tạo chưa
sudo systemctl status certbot.timer
# hoặc
sudo crontab -l | grep certbot

# Test gia hạn (dry run)
sudo certbot renew --dry-run

# Sau khi renew, cần reload Nginx để load cert mới
# Thêm hook vào Certbot:
sudo nano /etc/letsencrypt/renewal-hooks/post/reload-nginx.sh
```

```bash
#!/bin/bash
docker exec nginx-lb nginx -s reload
```

```bash
sudo chmod +x /etc/letsencrypt/renewal-hooks/post/reload-nginx.sh
```

##### Cách B — Self-Signed Certificate (lab nội bộ, IP LAN)

Dùng khi không có domain public — phù hợp với lab VMware.

```bash
# Tạo CA (Certificate Authority) của lab
mkdir -p ~/nginx-lb/certs
cd ~/nginx-lb/certs

# Bước 1: Tạo CA key và cert
openssl genrsa -out ca.key 4096
openssl req -new -x509 -days 3650 \
  -key ca.key \
  -out ca.crt \
  -subj "/CN=LabCA/O=Lab/C=VN"

# Bước 2: Tạo key cho Nginx
openssl genrsa -out nginx.key 2048

# Bước 3: Tạo CSR với Subject Alternative Names (bắt buộc từ Chrome 58+)
cat > san.cnf << 'EOF'
[req]
req_extensions = v3_req
distinguished_name = req_distinguished_name

[req_distinguished_name]

[v3_req]
subjectAltName = @alt_names

[alt_names]
IP.1 = 192.168.1.36      # VIP — địa chỉ client dùng
IP.2 = 192.168.1.37      # Master IP
IP.3 = 192.168.1.43      # Backup IP
DNS.1 = nginx.lab.local  # optional: nếu có DNS nội bộ
EOF

openssl req -new \
  -key nginx.key \
  -out nginx.csr \
  -subj "/CN=nginx.lab.local/O=Lab/C=VN" \
  -config san.cnf

# Bước 4: CA ký cert (hạn 10 năm cho lab)
openssl x509 -req -days 3650 \
  -in nginx.csr \
  -CA ca.crt \
  -CAkey ca.key \
  -CAcreateserial \
  -out nginx.crt \
  -extensions v3_req \
  -extfile san.cnf

# Verify cert
openssl verify -CAfile ca.crt nginx.crt
openssl x509 -in nginx.crt -text -noout | grep -A5 "Subject Alternative Name"
# Kỳ vọng: thấy IP:192.168.1.36, IP:192.168.1.37, IP:192.168.1.43
```

Cập nhật `nginx.conf` thêm HTTPS server block:

```nginx
# Thêm vào trong http { } của nginx.conf

server {
    listen 443 ssl http2;
    server_name _;

    ssl_certificate     /certs/nginx.crt;
    ssl_certificate_key /certs/nginx.key;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers off;

    # Session cache — tái sử dụng TLS session, giảm overhead handshake
    ssl_session_cache   shared:SSL:10m;
    ssl_session_timeout 10m;

    location / {
        proxy_pass http://app_backend;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /nginx-health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}

# HTTP redirect → HTTPS
server {
    listen 80;
    server_name _;
    return 301 https://$host$request_uri;
}
```

Cập nhật `docker-compose.yml` thêm port 443 và volume cert:

```yaml
services:
  nginx:
    image: nginx:1.25-alpine
    container_name: nginx-lb
    ports:
      - "80:80"
      - "443:443"     # thêm
      - "8080:8080"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/certs:ro    # thêm
      - nginx-logs:/var/log/nginx
    restart: unless-stopped
```

**Trust CA trên máy client** (để browser/curl không báo lỗi "untrusted"):

```bash
# Trên Ubuntu/Debian client
sudo cp ca.crt /usr/local/share/ca-certificates/lab-ca.crt
sudo update-ca-certificates

# Trên Windows (PowerShell admin)
Import-Certificate -FilePath "ca.crt" -CertStoreLocation Cert:\LocalMachine\Root

# Test
curl https://192.168.1.36/health   # không cần -k nữa
```

---

#### Tóm tắt giải pháp theo tình huống

| Nhược điểm | Tình huống | Giải pháp | VM cần thêm |
|---|---|---|---|
| Single PoF | Lab VMware | Keepalived VIP (.36) + Nginx Backup (.43) | 1 VM mới (.43) |
| Single PoF | Cloud | Managed LB (AWS ALB, Azure Load Balancer) | Không |
| Không có UI | Muốn quản lý dễ | Nginx Proxy Manager (NPM) | Không |
| Không có UI | CI/CD tự động | Giữ Nginx file config, dùng script reload | Không |
| SSL — domain public | Có domain thật | NPM + Let's Encrypt (1 click) | Không |
| SSL — lab nội bộ | IP LAN, không có domain | Self-signed CA + trust cert trên client | Không |

---

### Bước 1.1 — Cài Docker trên VM 192.168.1.37

```bash
# Trên 192.168.1.37
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
```

### Bước 1.2 — Tạo thư mục cấu hình

```bash
mkdir -p ~/nginx-lb/conf.d
cd ~/nginx-lb
```

### Bước 1.3 — Viết nginx.conf

```bash
cat > ~/nginx-lb/nginx.conf << 'EOF'
# /root/nginx-lb/nginx.conf

# ─── Worker ───────────────────────────────────────────────────────────────────
# auto = Nginx tự detect số CPU core
worker_processes auto;

events {
    # Số connection đồng thời mỗi worker process xử lý
    # worker_processes × worker_connections = tổng concurrent connections
    # 1024 phù hợp lab; production: 4096 - 65536
    worker_connections 1024;

    # epoll là I/O event model hiệu quả nhất trên Linux
    use epoll;

    # Cho phép worker accept nhiều connection trong 1 lần (tăng throughput)
    multi_accept on;
}

http {

    # ─── Cài đặt chung ──────────────────────────────────────────────────────
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;

    # sendfile: OS copy file trực tiếp kernel→socket, không qua user space
    # Tăng performance đáng kể khi serve static files
    sendfile on;

    # tcp_nopush: gom nhiều packet nhỏ thành 1 → giảm overhead TCP header
    tcp_nopush on;

    # tcp_nodelay: tắt Nagle algorithm → gửi ngay, không gom → giảm latency
    # Kết hợp tcp_nopush + tcp_nodelay cho cả throughput và latency tốt
    tcp_nodelay on;

    # Thời gian giữ connection keep-alive (giây)
    keepalive_timeout 65;

    # Tắt Nginx version trong header — bảo mật cơ bản
    server_tokens off;

    # ─── Logging ────────────────────────────────────────────────────────────
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for" '
                    'upstream="$upstream_addr" rt=$request_time '
                    'urt="$upstream_response_time"';

    access_log /var/log/nginx/access.log main;
    error_log  /var/log/nginx/error.log warn;

    # ─── Upstream: khai báo danh sách backend ───────────────────────────────
    #
    # Thuật toán load balancing:
    #
    #   (default) round-robin:
    #     Mỗi request → backend tiếp theo theo vòng tròn
    #     Dùng khi: tất cả backend cùng sức mạnh, request xử lý nhanh như nhau
    #
    #   least_conn:
    #     Request → backend ít connection active nhất
    #     Dùng khi: request có thời gian xử lý khác nhau (upload, long poll)
    #     Tránh tình huống 1 backend nhận toàn request chậm, 2 backend kia rảnh
    #
    #   ip_hash:
    #     Cùng IP client → cùng backend (sticky session)
    #     Dùng khi: ứng dụng có session state (KHÔNG stateless) — không recommend
    #     Vấn đề: 1 IP gateway tập đoàn → toàn bộ user vào 1 backend
    #
    #   random [two least_conn]:
    #     Random 2 backend, chọn cái ít connection hơn
    #     Hiệu quả nhất ở số backend lớn (>10)
    #
    upstream app_backend {
        # Bỏ comment 1 trong các dòng sau để chọn thuật toán:
        # least_conn;
        # ip_hash;
        # random two least_conn;

        # Swarm nodes — Nginx gửi đến port 80 của mỗi node
        # Docker Swarm tự routing đến container đúng (ingress network)
        server 192.168.1.40:80 weight=1;  # weight: backend mạnh hơn thì weight cao hơn
        server 192.168.1.41:80 weight=1;
        server 192.168.1.42:80 weight=1;

        # max_fails: số lần fail trước khi đánh dấu backend down
        # fail_timeout: khoảng thời gian tính fail (và thời gian không gửi đến backend down)
        # Ví dụ thêm health check passive:
        # server 192.168.1.40:80 max_fails=3 fail_timeout=30s;

        # Keepalive: giữ N persistent connection đến mỗi upstream server
        # Tránh overhead mở TCP connection mới cho mỗi request
        keepalive 32;
    }

    # ─── Server block chính: lắng nghe port 80 ──────────────────────────────
    server {
        listen 80;
        server_name _;  # _ = nhận tất cả hostname/IP

        # Timeout
        proxy_connect_timeout 10s;   # timeout khi kết nối đến backend
        proxy_send_timeout    60s;   # timeout gửi request đến backend
        proxy_read_timeout    60s;   # timeout chờ response từ backend

        # Gửi keepalive connection đến upstream (phải dùng HTTP/1.1)
        proxy_http_version 1.1;
        proxy_set_header Connection "";  # xóa header Connection (HTTP/1.0 style)

        # Headers để backend biết thông tin client thật
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Buffer: Nginx buffer response từ backend trước khi gửi cho client
        # Giúp release backend connection sớm hơn (backend không phải chờ client chậm)
        proxy_buffering on;
        proxy_buffer_size          4k;
        proxy_buffers              8 16k;
        proxy_busy_buffers_size    32k;

        # ─── Tất cả request → upstream ──────────────────────────────────────
        location / {
            proxy_pass http://app_backend;
        }

        # ─── Health check endpoint của chính Nginx ───────────────────────────
        location /nginx-health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }

    # ─── Server block phụ: status ────────────────────────────────────────────
    server {
        listen 8080;

        # Nginx stub_status: xem số connection, request đang xử lý
        location /nginx-status {
            stub_status on;
            access_log off;
            # Giới hạn chỉ internal mới xem được
            allow 192.168.1.0/24;
            deny all;
        }

        location /nginx-health {
            access_log off;
            return 200 "ok\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF
```

### Bước 1.4 — Tạo docker-compose.yml

```bash
cat > ~/nginx-lb/docker-compose.yml << 'EOF'
services:
  nginx:
    image: nginx:1.25-alpine
    container_name: nginx-lb
    ports:
      - "80:80"
      - "8080:8080"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - nginx-logs:/var/log/nginx
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/nginx-health"]
      interval: 10s
      timeout: 5s
      retries: 3

volumes:
  nginx-logs:
EOF
```

### Bước 1.5 — Khởi động Nginx

```bash
cd ~/nginx-lb
docker compose up -d

# Verify
docker ps
curl http://localhost:8080/nginx-health   # Kỳ vọng: ok
curl http://localhost:8080/nginx-status   # Xem connection stats
curl http://localhost/health              # Gọi qua vào backend (nếu Swarm đã chạy)
```

### Bước 1.6 — Xem log Nginx theo dõi phân phối

```bash
# Xem access log theo dõi upstream="..." để thấy request phân phối đến node nào
docker exec nginx-lb tail -f /var/log/nginx/access.log

# Reload config khi thay đổi (không downtime)
docker exec nginx-lb nginx -s reload

# Test config trước khi reload
docker exec nginx-lb nginx -t
```

### Giải thích: Round-robin vs Least-conn trong thực tế

```
Ví dụ: 3 requests đến cùng lúc

Round-robin:
  req1 → .40
  req2 → .41
  req3 → .42
  Vấn đề: Nếu req1 là upload 1GB (xử lý 30s), .40 bị nghẽn
          nhưng req4 vẫn có thể được gửi sang .40

Least_conn:
  req1 → .40 (0 conn → nhận, .40 có 1 conn)
  req2 → .41 (0 conn → nhận, .41 có 1 conn)
  req3 → .42 (0 conn → nhận, .42 có 1 conn)
  req4 → ? → chọn node ít conn nhất → .41 hoặc .42 nếu .40 vẫn bận
  → Đúng behavior hơn khi request có thời gian xử lý khác nhau
```

**Kết luận:** Với API stateless xử lý nhanh đồng đều → round-robin đủ dùng. Với API có upload/long-poll → dùng `least_conn`.

---

## Part 2 — Database Replication (192.168.1.38 / 192.168.1.39)

### Tại sao cần Database Replication?

**Vấn đề:** Khi API scale ngang (nhiều instance), DB trở thành cổ chai duy nhất:
- Mọi READ đều vào 1 server → CPU DB cao
- READ và WRITE tranh nhau → lock contention → latency tăng
- DB die → toàn bộ ứng dụng die

**Giải pháp — Replication (primary + replica):**
- Primary: nhận WRITE (INSERT/UPDATE/DELETE)
- Replica: nhận READ (SELECT) — data tự động sync từ Primary
- Kết quả: phân tải đọc/ghi lên 2 máy khác nhau

```
API instance 1 ─┐  WRITE → [Primary .38]
API instance 2 ─┤                │ replication stream
API instance 3 ─┘  READ  → [Replica .39] ← data tự sync
```

**Khi nào dùng:**
- CPU DB server > 60% liên tục
- API latency cao nhưng CPU API thấp (dấu hiệu đang chờ DB)
- Workload nặng về READ (>70% request là SELECT)
- Cần HA: replica làm failover khi Primary chết

---

### SQL Server AlwaysOn AG

#### Yêu cầu phiên bản

| Edition | AG | Readable Secondary | Ghi chú |
|---------|----|--------------------|---------|
| **Enterprise** | ✅ | ✅ | Production — có phí |
| **Developer** | ✅ | ✅ | **Dùng cho lab** — miễn phí, cấm production |
| **Standard** | ✅ Basic AG | ❌ | Chỉ failover, không đọc được |

> Lab này dùng Developer Edition (miễn phí). Image Docker: `mcr.microsoft.com/mssql/server:2022-latest` mặc định là Developer/Evaluation.

#### Cơ chế hoạt động

```
[Primary — vm-db1 .38]
  ├── Nhận WRITE từ API
  ├── Ghi transaction log (WAL)
  └── Gửi log stream qua TCP:5022 → Secondary

[Secondary — vm-db2 .39]
  ├── Nhận log stream từ Primary
  ├── Apply log liên tục (redo thread)
  └── Nhận READ query từ API (nếu là Readable Secondary)

2 chế độ đồng bộ:
  Synchronous: Primary chờ Secondary xác nhận trước khi commit
    → Zero data loss, nhưng thêm latency mỗi write (~1-5ms trên LAN)
    → Dùng khi 2 VM cùng datacenter / LAN nhanh

  Asynchronous: Primary commit ngay, gửi log sau
    → Có thể mất vài giây data nếu Primary crash
    → Dùng khi Secondary ở datacenter xa / WAN
```

#### Bước DB.SQL.1 — Tạo docker-compose trên CẢ 2 VM

**Trên VM_DB1 (192.168.1.38) — tạo file:**

```bash
mkdir -p ~/sqlserver-ag
cat > ~/sqlserver-ag/docker-compose.yml << 'EOF'
services:
  sqlserver:
    # Image SQL Server 2022 Developer Edition — miễn phí, đủ tính năng AG cho lab.
    # KHÔNG dùng cho production vì vi phạm license Microsoft.
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    # hostname quan trọng: SQL Server AlwaysOn dùng hostname để nhận diện node trong cluster.
    # Primary khai báo replica ON N'vm-db1' → phải khớp với hostname này.
    hostname: vm-db1
    ports:
      - "1433:1433"   # Port SQL Server chuẩn — client và API kết nối vào đây
      - "5022:5022"   # HADR mirroring endpoint — PHẢI mở, Primary và Secondary trao đổi
                      # transaction log qua port này. Không mở = AG không hoạt động.
    environment:
      - ACCEPT_EULA=Y                           # Bắt buộc: đồng ý license Microsoft
      - MSSQL_SA_PASSWORD=YourStrong@Passw0rd   # Password tài khoản sa (System Administrator)
                                                # Phải đủ mạnh: chữ hoa, chữ thường, số, ký tự đặc biệt
      - MSSQL_ENABLE_HADR=1                     # Bật tính năng Always On High Availability
                                                # Mặc định tắt; = 1 để AG hoạt động
      - MSSQL_AGENT_ENABLED=true                # Bật SQL Server Agent — cần cho một số tác vụ AG
    volumes:
      - sqlserver-data:/var/opt/mssql           # Data, log DB, config SQL Server — QUAN TRỌNG nhất
                                                # Mất volume này = mất toàn bộ database
      - sqlserver-certs:/var/opt/mssql/certs    # Lưu certificate dùng để xác thực giữa Primary và Secondary
      - sqlserver-backup:/var/opt/mssql/backup  # Lưu file backup .bak — AG yêu cầu ít nhất 1 full backup
    restart: unless-stopped
    healthcheck:
      # Chạy lệnh sqlcmd thực tế để kiểm tra SQL Server đã ready chưa.
      # SELECT 1 = query đơn giản nhất, nếu chạy được = SQL Server đang sống.
      # -C = trust server certificate (bỏ qua SSL verify trong môi trường lab)
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "SELECT 1" > /dev/null 2>&1
      interval: 15s      # kiểm tra mỗi 15 giây
      timeout: 10s       # chờ tối đa 10s mỗi lần
      retries: 10        # thử 10 lần — SQL Server khởi động chậm, cần nhiều lần
      start_period: 60s  # chờ 60s trước khi bắt đầu healthcheck (SQL Server cần ~30-60s để init)

volumes:
  sqlserver-data:    # lưu database files
  sqlserver-certs:   # lưu certificates cho AG authentication
  sqlserver-backup:  # lưu file backup
EOF

docker compose up -d
```

**Trên VM_DB2 (192.168.1.39) — CHỈ đổi hostname, tất cả còn lại giống hệt:**

```bash
mkdir -p ~/sqlserver-ag
cat > ~/sqlserver-ag/docker-compose.yml << 'EOF'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    # ← CHỈ DÒNG NÀY KHÁC: vm-db2 thay vì vm-db1
    # Primary sẽ khai báo: ADD REPLICA ON N'vm-db2' → phải khớp hostname này
    hostname: vm-db2
    ports:
      - "1433:1433"
      - "5022:5022"
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_ENABLE_HADR=1
      - MSSQL_AGENT_ENABLED=true
    volumes:
      - sqlserver-data:/var/opt/mssql
      - sqlserver-certs:/var/opt/mssql/certs
      - sqlserver-backup:/var/opt/mssql/backup
    restart: unless-stopped
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "SELECT 1" > /dev/null 2>&1
      interval: 15s
      timeout: 10s
      retries: 10
      start_period: 60s

volumes:
  sqlserver-data:
  sqlserver-certs:
  sqlserver-backup:
EOF

docker compose up -d
```

```bash
# Chờ container healthy (~60s sau khi start)
docker ps
# Verify SQL Server đã sẵn sàng
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -Q "SELECT @@VERSION"
```

#### Bước DB.SQL.2 — Mở firewall cho port HADR

```bash
# Trên CẢ 2 VM
sudo ufw allow 1433/tcp
sudo ufw allow 5022/tcp
sudo ufw reload

# Test kết nối chéo
# Từ VM_DB1 test sang VM_DB2:
nc -zv 192.168.1.39 5022
# Từ VM_DB2 test sang VM_DB1:
nc -zv 192.168.1.38 5022
```

#### Bước DB.SQL.3 — Tạo database và cấu hình Primary (VM_DB1)

Kết nối vào `192.168.1.38,1433` bằng Azure Data Studio hoặc SSMS, chạy từng block:

```sql
-- ========== BLOCK 1: Tạo database ==========
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'AuthDemoDB')
BEGIN
    CREATE DATABASE AuthDemoDB;
END
GO

-- Bắt buộc FULL recovery mode để tham gia AG
-- SIMPLE mode không cho phép transaction log replication
ALTER DATABASE AuthDemoDB SET RECOVERY FULL;
GO
```

```sql
-- ========== BLOCK 2: Backup (AG yêu cầu ít nhất 1 full backup) ==========
BACKUP DATABASE AuthDemoDB
  TO DISK = '/var/opt/mssql/backup/AuthDemoDB_full.bak'
  WITH FORMAT, INIT, COMPRESSION, STATS = 10;
GO

BACKUP LOG AuthDemoDB
  TO DISK = '/var/opt/mssql/backup/AuthDemoDB_log.bak'
  WITH FORMAT, INIT;
GO
```

```sql
-- ========== BLOCK 3: Tạo Master Key (dùng để mã hóa cert) ==========
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MasterKey@AG2025!';
GO
```

```sql
-- ========== BLOCK 4: Tạo certificate để xác thực giữa 2 node ==========
-- AG dùng cert thay vì Windows authentication (vì chạy Docker Linux)
CREATE CERTIFICATE AG_Cert_Primary
  WITH SUBJECT = 'HADR Endpoint Auth Primary',
       EXPIRY_DATE = '2035-12-31';
GO

-- Export cert để copy sang Secondary
BACKUP CERTIFICATE AG_Cert_Primary
  TO FILE = '/var/opt/mssql/certs/ag_cert_primary.cer';
GO
```

```sql
-- ========== BLOCK 5: Tạo HADR Endpoint ==========
-- Port 5022: channel riêng để Primary và Secondary trao đổi log stream
CREATE ENDPOINT AG_Endpoint
  STATE = STARTED
  AS TCP (LISTENER_PORT = 5022)
  FOR DATABASE_MIRRORING (
    AUTHENTICATION = CERTIFICATE AG_Cert_Primary,
    ENCRYPTION     = REQUIRED ALGORITHM AES,
    ROLE           = ALL   -- ALL = vừa Primary vừa Secondary role
  );
GO
```

```sql
-- ========== BLOCK 6: Tạo Availability Group ==========
-- CLUSTER_TYPE = NONE: chạy không cần WSFC/Pacemaker (phù hợp Docker Linux)
-- SEEDING_MODE = AUTOMATIC: Primary tự copy DB sang Secondary, không cần restore thủ công
-- SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY): Secondary nhận READ query
CREATE AVAILABILITY GROUP [AuthDemoAG]
WITH (
    CLUSTER_TYPE                                  = NONE,
    DB_FAILOVER                                   = OFF,
    REQUIRED_SYNCHRONIZED_SECONDARIES_TO_COMMIT   = 0
)
FOR DATABASE [AuthDemoDB]
REPLICA ON N'vm-db1' WITH (
    ENDPOINT_URL      = N'TCP://192.168.1.38:5022',
    FAILOVER_MODE     = MANUAL,
    AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
    SEEDING_MODE      = AUTOMATIC,
    SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
);
GO
```

#### Bước DB.SQL.4 — Cấu hình Secondary (VM_DB2)

Kết nối vào `192.168.1.39,1433`:

```sql
-- ========== BLOCK 1: Master Key + Cert Secondary ==========
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MasterKey@AG2025!';
GO

CREATE CERTIFICATE AG_Cert_Secondary
  WITH SUBJECT = 'HADR Endpoint Auth Secondary',
       EXPIRY_DATE = '2035-12-31';
GO

BACKUP CERTIFICATE AG_Cert_Secondary
  TO FILE = '/var/opt/mssql/certs/ag_cert_secondary.cer';
GO
```

```sql
-- ========== BLOCK 2: HADR Endpoint trên Secondary ==========
CREATE ENDPOINT AG_Endpoint
  STATE = STARTED
  AS TCP (LISTENER_PORT = 5022)
  FOR DATABASE_MIRRORING (
    AUTHENTICATION = CERTIFICATE AG_Cert_Secondary,
    ENCRYPTION     = REQUIRED ALGORITHM AES,
    ROLE           = ALL
  );
GO
```

#### Bước DB.SQL.5 — Trao đổi certificate giữa 2 VM

```bash
# ── TRÊN VM_DB1 (192.168.1.38) ──
# Lấy cert ra khỏi container
docker cp sqlserver:/var/opt/mssql/certs/ag_cert_primary.cer ~/ag_cert_primary.cer

# Gửi cert primary sang VM_DB2
scp ~/ag_cert_primary.cer bank@192.168.1.39:~/

# ── TRÊN VM_DB2 (192.168.1.39) ──
# Lấy cert secondary ra
docker cp sqlserver:/var/opt/mssql/certs/ag_cert_secondary.cer ~/ag_cert_secondary.cer

# Gửi cert secondary sang VM_DB1
scp ~/ag_cert_secondary.cer bank@192.168.1.38:~/

# ── TRÊN VM_DB1: Copy cert secondary vào container ──
docker cp ~/ag_cert_secondary.cer sqlserver:/var/opt/mssql/certs/

# ── TRÊN VM_DB2: Copy cert primary vào container ──
docker cp ~/ag_cert_primary.cer sqlserver:/var/opt/mssql/certs/
```

#### Bước DB.SQL.6 — Import cert và grant quyền

**Trên PRIMARY (VM_DB1):**

```sql
-- Import cert của Secondary để Primary trust Secondary
CREATE CERTIFICATE AG_Cert_Secondary_Pub
  FROM FILE = '/var/opt/mssql/certs/ag_cert_secondary.cer';
GO

-- Tạo login + user + grant connect endpoint
CREATE LOGIN AG_Login_Secondary WITH PASSWORD = 'AGLogin@2025!';
GO
CREATE USER AG_User_Secondary FOR LOGIN AG_Login_Secondary;
GO
GRANT CONNECT ON ENDPOINT::AG_Endpoint TO AG_Login_Secondary;
GO

-- Thêm Secondary vào AG
ALTER AVAILABILITY GROUP [AuthDemoAG]
  ADD REPLICA ON N'vm-db2' WITH (
    ENDPOINT_URL      = N'TCP://192.168.1.39:5022',
    FAILOVER_MODE     = MANUAL,
    AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
    SEEDING_MODE      = AUTOMATIC,
    SECONDARY_ROLE (ALLOW_CONNECTIONS = READ_ONLY)
  );
GO
```

**Trên SECONDARY (VM_DB2):**

```sql
-- Import cert của Primary để Secondary trust Primary
CREATE CERTIFICATE AG_Cert_Primary_Pub
  FROM FILE = '/var/opt/mssql/certs/ag_cert_primary.cer';
GO

CREATE LOGIN AG_Login_Primary WITH PASSWORD = 'AGLogin@2025!';
GO
CREATE USER AG_User_Primary FOR LOGIN AG_Login_Primary;
GO
GRANT CONNECT ON ENDPOINT::AG_Endpoint TO AG_Login_Primary;
GO

-- JOIN vào AG (Primary sẽ tự copy database sang đây — Automatic Seeding)
ALTER AVAILABILITY GROUP [AuthDemoAG] JOIN WITH (CLUSTER_TYPE = NONE);
GO

-- Cho phép AG tự tạo database trên Secondary
ALTER AVAILABILITY GROUP [AuthDemoAG] GRANT CREATE ANY DATABASE;
GO
```

#### Bước DB.SQL.7 — Verify AG hoạt động

```sql
-- Chạy trên PRIMARY — kiểm tra replica state
SELECT
    ar.replica_server_name,
    ars.role_desc,
    ars.synchronization_health_desc,
    ars.connected_state_desc
FROM sys.dm_hadr_availability_replica_states ars
JOIN sys.availability_replicas ar ON ars.replica_id = ar.replica_id;
-- Kỳ vọng: vm-db1 PRIMARY HEALTHY CONNECTED
--           vm-db2 SECONDARY HEALTHY CONNECTED

-- Kiểm tra replication lag
SELECT
    ar.replica_server_name,
    drs.log_send_queue_size AS unsent_log_kb,
    drs.redo_queue_size     AS unapplied_log_kb,
    drs.synchronization_state_desc
FROM sys.dm_hadr_database_replica_states drs
JOIN sys.availability_replicas ar ON drs.replica_id = ar.replica_id;
-- unsent_log_kb và unapplied_log_kb gần 0 = replication theo kịp
```

```bash
# Test: write vào Primary, đọc từ Secondary
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S 192.168.1.38 -U sa -P "YourStrong@Passw0rd" -C \
  -Q "CREATE TABLE AuthDemoDB.dbo.ReplicationTest (id UNIQUEIDENTIFIER DEFAULT NEWID(), msg NVARCHAR(100));
      INSERT INTO AuthDemoDB.dbo.ReplicationTest(msg) VALUES ('hello from primary');"

# Đọc từ Secondary (kết quả nên có ngay trong vài ms)
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S 192.168.1.39 -U sa -P "YourStrong@Passw0rd" -C \
  -Q "SELECT * FROM AuthDemoDB.dbo.ReplicationTest"
# Kỳ vọng: thấy row 'hello from primary'

# Write vào Secondary → PHẢI fail
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S 192.168.1.39 -U sa -P "YourStrong@Passw0rd" -C \
  -Q "INSERT INTO AuthDemoDB.dbo.ReplicationTest(msg) VALUES ('should fail');"
# Kỳ vọng: ERROR - The target database is in a read-only state
```

#### Cấu hình .NET để dùng Read Replica (SQL Server)

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Write": "Server=192.168.1.38,1433;Database=AuthDemoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Max Pool Size=50;",
    "Read":  "Server=192.168.1.39,1433;Database=AuthDemoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Max Pool Size=50;ApplicationIntent=ReadOnly;"
  }
}
```

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(config.GetConnectionString("Write")));

builder.Services.AddDbContext<ReadDbContext>(opt =>
    opt.UseSqlServer(config.GetConnectionString("Read"))
       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
```

---

### PostgreSQL Streaming Replication

#### Tại sao PostgreSQL dễ hơn SQL Server cho replication?

| | SQL Server AG | PostgreSQL Streaming |
|---|---|---|
| License | Enterprise (~$30k/VM) | Miễn phí hoàn toàn |
| Cài đặt | Phức tạp (cert exchange, HADR endpoint) | Đơn giản (1 user + pg_basebackup) |
| Readable replica | Cần Enterprise | Mặc định có |
| Cluster manager | Cần WSFC hoặc Pacemaker | Không cần |
| Cascading replica | Không native | Có sẵn |

#### Cơ chế Streaming Replication

```
Primary ghi WAL (Write-Ahead Log) → WAL Sender process → network → WAL Receiver trên Replica → apply vào data files

WAL = transaction log của PostgreSQL
Replica luôn ở trạng thái "đang apply log" → luôn gần bằng Primary (lag thường < 1ms trên LAN)
```

#### Bước DB.PG.1 — Cấu hình Primary (VM_DB1: 192.168.1.38)

```bash
mkdir -p ~/postgres-primary/config
cd ~/postgres-primary
```

```bash
cat > ~/postgres-primary/config/postgresql.conf << 'EOF'
# ═══════════════════════════════════════════════════════════
# postgresql.conf — cấu hình PostgreSQL Primary
# File này override cấu hình mặc định bên trong container
# ═══════════════════════════════════════════════════════════

# [BẮT BUỘC cho replication] Lắng nghe trên tất cả network interface.
# Mặc định PostgreSQL chỉ lắng nghe localhost → replica không kết nối được.
listen_addresses = '*'

# [BẮT BUỘC cho replication] Mức độ chi tiết ghi vào WAL (Write-Ahead Log).
# minimal  = chỉ đủ để crash recovery — replica KHÔNG đọc được
# replica  = ghi đủ để replica streaming — DÙNG CÁI NÀY
# logical  = ghi thêm cho logical replication — nhiều hơn cần thiết
wal_level = replica

# Tối đa bao nhiêu replica được kết nối đồng thời để nhận WAL.
# 10 là đủ dư cho lab; production thường 3-5.
max_wal_senders = 10

# Giữ lại 256MB WAL file trên disk của Primary.
# Nếu replica bị lag quá nhiều (network down, restart...) mà WAL đã bị xóa
# → replica phải pg_basebackup lại từ đầu. Giữ đủ để replica tự catch up.
wal_keep_size = 256MB

# Primary chờ WAL được flush vào disk TRƯỚC khi báo commit thành công.
# on  = đảm bảo không mất data (zero data loss) — thêm ~1-2ms latency mỗi write
# off = nhanh hơn nhưng có thể mất vài transaction khi crash
synchronous_commit = on

# [BẮT BUỘC] Cho phép replica nhận READ query trong khi đang apply WAL.
# Thiếu dòng này → replica chỉ có thể dùng làm failover, không phục vụ READ được.
hot_standby = on

# ─── Performance ──────────────────────────────────────────
# Cache data trong RAM. Quy tắc: ~25% tổng RAM của VM.
# VM 1GB RAM → 256MB, VM 4GB RAM → 1GB.
shared_buffers = 256MB

# Gợi ý cho query planner biết tổng cache OS + shared_buffers có thể dùng.
# Không thực sự cấp phát RAM, chỉ ảnh hưởng query plan.
effective_cache_size = 1GB

# Tối đa bao nhiêu client kết nối đồng thời (app + replica + admin).
max_connections = 200
EOF
```

```bash
cat > ~/postgres-primary/config/pg_hba.conf << 'EOF'
# ═══════════════════════════════════════════════════════════
# pg_hba.conf — Host-Based Authentication
# Kiểm soát AI được phép kết nối vào PostgreSQL từ ĐÂU và bằng CÁCH NÀO.
# Các dòng được đọc từ trên xuống, khớp dòng đầu tiên là dùng luôn.
# ═══════════════════════════════════════════════════════════
# Cú pháp: TYPE  DATABASE  USER  ADDRESS  METHOD
#
# TYPE:     local (unix socket), host (TCP/IP), hostssl (chỉ SSL)
# DATABASE: tên database, "all" = tất cả
# USER:     tên user, "all" = tất cả
# ADDRESS:  IP hoặc subnet (chỉ dùng với host/hostssl)
# METHOD:   trust (không cần pass), scram-sha-256 (cần pass), reject (chặn)

# Kết nối qua unix socket (từ trong container) — dùng trust (không cần pass)
local   all         all                             trust

# Kết nối từ localhost qua TCP/IP — yêu cầu xác thực password
host    all         all         127.0.0.1/32        scram-sha-256

# Toàn bộ subnet 192.168.1.0/24 — cho phép API server kết nối vào
host    all         all         192.168.1.0/24      scram-sha-256

# Chỉ user 'replicator' được kết nối với mục đích replication.
# Phải tạo user này với quyền REPLICATION (bước tiếp theo).
# Subnet /24 cho phép bất kỳ VM nào trong lab làm replica.
host    replication replicator  192.168.1.0/24      scram-sha-256
EOF
```

```yaml
# ~/postgres-primary/docker-compose.yml
services:
  postgres:
    image: postgres:16              # PostgreSQL 16 — bản LTS mới nhất
    container_name: postgres-primary
    hostname: pg-primary            # hostname để replica nhận diện trong log
    ports:
      - "5432:5432"                 # port PostgreSQL chuẩn
    environment:
      - POSTGRES_USER=pgadmin       # tạo superuser tên pgadmin khi init lần đầu
      - POSTGRES_PASSWORD=Admin@Postgres2025
      - POSTGRES_DB=AuthDemoDB      # tạo database AuthDemoDB khi init lần đầu
      - PGDATA=/var/lib/postgresql/data  # thư mục lưu data trong container
    volumes:
      - pgdata:/var/lib/postgresql/data
      # Mount 2 file config từ VM vào container thay thế config mặc định.
      # Nếu không mount, PostgreSQL dùng config mặc định bên trong image
      # (không có replication, chỉ lắng nghe localhost).
      - ./config/postgresql.conf:/etc/postgresql/postgresql.conf
      - ./config/pg_hba.conf:/etc/postgresql/pg_hba.conf
    # Override lệnh start để chỉ định đường dẫn đến file config.
    # Không có dòng này, PostgreSQL không biết dùng file config nào.
    command: >
      postgres
        -c config_file=/etc/postgresql/postgresql.conf
        -c hba_file=/etc/postgresql/pg_hba.conf
    restart: unless-stopped
    healthcheck:
      # pg_isready: tool có sẵn trong image, kiểm tra PostgreSQL đã accept connection chưa.
      # Nhanh và nhẹ hơn chạy query thực.
      test: ["CMD", "pg_isready", "-U", "pgadmin", "-d", "AuthDemoDB"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  pgdata:   # lưu toàn bộ data PostgreSQL — quan trọng nhất, không được xóa
```

```bash
# Khởi động Primary
docker compose up -d

# Chờ healthy (~10s)
docker ps

# Tạo user replication
docker exec postgres-primary psql -U pgadmin -d postgres \
  -c "CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'Replicator@2025!';"

# Verify
docker exec postgres-primary psql -U pgadmin \
  -c "SELECT usename, replication FROM pg_user WHERE usename = 'replicator';"
# Kỳ vọng: replicator | t
```

#### Bước DB.PG.2 — Mở firewall

```bash
# Trên CẢ 2 VM
sudo ufw allow 5432/tcp
sudo ufw reload

# Test từ VM_DB2 → VM_DB1
nc -zv 192.168.1.38 5432   # Kỳ vọng: succeeded
```

#### Bước DB.PG.3 — Khởi tạo Replica bằng pg_basebackup (VM_DB2: 192.168.1.39)

```bash
# Tạo thư mục data — để TRỐNG, không docker compose up trước
mkdir -p ~/postgres-replica/data

# pg_basebackup: copy toàn bộ data directory từ Primary
# Flag -R tự tạo file standby.signal và ghi connection string vào postgresql.auto.conf
# PostgreSQL khi start thấy standby.signal → tự chạy ở chế độ standby (read-only + apply WAL)
docker run --rm \
  -e PGPASSWORD='Replicator@2025' \
  -v ~/postgres-replica/data:/var/lib/postgresql/data \
  postgres:16 \
  pg_basebackup \
    -h 192.168.1.38 \
    -p 5432 \
    -U replicator \
    -D /var/lib/postgresql/data \
    -Xs \   # -Xs: stream WAL trong lúc backup (đảm bảo không mất transaction nào)
    -P \    # -P: hiện progress
    -R      # -R: tạo standby.signal + primary_conninfo tự động

# Quá trình này sẽ copy toàn bộ data từ Primary (~vài phút tùy kích thước DB)

# Fix permissions: postgres container dùng uid 999 (debian)
sudo chown -R 999:999 ~/postgres-replica/data
```

#### Bước DB.PG.4 — Khởi động Replica (VM_DB2: 192.168.1.39)

```yaml
# ~/postgres-replica/docker-compose.yml
# ⚠ QUAN TRỌNG: Không khai báo POSTGRES_USER/DB ở đây.
# Lý do: data đã được copy đầy đủ từ Primary qua pg_basebackup (bước trên).
# Nếu khai báo POSTGRES_USER/DB, Docker sẽ cố gắng init lại database → xung đột với data có sẵn.
services:
  postgres:
    image: postgres:16
    container_name: postgres-replica
    hostname: pg-replica
    ports:
      - "5432:5432"   # cùng port với Primary — client dùng IP khác nhau để phân biệt
    environment:
      - PGDATA=/var/lib/postgresql/data
      # POSTGRES_PASSWORD ở đây chỉ để healthcheck tool pg_isready chạy được,
      # KHÔNG phải để tạo user mới. Password thật đã có trong data copy từ Primary.
      - POSTGRES_PASSWORD=Admin@Postgres2025
    volumes:
      # Mount thư mục data đã copy từ Primary (bước pg_basebackup).
      # Trong data này đã có file standby.signal → PostgreSQL tự chạy ở chế độ standby
      # (read-only + liên tục apply WAL từ Primary).
      - ./data:/var/lib/postgresql/data
    restart: unless-stopped
    healthcheck:
      # pg_isready kiểm tra Replica đã accept connection chưa.
      test: ["CMD", "pg_isready", "-U", "pgadmin", "-d", "AuthDemoDB"]
      interval: 10s
      timeout: 5s
      retries: 5
```

```bash
cd ~/postgres-replica
docker compose up -d

# Chờ container start (~30s)
docker ps
```

#### Bước DB.PG.5 — Verify Replication

```bash
# Kiểm tra Replica đang streaming từ Primary
docker exec postgres-replica psql -U pgadmin -d AuthDemoDB \
  -c "SELECT status, sender_host, received_lsn, latest_end_lsn FROM pg_stat_wal_receiver;"
# Kỳ vọng: status=streaming, sender_host=192.168.1.38

# Kiểm tra từ Primary: xem replica đang kết nối
docker exec postgres-primary psql -U pgadmin \
  -c "SELECT client_addr, state, write_lag, flush_lag, replay_lag FROM pg_stat_replication;"
# Kỳ vọng: client_addr=192.168.1.39, state=streaming, lag gần 0

# Test write Primary → đọc Replica
docker exec postgres-primary psql -U pgadmin -d AuthDemoDB \
  -c "CREATE TABLE IF NOT EXISTS repl_test (id SERIAL, msg TEXT, ts TIMESTAMPTZ DEFAULT NOW());
      INSERT INTO repl_test(msg) VALUES ('hello from primary');"

docker exec postgres-replica psql -U pgadmin -d AuthDemoDB \
  -c "SELECT * FROM repl_test;"
# Kỳ vọng: thấy row 'hello from primary' ngay lập tức

# Write vào Replica → PHẢI fail
docker exec postgres-replica psql -U pgadmin -d AuthDemoDB \
  -c "INSERT INTO repl_test(msg) VALUES ('should fail');"
# Kỳ vọng: ERROR: cannot execute INSERT in a read-only transaction
```

#### Cấu hình .NET dùng Read Replica (PostgreSQL + Npgsql)

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Write": "Host=192.168.1.38;Port=5432;Database=AuthDemoDB;Username=pgadmin;Password=Admin@Postgres2025;Maximum Pool Size=50;",
    "Read":  "Host=192.168.1.39;Port=5432;Database=AuthDemoDB;Username=pgadmin;Password=Admin@Postgres2025;Maximum Pool Size=50;Target Session Attributes=prefer-standby;"
  }
}
```

> `Target Session Attributes=prefer-standby`: Npgsql ưu tiên kết nối standby. Nếu standby down thì fallback về primary (đọc từ primary). An toàn hơn so với hardcode IP.

#### So sánh SQL Server vs PostgreSQL Replication

| | SQL Server AG | PostgreSQL Streaming |
|---|---|---|
| **Chi phí** | $30k+ / VM (Enterprise) | Miễn phí |
| **Độ phức tạp setup** | Cao (cert, endpoint, T-SQL nhiều bước) | Thấp (pg_basebackup là xong) |
| **Thêm replica** | Lặp lại bước cert exchange | Chỉ chạy pg_basebackup trỏ vào Primary |
| **Failover tự động** | Cần Pacemaker / WSFC | Cần Patroni / Repmgr |
| **Lag replication** | ~1ms trên LAN | ~1ms trên LAN |
| **Connection pooler** | Không bắt buộc | Bắt buộc dùng PgBouncer ở tải cao |
| **Dùng trong lab/startup** | Developer Edition (miễn phí, cấm production) | Production thoải mái |

---

## Part 3 — Docker Swarm (192.168.1.40 / .41 / .42)

### Docker Swarm là gì?

**Docker Swarm** = tính năng clustering có sẵn trong Docker — không cần cài thêm gì.

**Tại sao dùng Swarm thay vì chạy `docker compose` riêng lẻ trên mỗi VM?**

| Vấn đề khi không có Swarm | Giải pháp với Swarm |
|---|---|
| Deploy phải SSH từng VM | Deploy 1 lần từ Manager → tất cả node |
| Scale phải thủ công từng VM | `docker service scale` → tự phân phối |
| VM chết → service chết | Swarm tự restart service trên VM khác |
| Config/Secret phải copy từng VM | Lưu tập trung ở Swarm, tự phân phối |

**Kiến trúc Swarm:**

```
[Manager — 192.168.1.40]   ← điều phối, lưu state, nhận lệnh deploy
  │   (Raft consensus để đảm bảo consistency)
  ├── [Worker1 — 192.168.1.41]  ← chạy container thực tế
  └── [Worker2 — 192.168.1.42]  ← chạy container thực tế
```

**Số lượng Manager nên có:**
- 1 Manager: đủ cho lab
- 3 Manager: HA — chịu được 1 manager chết (cần quorum 2/3)
- 5 Manager: chịu được 2 manager chết

> Raft consensus: cần quá nửa manager còn sống mới hoạt động. 2 manager tệ hơn 1 manager (1 chết = mất quorum, không thể deploy).

---

### Bước 3.1 — Cài Docker trên cả 3 VM

```bash
# Chạy trên CẢ 3 VM: 192.168.1.40, .41, .42
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
```

### Bước 3.2 — Khởi tạo Swarm trên Manager

```bash
# Chỉ chạy trên 192.168.1.40 (Manager)
docker swarm init --advertise-addr 192.168.1.40
```

Output sẽ có dạng:
```
Swarm initialized: current node (xxx) is now a manager.

To add a worker to this swarm, run the following command:

    docker swarm join --token SWMTKN-1-xxxxxxxxxx-xxxxxxxxxx 192.168.1.40:2377
```

**Copy lệnh `docker swarm join` đó** — sẽ dùng ở bước tiếp theo.

```bash
# Lấy lại token nếu lỡ quên
docker swarm join-token worker   # token cho worker
docker swarm join-token manager  # token cho manager (thêm manager mới)
```

### Bước 3.3 — Thêm Worker vào Swarm

```bash
# Chạy trên 192.168.1.41 (Worker1)
docker swarm join --token SWMTKN-1-xxxxxxxxxx-xxxxxxxxxx 192.168.1.40:2377

# Chạy trên 192.168.1.42 (Worker2)
docker swarm join --token SWMTKN-1-xxxxxxxxxx-xxxxxxxxxx 192.168.1.40:2377
```

```bash
# Verify trên Manager (192.168.1.40)
docker node ls
# Kỳ vọng:
# ID        HOSTNAME    STATUS  AVAILABILITY  MANAGER STATUS
# xxx *     host-40     Ready   Active        Leader
# yyy       host-41     Ready   Active
# zzz       host-42     Ready   Active
```

**Mở firewall cho Swarm:**

```bash
# Trên CẢ 3 VM
sudo ufw allow 2377/tcp   # Swarm manager API (worker join)
sudo ufw allow 7946/tcp   # Container network discovery
sudo ufw allow 7946/udp
sudo ufw allow 4789/udp   # VXLAN overlay network (container-to-container traffic)
sudo ufw reload
```

### Bước 3.4 — Tạo Overlay Network

Overlay network cho phép container trên các node khác nhau giao tiếp như đang cùng mạng.

```bash
# Trên Manager
docker network create \
  --driver overlay \
  --attachable \
  app-network
# --attachable: cho phép container standalone (không chỉ service) tham gia network này
# Dùng khi cần chạy docker run debug từ bất kỳ node nào

docker network ls
# Kỳ vọng thấy app-network với driver=overlay, scope=swarm
```

---

### Docker Secrets — Bảo mật credentials trong Swarm

#### Tại sao cần Secrets?

**Vấn đề với biến môi trường:**
```yaml
# ❌ KHÔNG AN TOÀN — ai có docker-compose.yml hoặc docker inspect đều thấy
environment:
  - DB_PASSWORD=YourStrong@Passw0rd
  - JWT_SECRET=my-super-secret-key
```

Lý do nguy hiểm:
- `docker inspect <container>` hiện toàn bộ env vars — ai access Docker daemon đều thấy
- File `.env` thường bị commit nhầm lên git
- Logs container đôi khi in ra env vars khi crash

**Giải pháp — Docker Secrets:**
- Secret được mã hóa và lưu trong Raft log của Swarm (encrypted at rest)
- Chỉ được decrypt và mount vào container cần thiết
- Mount dưới dạng file tại `/run/secrets/<secret-name>` trong container
- Worker nodes chỉ nhận secret khi có task chạy trên đó — không lưu trên node không cần

```
[Swarm Manager]
  Raft store (encrypted):
    secret "db-password" = "YourStrong@Passw0rd" (encrypted)
    secret "jwt-key"     = "my-super-secret"     (encrypted)
         │
         │ Khi deploy service cần secret "db-password"
         ▼
  Container trên Worker1:
    /run/secrets/db-password  ← mount in-memory (tmpfs), chỉ container này đọc được
```

#### Bước 3.5 — Tạo Secrets

```bash
# Tất cả lệnh docker secret chạy trên MANAGER (192.168.1.40)

# Cách 1: Từ stdin (không để lộ trong bash history)
echo "YourStrong@Passw0rd" | docker secret create db-password -
echo "Admin@Postgres2025" | docker secret create pg-password -
echo "Lab@OpenIddict2025" | docker secret create cert-password -

# Cách 2: Từ file (phù hợp với binary như .pfx)
docker secret create openiddict-cert ./docker/certs/openiddict.pfx

# Cách 3: Từ biến môi trường (không để trong history)
read -s JWT_KEY
# Nhập key, nhấn Enter
echo "$JWT_KEY" | docker secret create jwt-signing-key -

# Liệt kê secrets (chỉ thấy tên, không thấy giá trị)
docker secret ls

# KHÔNG có lệnh nào để xem giá trị secret sau khi tạo
# Đây là tính năng bảo mật — nếu quên thì phải tạo lại
```

```bash
# Xóa secret (phải remove khỏi tất cả service trước)
docker secret rm db-password

# Xem metadata (không thấy value)
docker secret inspect db-password
```

#### Bước 3.6 — Đọc Secret trong ứng dụng .NET

Trong container, secret được mount tại `/run/secrets/<tên>` dưới dạng file text (hoặc binary).

```csharp
// SecretHelper.cs — đọc secret từ file, fallback về env var
public static class SecretHelper
{
    public static string GetSecret(string secretName, string envVarFallback)
    {
        var secretPath = $"/run/secrets/{secretName}";
        if (File.Exists(secretPath))
            return File.ReadAllText(secretPath).Trim();

        return Environment.GetEnvironmentVariable(envVarFallback)
               ?? throw new InvalidOperationException($"Secret '{secretName}' not found");
    }
}
```

```csharp
// Program.cs — dùng secret thay vì env var trực tiếp
var dbPassword = SecretHelper.GetSecret("db-password", "DB_PASSWORD");
var connectionString = $"Server=192.168.1.38,1433;Database=AuthDemoDB;" +
                       $"User Id=sa;Password={dbPassword};TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
```

Hoặc dùng cách thanh lịch hơn với `AddSecretConfiguration`:

```csharp
// Program.cs — tự động load tất cả /run/secrets/* vào IConfiguration
// Prefix mặc định là tên file, dùng thay cho env var tương ứng
builder.Configuration.AddKeyPerFile(
    directoryPath: "/run/secrets",
    optional: true,   // không crash nếu chạy local (không có /run/secrets)
    reloadOnChange: false
);

// Sau đó dùng bình thường
var connStr = builder.Configuration["ConnectionStrings:DefaultConnection"];
```

> `AddKeyPerFile`: mỗi file trong thư mục → 1 config key. File `/run/secrets/db-password` → key `db-password`. Kết hợp với `__` làm separator: file `ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`.

---

### Bước 3.7 — Deploy Stack với Secrets

**Docker Stack** = phiên bản Swarm của docker-compose — deploy nhiều service cùng lúc.

Tạo file `docker-stack.yml` (trên Manager — **không** có `build:`, Swarm cần image đã build sẵn):

```yaml
# ~/stacks/docker-stack.yml
# ⚠ Docker Stack KHÔNG hỗ trợ "build:" — chỉ dùng image đã build sẵn từ registry.
# Lý do: worker node không có source code, chỉ pull image từ registry về chạy.
version: "3.8"

services:

  api:
    # Image lấy từ private registry (192.168.1.50) — không qua internet.
    # Format: <registry-host>:<port>/<image-name>:<tag>
    image: 192.168.1.50:5000/authdemo-api:latest
    ports:
      # Swarm ingress routing: port 5000 trên TẤT CẢ node (.40, .41, .42) đều
      # forward vào container :8080. Client gọi vào bất kỳ node nào cũng được.
      - "5000:8080"
    secrets:
      # Khai báo secrets sẽ được mount vào container tại /run/secrets/<tên>.
      # Container đọc từ file, không qua env var → an toàn hơn.
      - db-password           # → /run/secrets/db-password
      - openiddict-cert       # → /run/secrets/openiddict-cert  (file .pfx)
      - cert-password         # → /run/secrets/cert-password
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      # Connection string KHÔNG có password — password lấy từ secret file.
      - ConnectionStrings__DefaultConnection=Server=192.168.1.38,1433;Database=AuthDemoDB;User Id=sa;TrustServerCertificate=True;
      - DB_SERVER=192.168.1.38,1433
      - DB_NAME=AuthDemoDB
      - DB_USER=sa
      # App đọc các biến *_FILE này để biết đường dẫn đến secret file.
      - DB_PASSWORD_FILE=/run/secrets/db-password
      - OpenIddict__CertPath=/run/secrets/openiddict-cert
      - OpenIddict__CertPasswordFile=/run/secrets/cert-password
    networks:
      - app-network   # tham gia overlay network để giao tiếp container-to-container
    deploy:
      # replicated = chạy đúng số lượng bản sao cố định.
      # Đối lập với global (1 container trên mỗi node, không kể số node).
      mode: replicated
      replicas: 3   # 3 container — 1 trên mỗi worker node (.41, .42 + 1 node nữa)
      placement:
        constraints:
          # Chỉ deploy trên worker, không deploy trên Manager (.40).
          # Manager chuyên điều phối — không nên chạy workload để tránh ảnh hưởng Raft.
          - node.role == worker
      update_config:
        parallelism: 1    # Rolling update: chỉ update 1 container tại 1 thời điểm.
                          # parallelism: 2 = update 2 cùng lúc (nhanh hơn, rủi ro hơn).
        delay: 10s        # Chờ 10s sau mỗi container update trước khi update cái tiếp theo.
                          # Thời gian để container mới warm up và healthcheck pass.
        failure_action: rollback  # Nếu update fail → tự động rollback toàn bộ về version cũ.
        monitor: 30s      # Sau khi update, theo dõi 30s để phát hiện failure muộn.
        order: start-first  # Start container MỚI trước, rồi mới kill container CŨ.
                            # → Zero downtime: lúc nào cũng có container phục vụ request.
      rollback_config:
        parallelism: 1   # rollback 1 container tại 1 thời điểm
        delay: 5s        # chờ 5s giữa mỗi container rollback
      restart_policy:
        condition: on-failure   # chỉ restart khi container exit với code != 0 (lỗi)
                                # không restart nếu stop thủ công
        delay: 5s               # chờ 5s trước khi restart (tránh restart loop quá nhanh)
        max_attempts: 3         # thử tối đa 3 lần; nếu vẫn fail → Swarm đánh dấu failed
        window: 120s            # reset đếm max_attempts sau 120s nếu container đang healthy
      resources:
        limits:
          cpus: '1.0'     # container này dùng tối đa 1 CPU core
          memory: 512M    # tối đa 512MB RAM — nếu vượt, container bị kill (OOMKilled)
        reservations:
          cpus: '0.25'    # Swarm đảm bảo node có ít nhất 0.25 CPU core trống trước khi đặt container
          memory: 256M    # đảm bảo node có ít nhất 256MB RAM trống
    healthcheck:
      # Swarm dùng healthcheck để biết container có thực sự ready xử lý request chưa.
      # Container start xong nhưng app chưa kịp init → Swarm chờ healthy mới route traffic vào.
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
      interval: 15s      # kiểm tra mỗi 15s
      timeout: 5s        # không trả lời trong 5s = fail
      retries: 3         # fail 3 lần liên tiếp → unhealthy → Swarm restart container
      start_period: 30s  # chờ 30s sau khi start trước khi bắt đầu healthcheck
                         # (.NET app cần thời gian khởi động — tránh fail ngay từ đầu)

networks:
  app-network:
    # external: true = network này đã tạo sẵn bằng "docker network create" (bước 3.4).
    # Không để Swarm tự tạo — tự tạo sẽ đặt tên khác (authdemo_app-network).
    external: true

secrets:
  # external: true = secret đã tạo sẵn bằng "docker secret create" (bước 3.5).
  # Swarm không tự tạo secret — phải tạo trước khi deploy stack.
  db-password:
    external: true
  openiddict-cert:
    external: true
  cert-password:
    external: true
```

```bash
# Deploy stack
docker stack deploy \
  --compose-file ~/stacks/docker-stack.yml \
  --with-registry-auth \    # gửi registry credentials đến worker nodes khi pull image
  authdemo

# Theo dõi quá trình deploy
docker stack ps authdemo    # xem task status
docker service ls           # xem service status
docker service logs authdemo_api --follow   # xem log realtime
```

### Bước 3.8 — Scaling và Rolling Update

```bash
# Scale tăng lên 6 replicas (2 trên mỗi node)
docker service scale authdemo_api=6

# Xem phân phối
docker service ps authdemo_api
# NAME              NODE        DESIRED STATE   CURRENT STATE
# authdemo_api.1    host-41     Running         Running 5m
# authdemo_api.2    host-42     Running         Running 5m
# authdemo_api.3    host-40     Running         Running 5m
# authdemo_api.4    host-41     Running         Running 30s
# authdemo_api.5    host-42     Running         Running 28s
# authdemo_api.6    host-40     Running         Running 25s

# Rolling update: build image mới, push lên registry, rồi:
docker service update \
  --image 192.168.1.50:5000/authdemo-api:v2 \
  authdemo_api
# Swarm cập nhật từng container theo thứ tự, không downtime

# Rollback về version trước nếu có vấn đề
docker service rollback authdemo_api

# Xem chi tiết service (bao gồm update/rollback history)
docker service inspect authdemo_api --pretty
```

### Bước 3.9 — Drain và Maintain Node

```bash
# Drain một node (khi cần maintenance): Swarm di chuyển tất cả container sang node khác
docker node update --availability drain host-41

# Verify: tất cả task trên host-41 đã chuyển đi
docker service ps authdemo_api

# Sau maintenance, đưa node trở lại
docker node update --availability active host-41

# Remove node khỏi Swarm
docker node rm host-41      # sau khi đã drain
# Trên node đó: docker swarm leave
```

### Bước 3.10 — Monitoring Swarm

```bash
# Xem tất cả running task trong cluster
docker stack ps authdemo --filter desired-state=running

# Xem resource usage trên manager
docker stats $(docker ps -q)

# Xem event log của Swarm (deploy, scale, fail, restart...)
docker events --filter type=service

# Lấy IP thật của từng task
docker service ps authdemo_api --format "{{.Name}} {{.Node}} {{.CurrentState}}"
```

---

## Part 4 — Private Registry (192.168.1.50)

### Tại sao cần Private Registry?

**Vấn đề với Docker Hub:**
- Pull limit: 100 pull/6h cho anonymous, 200/6h cho free account
- Image private trên Hub bị giới hạn số lượng
- Phụ thuộc internet — mạng chậm = deploy chậm
- Sensitive code không muốn lưu trên hub public

**Giải pháp:** Private Registry trong mạng nội bộ
- Pull/push không qua internet → nhanh hơn nhiều (LAN 1Gbps vs internet)
- Không giới hạn số image/pull
- Kiểm soát hoàn toàn ai được push/pull

---

### Cấp 1 — Docker Registry cơ bản

Dùng image `registry:2` của Docker — minimal, không có UI.

**Dùng khi:** Lab, team nhỏ, cần chạy nhanh không cần auth.

#### Bước REG.1.1 — Cài Docker trên 192.168.1.50

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
```

#### Bước REG.1.2 — Chạy Registry

```bash
mkdir -p ~/registry
cat > ~/registry/docker-compose.yml << 'EOF'
services:
  registry:
    # Image registry:2 — Docker Distribution Registry, phiên bản chính thức.
    # Minimal, không có UI, không có auth — chỉ dùng cho lab nội bộ tin tưởng.
    image: registry:2
    container_name: registry
    ports:
      # Port 5000 là port mặc định của Docker Registry.
      # Khi push: docker push 192.168.1.50:5000/myimage:tag
      - "5000:5000"
    environment:
      # Thư mục lưu tất cả image layers bên trong container.
      # Được mount ra volume bên dưới để dữ liệu không mất khi restart.
      REGISTRY_STORAGE_FILESYSTEM_ROOTDIRECTORY: /var/lib/registry
    volumes:
      # Toàn bộ image data được lưu tại đây.
      # Xóa volume này = mất hết tất cả image đã push lên.
      - registry-data:/var/lib/registry
    restart: unless-stopped

volumes:
  registry-data:   # volume lưu image layers — giữ nguyên kể cả khi container restart
EOF

cd ~/registry
docker compose up -d
```

#### Bước REG.1.3 — Cấu hình Docker client trust HTTP registry

Docker mặc định từ chối push/pull qua HTTP (không TLS). Cần cấu hình thêm.

```bash
# Trên MÁY PUSH và tất cả SWARM NODES (192.168.1.40, .41, .42)
sudo nano /etc/docker/daemon.json
```

```json
{
  "insecure-registries": ["192.168.1.50:5000"]
}
```

```bash
sudo systemctl restart docker
```

#### Bước REG.1.4 — Build và push image

```bash
# Trên máy build (có source code)
# Build image
docker build \
  -t 192.168.1.50:5000/authdemo-api:latest \
  -f AuthDemo.Api/Dockerfile \
  .

# Tag thêm version
docker tag 192.168.1.50:5000/authdemo-api:latest \
           192.168.1.50:5000/authdemo-api:v1.0.0

# Push
docker push 192.168.1.50:5000/authdemo-api:latest
docker push 192.168.1.50:5000/authdemo-api:v1.0.0
```

#### Bước REG.1.5 — Verify và quản lý Registry API

```bash
# Liệt kê tất cả image trong registry
curl http://192.168.1.50:5000/v2/_catalog
# Output: {"repositories":["authdemo-api"]}

# Liệt kê tags của 1 image
curl http://192.168.1.50:5000/v2/authdemo-api/tags/list
# Output: {"name":"authdemo-api","tags":["latest","v1.0.0"]}

# Pull từ một máy khác
docker pull 192.168.1.50:5000/authdemo-api:latest
```

**Xóa image (garbage collect):**

```bash
# Registry không xóa layer tự động — cần chạy GC
# Lấy digest của image cần xóa
DIGEST=$(curl -s \
  -H "Accept: application/vnd.docker.distribution.manifest.v2+json" \
  http://192.168.1.50:5000/v2/authdemo-api/manifests/v1.0.0 \
  -I | grep Docker-Content-Digest | awk '{print $2}' | tr -d '\r')

# Xóa manifest
curl -X DELETE http://192.168.1.50:5000/v2/authdemo-api/manifests/$DIGEST

# Chạy garbage collect trong container
docker exec registry bin/registry garbage-collect /etc/docker/registry/config.yml
```

---

### Cấp 2 — Registry + TLS + Basic Auth

**Khi nào dùng:** Khi cần bảo mật — ngăn người không được phép push/pull.

**TLS cần thiết vì:**
- HTTP registry không yêu cầu auth → ai có network là push/pull được
- Docker yêu cầu TLS nếu dùng Basic Auth

#### Bước REG.2.1 — Tạo Self-Signed Certificate

```bash
mkdir -p ~/registry/certs ~/registry/auth

# Tạo CA key + cert
openssl genrsa -out ~/registry/certs/ca.key 4096
openssl req -new -x509 -days 3650 \
  -key ~/registry/certs/ca.key \
  -out ~/registry/certs/ca.crt \
  -subj "/CN=LabCA/O=Lab/C=VN"

# Tạo registry key + CSR
openssl genrsa -out ~/registry/certs/registry.key 4096
openssl req -new \
  -key ~/registry/certs/registry.key \
  -out ~/registry/certs/registry.csr \
  -subj "/CN=192.168.1.50/O=Lab/C=VN"

# Tạo extension file (SAN — Subject Alternative Names)
cat > ~/registry/certs/ext.cnf << 'EOF'
[v3_req]
subjectAltName = @alt_names

[alt_names]
IP.1 = 192.168.1.50
DNS.1 = registry.local
EOF

# Ký cert bởi CA
openssl x509 -req -days 3650 \
  -in ~/registry/certs/registry.csr \
  -CA ~/registry/certs/ca.crt \
  -CAkey ~/registry/certs/ca.key \
  -CAcreateserial \
  -out ~/registry/certs/registry.crt \
  -extensions v3_req \
  -extfile ~/registry/certs/ext.cnf

# Verify
openssl verify -CAfile ~/registry/certs/ca.crt ~/registry/certs/registry.crt
```

#### Bước REG.2.2 — Tạo file htpasswd (user/password)

```bash
# Dùng image httpd để tạo htpasswd (tránh cài apache2-utils)
docker run --rm \
  httpd:alpine \
  htpasswd -Bbn registry-admin "Admin@Registry2025" > ~/registry/auth/htpasswd

# Thêm user thứ 2 (developer — chỉ pull, không push)
docker run --rm \
  httpd:alpine \
  htpasswd -Bbn dev-user "Dev@Registry2025" >> ~/registry/auth/htpasswd

cat ~/registry/auth/htpasswd
# Kỳ vọng: thấy 2 dòng với hash bcrypt ($2y$...)
```

#### Bước REG.2.3 — Cập nhật docker-compose với TLS + Auth

```yaml
# ~/registry/docker-compose.yml — Cấp 2: có TLS + Basic Auth
services:
  registry:
    image: registry:2
    container_name: registry
    ports:
      # Đổi từ 5000 → 443 để client dùng HTTPS chuẩn.
      # docker push 192.168.1.50/myimage (không cần port vì 443 là mặc định HTTPS)
      - "443:5000"
    environment:
      # Đường dẫn đến cert và key TLS bên trong container.
      # Registry tự bật HTTPS khi 2 biến này được set.
      REGISTRY_HTTP_TLS_CERTIFICATE: /certs/registry.crt
      REGISTRY_HTTP_TLS_KEY:         /certs/registry.key

      # Loại authentication: htpasswd (file username:bcrypt-hash).
      REGISTRY_AUTH: htpasswd
      # Tên realm hiển thị khi client bị hỏi username/password (chuỗi tùy ý).
      REGISTRY_AUTH_HTPASSWD_REALM: "Registry Realm"
      # Đường dẫn đến file htpasswd trong container.
      REGISTRY_AUTH_HTPASSWD_PATH: /auth/htpasswd

      # Cho phép xóa image qua API (DELETE /v2/<name>/manifests/<digest>).
      # Mặc định false — bật để có thể dọn dẹp image cũ.
      REGISTRY_STORAGE_DELETE_ENABLED: "true"
    volumes:
      - registry-data:/var/lib/registry
      - ./certs:/certs:ro   # cert và key TLS — read-only
      - ./auth:/auth:ro     # file htpasswd chứa username/password — read-only
    restart: unless-stopped

volumes:
  registry-data:
```

```bash
cd ~/registry
docker compose down && docker compose up -d
```

#### Bước REG.2.4 — Trust CA trên client và Swarm nodes

```bash
# Chạy trên MỖI client (máy push) và MỖI Swarm node (pull khi deploy)
# Trên Ubuntu:
sudo mkdir -p /etc/docker/certs.d/192.168.1.50
sudo cp ~/registry/certs/ca.crt /etc/docker/certs.d/192.168.1.50/ca.crt

# Hoặc copy từ registry server
scp bank@192.168.1.50:~/registry/certs/ca.crt /tmp/
sudo mkdir -p /etc/docker/certs.d/192.168.1.50
sudo cp /tmp/ca.crt /etc/docker/certs.d/192.168.1.50/ca.crt

# Restart Docker
sudo systemctl restart docker
```

#### Bước REG.2.5 — Login và Push

```bash
# Login (lần đầu)
docker login 192.168.1.50 \
  -u registry-admin \
  -p "Admin@Registry2025"
# Credentials được lưu tại ~/.docker/config.json (encrypted)

# Push image
docker build -t 192.168.1.50/authdemo-api:v2 -f AuthDemo.Api/Dockerfile .
docker push 192.168.1.50/authdemo-api:v2

# Pull (cần login trước)
docker pull 192.168.1.50/authdemo-api:v2
```

**Dùng với Docker Swarm:**

```bash
# Tạo secret chứa registry credentials để Swarm pull image khi deploy
cat ~/.docker/config.json | docker secret create registry-credentials -

# Hoặc khi deploy stack
docker stack deploy \
  --compose-file docker-stack.yml \
  --with-registry-auth \   # gửi credentials hiện tại của manager đến worker
  authdemo
```

---

### Cấp 3 — Harbor (Enterprise-grade Registry)

**Harbor** = open-source registry cấp enterprise, thay thế Artifactory/Nexus.

**Khi nào dùng Harbor thay vì Docker Registry:**
- Cần UI đẹp để quản lý image, xem layer
- Cần Role-Based Access Control (RBAC) — admin/developer/readonly
- Cần scan vulnerability tự động (Trivy/Clair tích hợp sẵn)
- Cần replication: sync image tự động giữa nhiều registry
- Cần audit log: ai push/pull gì, khi nào
- Cần image signing (Notary)

**Nhược điểm:**
- Yêu cầu nhiều tài nguyên hơn (~4GB RAM, nhiều container)
- Cài đặt phức tạp hơn

#### Bước REG.3.1 — Cài Harbor

```bash
# Trên 192.168.1.50
# Harbor cần Docker Compose v2
docker compose version   # phải >= 2.x

# Download Harbor installer
cd ~
HARBOR_VERSION=v2.10.2
wget https://github.com/goharbor/harbor/releases/download/${HARBOR_VERSION}/harbor-online-installer-${HARBOR_VERSION}.tgz
tar xvf harbor-online-installer-${HARBOR_VERSION}.tgz
cd harbor
```

```bash
# Tạo self-signed cert (hoặc dùng cert từ bước REG.2.1)
mkdir -p ~/harbor/certs
openssl req -x509 -nodes -days 3650 -newkey rsa:4096 \
  -keyout ~/harbor/certs/harbor.key \
  -out ~/harbor/certs/harbor.crt \
  -subj "/CN=192.168.1.50/O=Lab" \
  -addext "subjectAltName=IP:192.168.1.50"
```

```bash
# Tạo harbor.yml từ template
cp harbor.yml.tmpl harbor.yml
```

Sửa `harbor.yml`:

```yaml
# harbor.yml — chỉ các dòng quan trọng cần thay đổi

hostname: 192.168.1.50    # IP hoặc domain của Harbor

# HTTPS
https:
  port: 443
  certificate: /root/harbor/certs/harbor.crt
  private_key: /root/harbor/certs/harbor.key

# HTTP redirect (tắt để chỉ dùng HTTPS)
http:
  port: 80

# Admin password
harbor_admin_password: Admin@Harbor2025

# Database
database:
  password: Postgres@Harbor2025

# Data path
data_volume: /data/harbor

# Log
log:
  level: info
  rotate_count: 50
  rotate_size: 200M
  location: /var/log/harbor
```

```bash
# Install
sudo mkdir -p /data/harbor
sudo ./install.sh \
  --with-trivy   # Bật Trivy vulnerability scanner

# Theo dõi cài đặt (mất 5-10 phút lần đầu)
```

#### Bước REG.3.2 — Truy cập Harbor UI

```
Mở browser: https://192.168.1.50
Username: admin
Password: Admin@Harbor2025
```

#### Bước REG.3.3 — Tạo Project và User trong Harbor

```
1. Vào Administration → Users → New User
   - Username: dev-team
   - Password: Dev@Harbor2025
   - Role: Developer

2. Vào Projects → New Project
   - Project name: authdemo
   - Access Level: Private
   - → Tạo

3. Vào authdemo → Members → Add Member
   - User: dev-team
   - Role: Developer (push + pull)
         hoặc Guest (chỉ pull)
```

#### Bước REG.3.4 — Dùng Harbor với Docker

```bash
# Trust CA (tương tự Cấp 2)
sudo mkdir -p /etc/docker/certs.d/192.168.1.50
# Export cert từ Harbor UI: Configuration → System Settings → Registry Root Certificate → Download
sudo cp harbor.crt /etc/docker/certs.d/192.168.1.50/ca.crt
sudo systemctl restart docker

# Login với user Harbor
docker login 192.168.1.50 -u dev-team -p "Dev@Harbor2025"

# Push image vào project
docker tag authdemo-api:latest 192.168.1.50/authdemo/authdemo-api:latest
docker push 192.168.1.50/authdemo/authdemo-api:latest
```

#### Bước REG.3.5 — Tính năng nổi bật Harbor

**Vulnerability Scanning (Trivy):**
```
Vào Harbor UI → image cụ thể → Scan → xem CVE list
Hoặc cấu hình scan tự động mỗi khi push image
```

**Replication — sync image sang registry khác:**
```
Administration → Registries → New Endpoint
  Type: Docker Hub / Docker Registry / Harbor ...
  URL:  https://registry-hub.com

Replications → New Replication Rule
  Source: local project
  Destination: registry vừa tạo
  Trigger: Event-based (khi push) / Scheduled
```

**Webhook — trigger CI/CD khi push:**
```
Project → authdemo → Webhooks → Add Webhook
URL: http://ci-server/webhook/harbor
Events: Push artifact, Delete artifact, Scan completed
```

**Quản lý qua API:**
```bash
# Liệt kê repositories
curl -s -u admin:Admin@Harbor2025 \
  https://192.168.1.50/api/v2.0/projects/authdemo/repositories | jq .

# Trigger scan
curl -X POST -u admin:Admin@Harbor2025 \
  https://192.168.1.50/api/v2.0/projects/authdemo/repositories/authdemo-api/artifacts/latest/scan
```

---

### Quy trình CI/CD đầy đủ với Private Registry + Swarm

```
[Developer push code]
        │
        ▼
[CI Server]
  1. git pull
  2. docker build -t 192.168.1.50/authdemo/authdemo-api:$GIT_SHA .
  3. docker push 192.168.1.50/authdemo/authdemo-api:$GIT_SHA
  4. SSH vào Swarm Manager:
     docker service update \
       --image 192.168.1.50/authdemo/authdemo-api:$GIT_SHA \
       authdemo_api
        │
        ▼
[Swarm Manager]
  - Rolling update: kill 1 old container, start 1 new container
  - Worker nodes: docker pull 192.168.1.50/authdemo/... (từ private registry)
  - Health check pass → tiếp tục; fail → rollback tự động
```

---

## So sánh tổng hợp

### Nginx Load Balancing Algorithms

| Algorithm | Dùng khi | Tránh khi |
|---|---|---|
| `round-robin` (default) | API stateless, request nhanh đều nhau | Request có thời gian xử lý rất khác nhau |
| `least_conn` | Upload, long polling, WebSocket | — |
| `ip_hash` | Session-based app (cần sticky session) | API stateless (lãng phí) |
| `random two least_conn` | Nhiều backend (>10), tải không đều | Backend ít |

### DB Replication

| | SQL Server AG | PostgreSQL Streaming |
|---|---|---|
| Chi phí | Cao (Enterprise) | Miễn phí |
| Setup | Phức tạp | Đơn giản |
| Readable replica | Enterprise only | Mặc định |
| Failover tự động | Pacemaker/WSFC | Patroni/Repmgr |
| Phù hợp | Đang có Windows/SQL Server | Greenfield, startup, Linux |

### Registry

| | Docker Registry (cơ bản) | Docker Registry + TLS/Auth | Harbor |
|---|---|---|---|
| Chi phí | Miễn phí | Miễn phí | Miễn phí (self-host) |
| Cài đặt | 1 container, 5 phút | 30 phút | 1-2 giờ |
| UI | Không có | Không có | Đầy đủ |
| RBAC | Không | Basic Auth (user/pass) | Role-based |
| Vulnerability scan | Không | Không | Có (Trivy) |
| Phù hợp | Lab, prototype | Team nhỏ | Team/production |

### Docker Swarm vs Kubernetes

| | Docker Swarm | Kubernetes |
|---|---|---|
| Học | Dễ — cùng syntax docker-compose | Khó — nhiều khái niệm mới |
| Cài đặt | `docker swarm init` — 1 lệnh | kubeadm, kind, k3s... |
| Tài nguyên | Nhẹ (~100MB overhead) | Nặng hơn (~500MB+) |
| Secrets | Có sẵn, đơn giản | Secrets + RBAC phức tạp hơn |
| Auto-scaling | Không (cần thủ công hoặc external) | HPA tự động |
| Ecosystem | Nhỏ hơn | Rất lớn (Helm, Operator...) |
| Phù hợp | Startup, team nhỏ, lab, Docker-native | Production lớn, cloud-native |

---

## Troubleshooting thường gặp

### Nginx

```bash
# Config sai → reload fail
docker exec nginx-lb nginx -t
# Output sẽ chỉ rõ dòng lỗi trong nginx.conf

# Backend không thể reach
docker exec nginx-lb wget -qO- http://192.168.1.40/health
# Nếu fail → kiểm tra firewall trên Swarm node

# Xem upstream mà Nginx đang gửi đến
docker exec nginx-lb tail -f /var/log/nginx/access.log | grep upstream
```

### Docker Swarm

```bash
# Service không start
docker service ps authdemo_api --no-trunc
# Cột ERROR sẽ hiện lý do fail

# Image pull fail (registry auth)
docker service ps authdemo_api
# Nếu thấy "No such image" hoặc "pull access denied"
# → Verify: docker login trên Manager, dùng --with-registry-auth khi deploy

# Secret mount fail
docker service logs authdemo_api 2>&1 | grep -i secret
# Verify secret tồn tại: docker secret ls
# Verify secret name trong stack file khớp với tên đã tạo

# Node không join được
# Trên Manager:
docker swarm join-token worker   # lấy token mới
# Trên Worker:
docker info | grep -i swarm      # xem state hiện tại
docker swarm leave               # rời swarm cũ trước khi join lại

# Overlay network không thông
docker run --rm --network app-network alpine ping -c3 <container-name>
# Nếu fail → verify port 4789/udp mở trên tất cả node
sudo ufw status | grep 4789
```

### SQL Server AG

```sql
-- AG ở RESOLVING state (không phải HEALTHY)
SELECT name, synchronization_health_desc
FROM sys.dm_hadr_database_replica_states drs
JOIN sys.databases d ON drs.database_id = d.database_id;

-- Kiểm tra endpoint đang chạy
SELECT name, state_desc, role_desc FROM sys.endpoints;

-- Restart endpoint nếu cần
ALTER ENDPOINT AG_Endpoint STATE = STOPPED;
ALTER ENDPOINT AG_Endpoint STATE = STARTED;
```

```bash
# Port 5022 chưa mở → AG không kết nối được
sudo ufw status | grep 5022
# Nếu thiếu: sudo ufw allow 5022/tcp

# Test kết nối endpoint giữa 2 VM
nc -zv 192.168.1.38 5022   # từ VM_DB2
nc -zv 192.168.1.39 5022   # từ VM_DB1
```

### PostgreSQL Replication

```bash
# Replica không streaming
docker exec postgres-replica psql -U pgadmin \
  -c "SELECT status, last_error FROM pg_stat_wal_receiver;"
# last_error sẽ cho biết lý do fail

# Lỗi thường gặp 1: pg_hba.conf chưa allow replication user
# → Thêm dòng "host replication replicator 192.168.1.0/24 scram-sha-256" vào pg_hba.conf
# → Reload: docker exec postgres-primary psql -U pgadmin -c "SELECT pg_reload_conf();"

# Lỗi thường gặp 2: replica lag quá lớn, WAL segment bị xóa trên Primary
# → Tăng wal_keep_size trên Primary
# → Hoặc dùng replication slot (đảm bảo Primary không xóa WAL cho đến khi replica apply)
docker exec postgres-primary psql -U pgadmin \
  -c "SELECT * FROM pg_create_physical_replication_slot('replica_slot');"
# Sau đó trên Replica, thêm vào postgresql.auto.conf:
# primary_slot_name = 'replica_slot'
```

### Harbor / Registry

```bash
# Push fail: "x509: certificate signed by unknown authority"
# → CA chưa được trust trên client
sudo ls /etc/docker/certs.d/192.168.1.50/
# Nếu trống → copy ca.crt vào đây → restart docker

# Harbor UI không load được
cd ~/harbor
docker compose ps    # xem container nào unhealthy
docker compose logs harbor-core | tail -50

# Disk đầy → push fail
df -h /data/harbor   # kiểm tra dung lượng
# Chạy GC qua Harbor UI: Administration → Garbage Collection → Run Now
```

---

## Checklist hoàn thành Lab

```
□ 192.168.1.37  Nginx Master running, curl /nginx-health = ok
□ 192.168.1.43  Nginx Backup running (config giống Master)
□ 192.168.1.36  VIP active — ip addr trên .37 thấy .36, trên .43 không thấy
□ Keepalived     Failover test: stop nginx trên .37 → VIP chuyển sang .43 trong <2s
□ 192.168.1.37  Nginx round-robin đến Swarm nodes (log thấy upstream xen kẽ)
□ 192.168.1.37  HTTPS hoạt động (self-signed hoặc Let's Encrypt)
□ 192.168.1.38  SQL Server Primary — AG HEALTHY, database ở FULL recovery
□ 192.168.1.39  SQL Server Secondary — READ_ONLY, data sync từ Primary
□ 192.168.1.38  PostgreSQL Primary — wal_level=replica, replicator user tạo
□ 192.168.1.39  PostgreSQL Replica — status=streaming, lag gần 0
□ 192.168.1.40  Docker Swarm Manager — `docker node ls` thấy 3 node Ready
□ 192.168.1.41  Swarm Worker1 joined
□ 192.168.1.42  Swarm Worker2 joined
□ Swarm         Secrets db-password, cert-password, openiddict-cert đã tạo
□ Swarm         Stack authdemo deployed, 3+ replicas Running
□ Swarm         Rolling update thành công (không downtime)
□ 192.168.1.50  Registry running, push/pull hoạt động
□ Swarm nodes   Trust CA của registry, pull image thành công khi deploy
□ 192.168.1.51  Alertmanager :9093 running, UI load được
□ Alertmanager  Prometheus → Status → Alertmanagers hiện địa chỉ alertmanager:9093
□ Alertmanager  amtool check-config pass, receivers hiện đủ tên
□ Alertmanager  Test alert giả lập (amtool alert add) xuất hiện trong UI
□ Alertmanager  Inhibition rule hoạt động: dừng node-exporter trên .40 → chỉ thấy TargetDown
```

---

> **Ghi chú VMware:** Có thể clone VM nhanh bằng "Linked Clone" để tạo nhiều VM từ 1 snapshot. 
> Sau khi clone, nhớ đổi:
> - Hostname: `sudo hostnamectl set-hostname <new-name>`
> - IP tĩnh trong `/etc/netplan/00-installer-config.yaml`
> - Machine ID (để tránh conflict DHCP): `sudo truncate -s 0 /etc/machine-id && sudo systemd-machine-id-setup`
> - Chạy `sudo netplan apply` sau khi đổi IP

---

## Part 5 — Monitoring Stack: Prometheus + Grafana + InfluxDB + k6 (192.168.1.51)

### Trả lời câu hỏi thực tế trước

#### Câu hỏi 1: Có cần tạo thêm VM riêng không?

**Có — nên tạo 1 VM riêng cho monitoring.**

Lý do:
- Monitoring cần chạy **liên tục 24/7**, kể cả khi bạn đang update/restart toàn bộ Swarm cluster
- Nếu để chung với Swarm node, khi node đó bị drain/restart thì mất luôn dữ liệu metric tại thời điểm quan trọng nhất
- Prometheus lưu time-series data tốn disk — để riêng tránh ảnh hưởng app server
- Grafana + InfluxDB cần RAM ổn định — không bị Swarm container tranh tài nguyên

**Cấu hình VM monitoring gợi ý:**
- RAM: 4 GB (Prometheus ~1GB, Grafana ~512MB, InfluxDB ~1GB, buffer)
- CPU: 2 core
- Disk: 50 GB (Prometheus lưu 15 ngày metric theo mặc định, InfluxDB lưu k6 results)
- IP: 192.168.1.51

#### Câu hỏi 2: 1 VM dùng chung cho 10 dự án được không?

**Được — đây là cách chuẩn trong thực tế.**

Prometheus, Grafana, InfluxDB đều hỗ trợ **multi-tenancy**:

| Tool | Cách phân tách 10 dự án |
|---|---|
| **Prometheus** | Label `project="projectA"` trên mỗi target — query filter theo label |
| **Grafana** | Mỗi team có **Organization** riêng, hoặc dùng **Dashboard** với variable `$project` |
| **InfluxDB v2** | Mỗi dự án có **Bucket** riêng — dữ liệu k6 hoàn toàn tách biệt |

10 dự án với tải lab (không phải production) sẽ thoải mái trên 1 VM 4GB RAM.

---

### Kiến trúc tổng quan Monitoring

```
┌─────────────────────────────────────────────────────────────────────┐
│                     MONITORING VM — 192.168.1.51                    │
│                                                                      │
│  ┌─────────────┐    scrape metrics    ┌──────────────────────────┐  │
│  │  Prometheus  │ ◄─────────────────── │ Node Exporter :9100      │  │
│  │   :9090      │    mỗi 15 giây       │ (chạy trên TẤT CẢ VM)   │  │
│  │  (pull-based)│ ◄─────────────────── │ cAdvisor :8081           │  │
│  └──────┬───────┘                      │ (Docker container metric)│  │
│         │                              │ Nginx Exporter :9113     │  │
│         │ data source                  └──────────────────────────┘  │
│         ▼                                                            │
│  ┌─────────────┐                                                     │
│  │   Grafana   │ ◄──── data source ──── ┌─────────────┐            │
│  │    :3000    │                         │  InfluxDB   │            │
│  │  Dashboard  │                         │    :8086    │            │
│  └─────────────┘                         │ (push-based)│            │
│                                          └──────┬──────┘            │
│                                                 ▲                   │
└─────────────────────────────────────────────────┼───────────────────┘
                                                  │ push results
                                           ┌──────┴──────┐
                                           │  k6 :local  │
                                           │ (load test) │
                                           └─────────────┘

Luồng dữ liệu:
  Prometheus: PULL — Prometheus chủ động đến từng target lấy metric
  InfluxDB:   PUSH — k6 chủ động đẩy kết quả vào InfluxDB
```

**Tại sao dùng CẢ Prometheus VÀ InfluxDB — không dùng 1 thứ?**

| | Prometheus | InfluxDB |
|---|---|---|
| Mô hình | Pull (scrape) | Push (write) |
| Phù hợp | App metrics liên tục (CPU, latency, error rate) | Kết quả test ngắn hạn, event-driven |
| k6 output | Không native | Có sẵn (`--out influxdb`) |
| Query | PromQL (mạnh cho alert) | Flux/InfluxQL (mạnh cho time range) |
| Kết luận | Dùng để monitor app đang chạy | Dùng để lưu và phân tích kết quả k6 |

---

### Bước MON.1 — Tạo VM và cài Docker trên 192.168.1.51

Clone từ VM có sẵn trong VMware, đổi IP thành .51:

```bash
# Sau khi clone và boot
sudo hostnamectl set-hostname monitoring

# Đổi IP
sudo nano /etc/netplan/00-installer-config.yaml
# Sửa address: 192.168.1.51/24

sudo netplan apply
ip a   # verify IP .51

# Cài Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
```

---

### Bước MON.2 — Tạo cấu trúc thư mục

```bash
# Tạo cây thư mục cho toàn bộ monitoring stack
mkdir -p ~/monitoring/{prometheus,grafana/provisioning/{datasources,dashboards},influxdb}

# Giải thích từng thư mục:
# ~/monitoring/prometheus/          → file cấu hình Prometheus
# ~/monitoring/grafana/provisioning → auto-provision datasource và dashboard
#   /datasources/                   → Grafana tự kết nối Prometheus/InfluxDB khi start
#   /dashboards/                    → Grafana tự import dashboard JSON khi start
# ~/monitoring/influxdb/            → cấu hình InfluxDB
```

---

### Bước MON.3 — Viết prometheus.yml (cấu hình scrape targets)

```bash
cat > ~/monitoring/prometheus/prometheus.yml << 'EOF'
# prometheus.yml — cấu hình trung tâm của Prometheus
# Prometheus đọc file này khi start, sau đó cứ 15s đến từng target để lấy metric

global:
  scrape_interval:     15s   # Cứ 15 giây scrape 1 lần
                             # Giảm → metric chính xác hơn nhưng tốn tài nguyên
                             # Tăng → ít tài nguyên hơn nhưng mất granularity
  evaluation_interval: 15s   # Cứ 15 giây evaluate alerting rules
                             # Chưa có effect ở Part 5 — rule_files được thêm ở Part 6
  # Thêm label mặc định vào TẤT CẢ time series — giúp phân biệt khi có nhiều Prometheus
  external_labels:
    monitor: 'lab-monitor'
    environment: 'lab'

# alerting: và rule_files: sẽ được thêm ở Part 6 (Alertmanager)
# Chạy Prometheus trước không cần 2 block này — scrape_configs đủ để xem metric trên UI

# ─── Scrape configs — ĐỊNH NGHĨA AI cần được scrape ─────────────────────────
scrape_configs:

  # 1. Prometheus tự monitor chính nó
  # Prometheus expose metric của bản thân tại /metrics
  # Từ đây biết: số target đang scrape, scrape duration, storage size...
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  # 2. Node Exporter — metric của HỆ ĐIỀU HÀNH host (không phải container)
  # Metrics: CPU usage, RAM, disk I/O, network in/out, load average...
  # Node Exporter chạy trên TỪNG VM và expose tại port 9100
  - job_name: 'node-exporter'
    static_configs:
      - targets:
          - '192.168.1.37:9100'    # Nginx Master
          - '192.168.1.43:9100'    # Nginx Backup
          - '192.168.1.38:9100'    # DB Primary
          - '192.168.1.39:9100'    # DB Secondary
          - '192.168.1.40:9100'    # Swarm Manager
          - '192.168.1.41:9100'    # Swarm Worker1
          - '192.168.1.42:9100'    # Swarm Worker2
          - '192.168.1.51:9100'    # Monitoring VM
    # relabel_configs: thêm label phụ vào từng metric
    # Ví dụ: thêm label "vm_role" để dashboard phân biệt loại VM
    relabel_configs:
      - source_labels: [__address__]
        target_label: instance    # giữ IP:port làm tên instance

  # 3. cAdvisor — metric của Docker CONTAINER (không phải host)
  # Metrics: container CPU%, memory usage, network in/out per container...
  # cAdvisor phải chạy trên node có Docker — cần mount /var/run/docker.sock
  - job_name: 'cadvisor'
    static_configs:
      - targets:
          - '192.168.1.40:8081'   # Swarm Manager
          - '192.168.1.41:8081'   # Worker1
          - '192.168.1.42:8081'   # Worker2
          - '192.168.1.51:8081'   # Monitoring VM

  # 4. Nginx Prometheus Exporter — metric của Nginx stub_status
  # Metrics: requests/s, active connections, reading/writing/waiting connections
  # Exporter đọc Nginx stub_status (đã bật ở bước 1.3: /nginx-status) và convert sang Prometheus format
  - job_name: 'nginx'
    static_configs:
      - targets:
          - '192.168.1.37:9113'   # Nginx Master
          - '192.168.1.43:9113'   # Nginx Backup (nếu đã tạo VM .43)

  # ─── MULTI-PROJECT: Thêm project khác vào đây ──────────────────────────────
  # Ví dụ: project thứ 2 có Swarm cluster riêng ở subnet khác
  # - job_name: 'node-exporter-project2'
  #   static_configs:
  #     - targets: ['10.0.1.40:9100', '10.0.1.41:9100']
  #   labels:
  #     project: 'project2'
  #     team: 'team-b'
EOF
```

---

## Part 6 — Alertmanager: Cấu hình và gửi thông báo (192.168.1.51)

> **Yêu cầu trước khi học Part 6:** Đã hoàn thành Part 5 MON.1–MON.6 (Prometheus đang chạy, scrape được target, thấy metric trên UI :9090). Part 6 bổ sung lớp thông báo vào hệ thống monitoring — không phụ thuộc vào Grafana hay InfluxDB.

### Alertmanager là gì và tại sao cần?

Prometheus chỉ **phát hiện** alert (evaluate rule, đánh dấu FIRING). **Alertmanager** nhận alert từ Prometheus và xử lý toàn bộ logic **gửi thông báo**:

```
[Prometheus]
  evaluate rules mỗi 15s
  rule → FIRING
        │
        │ POST /api/v1/alerts (HTTP)
        ▼
[Alertmanager :9093]
  ┌─────────────────────────────────────────────────────────┐
  │  Deduplication — gom alert giống nhau thành 1            │
  │  Grouping      — nhóm alert liên quan gửi 1 message      │
  │  Inhibition    — nếu .38 down thì không spam "lag cao"   │
  │  Silencing     — tắt alert trong giờ maintenance         │
  │  Routing       — alert severity=critical → PagerDuty     │
  │                  alert severity=warning  → Slack          │
  └──────────────────────────────┬──────────────────────────┘
                                 │
                    ┌────────────┼────────────┐
                    ▼            ▼            ▼
               [Email]       [Slack]    [Webhook]
```

**Tại sao không gửi thẳng từ Prometheus?**
- Prometheus không có deduplication — cứ 15s evaluate lại là gửi 1 message
- Không có grouping — 8 VM down gửi 8 email riêng thay vì 1 email tổng
- Không có silencing — maintenance cũng bị spam
- Không có routing — mọi alert đều đến cùng 1 kênh

---

### Bước AM.0 — Cập nhật prometheus.yml để kết nối Alertmanager

Ở Part 5 (MON.3), `prometheus.yml` chỉ có `scrape_configs` — chưa biết Alertmanager tồn tại. Bước này thêm 2 block còn thiếu vào file đó.

```bash
# Mở file đã tạo ở Part 5 MON.3
nano ~/monitoring/prometheus/prometheus.yml
```

Thêm 2 block sau vào **ngay sau phần `global:`**, trước `scrape_configs:`:

```yaml
# ─── Alertmanager — nơi Prometheus gửi alert khi rule trigger ─────────────────
alerting:
  alertmanagers:
    - static_configs:
        - targets: ['alertmanager:9093']   # tên service trong docker-compose network
      timeout: 10s

# ─── Rule files — Prometheus load các file rule từ thư mục này ────────────────
# Các file .yml trong rules/ định nghĩa KHI NÀO alert được bắn (expr + for)
# Alertmanager quyết định GỬI ĐI ĐÂU
rule_files:
  - "rules/*.yml"
```

Tạo thư mục chứa rule files (dùng ở AM.7):

```bash
mkdir -p ~/monitoring/prometheus/rules
```

Reload Prometheus để nhận config mới (nếu Prometheus đã đang chạy):

```bash
curl -X POST http://192.168.1.51:9090/-/reload
# hoặc restart nếu chưa chạy — sẽ tự load khi docker compose up ở MON.6
```

Verify Prometheus đã nhận Alertmanager:

```bash
# Mở: http://192.168.1.51:9090/status
# Phần "Alertmanagers" phải hiện: http://alertmanager:9093/api/v2/alerts
# Nếu thấy "0 active alertmanagers" → Alertmanager chưa start → tiếp tục AM.4
```

---

### Bước AM.1 — Tạo cấu trúc thư mục Alertmanager

```bash
mkdir -p ~/monitoring/alertmanager/templates
```

### Bước AM.2 — Viết alertmanager.yml

```bash
cat > ~/monitoring/alertmanager/alertmanager.yml << 'EOF'
# alertmanager.yml — cấu hình trung tâm của Alertmanager

global:
  # Thời gian Alertmanager chờ sau khi alert không còn FIRING trước khi gửi "resolved"
  resolve_timeout: 5m

  # ─── SMTP (Email) ───────────────────────────────────────────────────────────
  smtp_smarthost: 'smtp.gmail.com:587'       # Gmail SMTP (thay bằng SMTP nội bộ nếu có)
  smtp_from: 'alertmanager@lab.local'        # địa chỉ gửi
  smtp_auth_username: 'your-gmail@gmail.com' # tài khoản Gmail
  smtp_auth_password: 'your-app-password'    # App Password (không phải password Gmail thường)
                                             # Tạo tại: Google Account → Security → App passwords
  smtp_require_tls: true

  # ─── Slack ──────────────────────────────────────────────────────────────────
  # Webhook URL lấy từ: Slack API → Incoming Webhooks → Add to Slack
  slack_api_url: 'https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXX'

# ─── TEMPLATES — định dạng message ──────────────────────────────────────────
# Load tất cả file .tmpl trong thư mục templates/
templates:
  - '/etc/alertmanager/templates/*.tmpl'

# ─── ROUTING — quyết định alert nào đến kênh nào ─────────────────────────────
#
# Cấu trúc routing là CÂY (tree):
#   - Bắt đầu từ root route
#   - Prometheus gửi alert → Alertmanager đi từ root → match route con đầu tiên thỏa
#   - Nếu không match route nào → dùng root route (receiver mặc định)
#
# continue: false (mặc định) = dừng khi match route đầu tiên
# continue: true             = tiếp tục kiểm tra route sau khi đã match

route:
  # Receiver mặc định — dùng khi alert không match route nào bên dưới
  receiver: 'email-lab'

  # Grouping: nhóm các alert có cùng label "alertname" + "severity" thành 1 message
  # Thay vì gửi 5 email cho 5 instance disk đầy → gửi 1 email tổng
  group_by: ['alertname', 'severity', 'instance']

  # Chờ 30s từ khi alert đầu tiên xuất hiện trước khi gửi
  # Để gom thêm alert liên quan trong cùng nhóm (nếu nhiều VM cùng die lúc đó)
  group_wait: 30s

  # Khi đã gửi 1 group rồi, nếu có alert mới trong group → chờ 5m rồi gửi lại
  # Tránh spam khi hệ thống đang hồi phục
  group_interval: 5m

  # Gửi lại alert đang FIRING (nhắc nhở) mỗi 4h
  # Tránh team quên alert đang chạy nền
  repeat_interval: 4h

  # ─── ROUTES CON ─────────────────────────────────────────────────────────────
  routes:

    # Route 1: Alert severity=critical → Slack #alerts-critical + Email
    - match:
        severity: critical
      receiver: 'slack-critical'
      # continue: true → sau khi gửi Slack vẫn tiếp tục check route tiếp theo
      # Dùng khi muốn gửi CẢ Slack và Email cho critical
      continue: true

    # Route 2: Alert severity=critical → Email (sau khi đã gửi Slack ở Route 1)
    - match:
        severity: critical
      receiver: 'email-critical'

    # Route 3: Alert severity=warning → Slack #alerts-warning (không gửi email)
    - match:
        severity: warning
      receiver: 'slack-warning'

    # Route 4: Alert từ job=nginx → kênh riêng cho infra team
    - match:
        job: nginx
      receiver: 'slack-infra'

    # Route 5: Alert từ node-exporter trên DB VM → ưu tiên cao hơn
    - match_re:
        instance: '192\.168\.1\.(38|39):.*'   # match .38 hoặc .39 (DB VM)
      receiver: 'slack-critical'
      group_wait: 10s    # gửi nhanh hơn vì DB down = tất cả bị ảnh hưởng

# ─── INHIBITION — tắt alert phụ khi có alert chính ───────────────────────────
#
# Ví dụ: khi VM .38 down (TargetDown) → Prometheus sẽ alert thêm:
#   - LowMemory (vì không lấy được metric → giá trị 0 → < 20%)
#   - HighCPUUsage, v.v.
# → Rác. Inhibition: nếu có TargetDown thì tắt các alert khác trên cùng instance đó.

inhibit_rules:

  # Nếu có TargetDown trên instance X → tắt mọi alert khác trên instance X
  - source_match:
      alertname: 'TargetDown'    # alert "nguồn" kích hoạt inhibition
    target_match_re:
      alertname: '.+'            # tắt TẤT CẢ alert khác
    equal: ['instance']          # chỉ inhibit khi source và target có cùng label "instance"

  # Nếu có alert critical trên instance X → tắt warning trên cùng instance
  - source_match:
      severity: 'critical'
    target_match:
      severity: 'warning'
    equal: ['alertname', 'instance']

# ─── RECEIVERS — kênh gửi thông báo ──────────────────────────────────────────

receivers:

  # ─── Email thông thường (mọi alert) ─────────────────────────────────────────
  - name: 'email-lab'
    email_configs:
      - to: 'team-lab@company.com'
        headers:
          Subject: '[LAB ALERT] {{ .GroupLabels.alertname }} — {{ .GroupLabels.severity }}'
        html: '{{ template "email.html" . }}'   # dùng template tùy chỉnh
        send_resolved: true   # gửi email khi alert resolved (hết FIRING)

  # ─── Email critical ──────────────────────────────────────────────────────────
  - name: 'email-critical'
    email_configs:
      - to: 'oncall@company.com, team-lead@company.com'
        headers:
          Subject: '🚨 [CRITICAL] {{ .GroupLabels.alertname }} cần xử lý ngay!'
        html: '{{ template "email.html" . }}'
        send_resolved: true

  # ─── Slack #alerts-critical ──────────────────────────────────────────────────
  - name: 'slack-critical'
    slack_configs:
      - channel: '#alerts-critical'
        # username và icon xuất hiện trong Slack như là "user" gửi message
        username: 'Alertmanager'
        icon_emoji: ':fire:'
        # color: màu sidebar của Slack message
        # .Status: "firing" hoặc "resolved"
        color: '{{ if eq .Status "firing" }}danger{{ else }}good{{ end }}'
        # title: dòng đầu to đậm
        title: '{{ if eq .Status "firing" }}🔴 FIRING{{ else }}✅ RESOLVED{{ end }} — {{ .GroupLabels.alertname }}'
        # text: nội dung chính — dùng Go template
        text: |
          {{ range .Alerts }}
          *Instance:* {{ .Labels.instance }}
          *Severity:* {{ .Labels.severity }}
          *Summary:* {{ .Annotations.summary }}
          *Description:* {{ .Annotations.description }}
          *Started:* {{ .StartsAt.Format "2006-01-02 15:04:05" }}
          {{ end }}
        # Link đến Prometheus hoặc Grafana để xem detail
        title_link: 'http://192.168.1.51:9090/alerts'
        send_resolved: true
        # Cho phép mention user/group khi firing
        # actions:
        #   - type: button
        #     text: "View in Grafana"
        #     url: 'http://192.168.1.51:3000'

  # ─── Slack #alerts-warning ──────────────────────────────────────────────────
  - name: 'slack-warning'
    slack_configs:
      - channel: '#alerts-warning'
        username: 'Alertmanager'
        icon_emoji: ':warning:'
        color: 'warning'
        title: '⚠️ WARNING — {{ .GroupLabels.alertname }}'
        text: |
          {{ range .Alerts }}
          *Instance:* {{ .Labels.instance }}
          *Summary:* {{ .Annotations.summary }}
          *Description:* {{ .Annotations.description }}
          {{ end }}
        send_resolved: true

  # ─── Slack #infra ────────────────────────────────────────────────────────────
  - name: 'slack-infra'
    slack_configs:
      - channel: '#infra-alerts'
        username: 'Alertmanager'
        icon_emoji: ':nginx:'
        color: '{{ if eq .Status "firing" }}warning{{ else }}good{{ end }}'
        title: 'Nginx Alert — {{ .GroupLabels.alertname }}'
        text: |
          {{ range .Alerts }}
          *Instance:* {{ .Labels.instance }}
          *Summary:* {{ .Annotations.summary }}
          {{ end }}
        send_resolved: true

  # ─── Webhook — tích hợp với bất kỳ hệ thống nào ────────────────────────────
  # Alertmanager gửi POST request với JSON body đến URL này
  # Dùng để tích hợp với: PagerDuty, Teams, Telegram bot, custom handler
  - name: 'webhook-default'
    webhook_configs:
      - url: 'http://192.168.1.51:5001/alerts'   # custom webhook handler
        send_resolved: true
        # max_alerts: 0 = gửi tất cả alert trong batch, không giới hạn
        max_alerts: 0
        # http_config: auth header nếu webhook yêu cầu
        # http_config:
        #   bearer_token: 'your-webhook-token'
EOF
```

### Bước AM.3 — Tạo Email Template tùy chỉnh

```bash
cat > ~/monitoring/alertmanager/templates/email.tmpl << 'EOF'
{{ define "email.html" }}
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 20px; }
    .container { background: white; border-radius: 8px; padding: 20px; max-width: 800px; margin: 0 auto; }
    .header { padding: 15px; border-radius: 5px; margin-bottom: 20px; }
    .firing  { background: #fde8e8; border-left: 4px solid #dc3545; }
    .resolved{ background: #e8f5e9; border-left: 4px solid #28a745; }
    .alert-block { border: 1px solid #ddd; border-radius: 5px; padding: 15px; margin: 10px 0; }
    .label { font-weight: bold; color: #555; min-width: 120px; display: inline-block; }
    .critical { color: #dc3545; font-weight: bold; }
    .warning  { color: #fd7e14; font-weight: bold; }
    .info     { color: #0d6efd; font-weight: bold; }
    table { width: 100%; border-collapse: collapse; }
    td { padding: 6px 10px; border-bottom: 1px solid #eee; }
    td:first-child { font-weight: bold; color: #555; width: 130px; }
  </style>
</head>
<body>
  <div class="container">

    <div class="header {{ if eq .Status "firing" }}firing{{ else }}resolved{{ end }}">
      <h2>
        {{ if eq .Status "firing" }}🔴 ALERT FIRING{{ else }}✅ ALERT RESOLVED{{ end }}
      </h2>
      <p>
        <strong>Group:</strong> {{ .GroupLabels.alertname }}<br>
        <strong>Severity:</strong>
        <span class="{{ .GroupLabels.severity }}">{{ .GroupLabels.severity | toUpper }}</span><br>
        <strong>Total alerts:</strong> {{ len .Alerts }}<br>
        <strong>Time:</strong> {{ .Alerts | first | .StartsAt.Format "2006-01-02 15:04:05 UTC" }}
      </p>
    </div>

    {{ range .Alerts }}
    <div class="alert-block">
      <table>
        <tr><td>Instance</td><td>{{ .Labels.instance }}</td></tr>
        <tr><td>Job</td><td>{{ .Labels.job }}</td></tr>
        <tr><td>Severity</td><td><span class="{{ .Labels.severity }}">{{ .Labels.severity }}</span></td></tr>
        <tr><td>Summary</td><td>{{ .Annotations.summary }}</td></tr>
        <tr><td>Description</td><td>{{ .Annotations.description }}</td></tr>
        <tr><td>Started</td><td>{{ .StartsAt.Format "2006-01-02 15:04:05" }} UTC</td></tr>
        {{ if .EndsAt }}
        <tr><td>Resolved</td><td>{{ .EndsAt.Format "2006-01-02 15:04:05" }} UTC</td></tr>
        {{ end }}
      </table>
    </div>
    {{ end }}

    <hr>
    <p style="color: #999; font-size: 12px;">
      Sent by Alertmanager — Lab Monitoring Stack<br>
      <a href="http://192.168.1.51:9090/alerts">View in Prometheus</a> |
      <a href="http://192.168.1.51:3000">View in Grafana</a> |
      <a href="http://192.168.1.51:9093">Alertmanager UI</a>
    </p>

  </div>
</body>
</html>
{{ end }}
EOF
```

### Bước AM.4 — Thêm Alertmanager vào docker-compose.yml

Cập nhật `~/monitoring/docker-compose.yml` — thêm service `alertmanager` vào khối `services:`:

```yaml
# Thêm vào ~/monitoring/docker-compose.yml, trong phần services:

  # ─── ALERTMANAGER — xử lý routing và gửi thông báo ──────────────────────────
  alertmanager:
    image: prom/alertmanager:latest
    container_name: alertmanager
    ports:
      - "9093:9093"     # Web UI — truy cập: http://192.168.1.51:9093
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'
      - '--storage.path=/alertmanager'              # lưu state (silence, nflog)
      - '--web.external-url=http://192.168.1.51:9093'  # URL hiển thị trong link email
      - '--cluster.advertise-address=0.0.0.0:9094'  # clustering port (dùng khi chạy nhiều AM)
      # Reload config không cần restart:
      # curl -X POST http://192.168.1.51:9093/-/reload
      - '--web.enable-lifecycle'
    volumes:
      - ./alertmanager/alertmanager.yml:/etc/alertmanager/alertmanager.yml:ro
      - ./alertmanager/templates:/etc/alertmanager/templates:ro
      - alertmanager-data:/alertmanager    # lưu silences và notification log
    networks:
      - monitoring
    depends_on:
      - prometheus
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:9093/-/healthy"]
      interval: 15s
      timeout: 5s
      retries: 3
```

Và thêm volume mới vào cuối khối `volumes:`:

```yaml
volumes:
  prometheus-data:
  grafana-data:
  influxdb-data:
  influxdb-config:
  alertmanager-data:    # ← thêm dòng này
```

Sau đó restart stack để Alertmanager được thêm vào:

```bash
cd ~/monitoring
docker compose up -d alertmanager
docker compose ps
# Kỳ vọng: alertmanager Up, healthy
```

### Bước AM.5 — Verify Alertmanager hoạt động

```bash
# 1. Kiểm tra Alertmanager UI
# http://192.168.1.51:9093
# → Thấy tab: Alerts / Silences / Status / Receivers

# 2. Kiểm tra Prometheus đã biết Alertmanager
# http://192.168.1.51:9090/status
# → Phần "Alertmanagers" phải hiển thị: http://alertmanager:9093/api/v2/alerts

# 3. Kiểm tra qua API
curl http://192.168.1.51:9093/api/v2/status | jq .
# → uptime, config, cluster info

# 4. Kiểm tra routing config
curl http://192.168.1.51:9093/api/v2/receivers | jq '.[] | .name'
# Kỳ vọng: thấy tên các receiver đã cấu hình

# 5. Test gửi alert giả lập (amtool)
docker exec alertmanager amtool \
  --alertmanager.url=http://localhost:9093 \
  alert add \
  alertname=TestAlert \
  severity=warning \
  instance=192.168.1.40:9100 \
  job=node-exporter \
  --annotation=summary="Test alert từ amtool" \
  --annotation=description="Alert thử nghiệm, bỏ qua"
# Sau đó vào UI → Alerts → thấy TestAlert đang ACTIVE

# Expire alert test
docker exec alertmanager amtool \
  --alertmanager.url=http://localhost:9093 \
  alert query
# Thấy list alert đang active

# Xóa alert test (để nó tự expire sau vài phút)
```

### Bước AM.6 — Quản lý Silence (tắt alert trong maintenance)

**Silence** = tắt alert tạm thời mà không cần thay đổi rule. Dùng khi:
- Nâng cấp server (biết sẽ có downtime)
- Test load (CPU cao có chủ ý)
- Ngày cuối tuần, giờ thấp điểm không cần notify

```bash
# Tạo silence qua amtool CLI — tắt mọi alert trên .40 trong 2 giờ
docker exec alertmanager amtool \
  --alertmanager.url=http://localhost:9093 \
  silence add \
  instance="192.168.1.40:9100" \
  --duration=2h \
  --comment="Maintenance window: upgrade Docker on .40"

# Liệt kê silences đang active
docker exec alertmanager amtool \
  --alertmanager.url=http://localhost:9093 \
  silence query

# Expire (hủy) silence trước hạn
docker exec alertmanager amtool \
  --alertmanager.url=http://localhost:9093 \
  silence expire <SILENCE_ID>
```

Hoặc dùng **Alertmanager UI** (http://192.168.1.51:9093):
```
→ Silences → New Silence
  Matchers: instance = 192.168.1.40:9100
  Duration: 2h
  Creator: your-name
  Comment: Maintenance window
  → Create
```

### Bước AM.7 — Viết alerting rules

Rules chia làm 2 file: `basic.yml` cho host alerts chung, `swarm.yml` cho service-level alerts.

**File 1 — basic.yml (CPU, RAM, Disk, TargetDown cho mọi VM):**

```bash
cat > ~/monitoring/prometheus/rules/basic.yml << 'EOF'
# rules/basic.yml
# Prometheus evaluate mỗi 15s — khi điều kiện đúng liên tục quá `for` → FIRING

groups:
  - name: host-alerts
    rules:

      # CPU > 80% liên tục 5 phút
      # "for: 5m" tránh alert nhảy khi chỉ spike ngắn 1-2s
      - alert: HighCPUUsage
        expr: 100 - (avg by(instance) (rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100) > 80
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "CPU cao trên {{ $labels.instance }}"
          description: "CPU sử dụng {{ $value | printf \"%.1f\" }}% trong 5 phút qua"

      # RAM còn < 20%
      - alert: LowMemory
        expr: (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes) * 100 < 20
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "RAM thấp trên {{ $labels.instance }}"
          description: "RAM còn lại {{ $value | printf \"%.1f\" }}%"

      # Disk > 85%
      - alert: HighDiskUsage
        expr: (1 - node_filesystem_free_bytes{fstype!="tmpfs"} / node_filesystem_size_bytes{fstype!="tmpfs"}) * 100 > 85
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Disk đầy trên {{ $labels.instance }}"
          description: "Disk {{ $labels.mountpoint }} đầy {{ $value | printf \"%.1f\" }}%"

      # Target down — up == 0 nghĩa là Prometheus không reach được target
      - alert: TargetDown
        expr: up == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Target down: {{ $labels.instance }}"
          description: "{{ $labels.job }} trên {{ $labels.instance }} không respond"
EOF
```

**File 2 — swarm.yml (Swarm service, Nginx, DB alerts):**

```bash
cat > ~/monitoring/prometheus/rules/swarm.yml << 'EOF'
groups:
  - name: swarm-alerts
    rules:

      # Alert khi Swarm service có replica thấp hơn mong đợi
      # Metric từ cAdvisor: container_last_seen cho phép tính số container đang chạy
      - alert: SwarmServiceUnhealthy
        expr: |
          count by (container_label_com_docker_swarm_service_name) (
            container_last_seen{
              container_label_com_docker_swarm_service_name!="",
              container_label_com_docker_swarm_task_state="running"
            }
          ) < 2
        for: 3m
        labels:
          severity: critical
        annotations:
          summary: "Swarm service {{ $labels.container_label_com_docker_swarm_service_name }} thiếu replica"
          description: "Chỉ còn {{ $value }} replica đang chạy, cần ít nhất 2"

      # Alert khi container restart nhiều lần (crash loop)
      - alert: ContainerCrashLoop
        expr: |
          rate(container_last_seen{name!=""}[5m]) == 0
          and
          changes(container_last_seen{name!=""}[10m]) > 3
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Container {{ $labels.name }} đang crash loop"
          description: "Container restart {{ $value }} lần trong 10 phút qua"

      # Alert khi container dùng quá nhiều memory (gần limit)
      - alert: ContainerMemoryNearLimit
        expr: |
          (container_memory_usage_bytes{name!=""}
          / container_spec_memory_limit_bytes{name!=""}) * 100 > 85
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Container {{ $labels.name }} gần đầy memory"
          description: "Đang dùng {{ $value | printf \"%.1f\" }}% memory limit"

  - name: nginx-alerts
    rules:

      # Alert khi Nginx down (nginx_up = 0 từ nginx exporter)
      - alert: NginxDown
        expr: nginx_up == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Nginx down trên {{ $labels.instance }}"
          description: "Nginx load balancer không respond — toàn bộ traffic bị ảnh hưởng"

      # Alert khi error rate cao (5xx responses)
      # Dùng nginx access log metric nếu có log exporter
      # Với nginx stub_status (basic), dùng connection anomaly thay thế
      - alert: NginxHighActiveConnections
        expr: nginx_connections_active > 500
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Nginx active connections cao: {{ $value }}"
          description: "Có thể đang bị DDoS hoặc backend chậm — kiểm tra Grafana"

  - name: database-alerts
    rules:

      # Alert khi không scrape được DB node (DB down)
      - alert: DatabaseDown
        expr: up{job="node-exporter", instance=~"192\\.168\\.1\\.(38|39):.*"} == 0
        for: 1m
        labels:
          severity: critical
          team: dba
        annotations:
          summary: "Database VM {{ $labels.instance }} không reach được"
          description: "Node exporter trên DB VM không respond — có thể DB hoặc VM đã down"

      # Alert khi disk của DB VM > 80% (DB cần thêm headroom hơn app server)
      - alert: DatabaseDiskCritical
        expr: |
          (1 - node_filesystem_free_bytes{
            fstype!="tmpfs",
            instance=~"192\\.168\\.1\\.(38|39):.*"
          } / node_filesystem_size_bytes{
            fstype!="tmpfs",
            instance=~"192\\.168\\.1\\.(38|39):.*"
          }) * 100 > 80
        for: 2m
        labels:
          severity: critical
          team: dba
        annotations:
          summary: "Disk DB {{ $labels.instance }} sắp đầy"
          description: "Disk {{ $labels.mountpoint }} đã dùng {{ $value | printf \"%.1f\" }}% — DB có thể dừng ghi"
EOF
```

### Bước AM.8 — Reload config không cần restart

```bash
# Reload Alertmanager config (sau khi sửa alertmanager.yml)
curl -X POST http://192.168.1.51:9093/-/reload
# Hoặc:
docker exec alertmanager kill -HUP 1

# Reload Prometheus config (sau khi sửa prometheus.yml hoặc rules)
curl -X POST http://192.168.1.51:9090/-/reload

# Verify config hợp lệ trước khi reload
docker exec alertmanager amtool \
  check-config /etc/alertmanager/alertmanager.yml

docker run --rm \
  -v ~/monitoring/prometheus:/etc/prometheus \
  prom/prometheus:latest \
  promtool check config /etc/prometheus/prometheus.yml

docker run --rm \
  -v ~/monitoring/prometheus/rules:/etc/prometheus/rules \
  prom/prometheus:latest \
  promtool check rules /etc/prometheus/rules/*.yml
```

### Luồng hoạt động end-to-end khi có sự cố

```
Ví dụ: VM 192.168.1.38 (DB Primary) chết lúc 14:35

T+00s  VM .38 down
T+15s  Prometheus scrape lần tiếp theo → node_exporter .38 không respond
       → target health = DOWN
       → rule "TargetDown" bắt đầu đếm thời gian (for: 1m)
T+75s  Rule "TargetDown" đã PENDING 1 phút → chuyển sang FIRING
       Prometheus gửi alert đến Alertmanager:9093
T+90s  Alertmanager nhận alert:
       → grouping: chờ group_wait=30s để gom alert liên quan
T+120s Alertmanager gửi:
       → Slack #alerts-critical: 🔴 FIRING — TargetDown (DB down .38)
       → Email oncall@company.com (vì severity=critical + route DB VM)
       → Inhibition kích hoạt: tắt LowMemory, HighCPUUsage trên .38

T+4h   Nếu .38 vẫn down → repeat_interval kích hoạt → gửi lại email nhắc nhở

T+X    Team fix xong, .38 up lại
T+X+15s Prometheus scrape thành công → target UP
       → rule "TargetDown" không còn thỏa → chuyển về INACTIVE
       → Alertmanager chờ resolve_timeout=5m
T+X+5m Alertmanager gửi "✅ RESOLVED — TargetDown" vào Slack + Email
```

### Tổng hợp: Alertmanager vs không có Alertmanager

| Tình huống | Không có Alertmanager | Có Alertmanager |
|---|---|---|
| 8 VM down cùng lúc | 8 × mỗi 15s = spam 32 email/phút | 1 message tổng sau 30s (grouping) |
| DB down → cascade alert | Nhận 10+ alert rác | Chỉ nhận "DB down", rest bị inhibit |
| Maintenance 2h | Phải tắt tạm rule | Tạo silence 2h, rule vẫn evaluate |
| Alert critical vs warning | Gửi chung 1 nơi | Critical → oncall, warning → Slack |
| Đêm khuya | Gửi bình thường | Có thể config time_intervals để delay |

---

### Bước MON.5 — Tạo Grafana provisioning (datasource tự động)

Thay vì vào UI tay thêm datasource mỗi lần restart, Grafana có thể tự load datasource từ file YAML khi start. Đây gọi là **provisioning** — cấu hình as code.

```bash
cat > ~/monitoring/grafana/provisioning/datasources/datasources.yml << 'EOF'
# datasources.yml — Grafana đọc file này khi start và tự thêm datasource
apiVersion: 1

datasources:

  # Prometheus — nguồn metric chính (app metrics, host metrics)
  - name: Prometheus
    type: prometheus
    access: proxy      # proxy = Grafana server gọi Prometheus, không phải browser
                       # Ngược lại là "direct" = browser gọi thẳng (không dùng nếu browser và Prometheus khác mạng)
    url: http://prometheus:9090   # dùng tên service trong docker network, không phải IP
    isDefault: true    # đây là datasource mặc định khi tạo dashboard mới
    editable: true     # cho phép sửa trong UI (nếu false = chỉ đọc, không sửa được)

  # InfluxDB v2 — nguồn k6 load test results
  - name: InfluxDB-k6
    type: influxdb
    access: proxy
    url: http://influxdb:8086
    jsonData:
      version: Flux           # Flux = ngôn ngữ query của InfluxDB v2
                              # Nếu dùng InfluxQL thì đổi thành "InfluxQL"
      organization: lab-org   # phải khớp với org tạo ở bước MON.7
      defaultBucket: k6-authdemo   # bucket mặc định khi query
      tlsSkipVerify: true
    secureJsonData:
      token: "${INFLUXDB_TOKEN}"   # token sẽ inject từ env var khi container start
    editable: true
EOF
```

```bash
# File này nói với Grafana: "Tôi sẽ quản lý dashboard từ file, không cần lưu trong DB của mình"
cat > ~/monitoring/grafana/provisioning/dashboards/dashboards.yml << 'EOF'
apiVersion: 1

providers:
  - name: 'Lab Dashboards'
    orgId: 1
    folder: 'Lab'          # tên folder trong Grafana UI
    type: file
    disableDeletion: false  # cho phép xóa dashboard qua UI
    updateIntervalSeconds: 30   # cứ 30s kiểm tra file có thay đổi không, nếu có thì reload
    allowUiUpdates: true        # cho phép sửa dashboard qua UI (thay đổi sẽ ghi đè file)
    options:
      path: /var/lib/grafana/dashboards   # thư mục trong container chứa file JSON
EOF
```

---

### Bước MON.6 — Tạo docker-compose cho Monitoring Stack

```bash
cat > ~/monitoring/docker-compose.yml << 'EOF'
# docker-compose.yml cho monitoring stack trên 192.168.1.51
# Tất cả service trong cùng network "monitoring" để gọi nhau bằng tên service

services:

  # ─── PROMETHEUS ───────────────────────────────────────────────────────────
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"     # UI và API của Prometheus — truy cập: http://192.168.1.51:9090
    command:
      # Chỉ định file config (overwrite default location)
      - '--config.file=/etc/prometheus/prometheus.yml'
      # Thư mục lưu time-series data (TSDB)
      - '--storage.tsdb.path=/prometheus'
      # Giữ data 30 ngày (mặc định 15 ngày)
      # Tính toán: ~2MB/target/ngày → 8 target × 30 ngày = ~480MB
      - '--storage.tsdb.retention.time=30d'
      # Cho phép reload config không cần restart (dùng: curl -X POST http://localhost:9090/-/reload)
      - '--web.enable-lifecycle'
      # Cho phép remote-write API (InfluxDB có thể push vào Prometheus nếu muốn)
      # - '--web.enable-remote-write-receiver'
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - ./prometheus/rules:/etc/prometheus/rules:ro   # alerting rules
      - prometheus-data:/prometheus                   # lưu TSDB data
    networks:
      - monitoring
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:9090/-/healthy"]
      interval: 15s
      timeout: 5s
      retries: 3

  # ─── GRAFANA ──────────────────────────────────────────────────────────────
  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"     # Web UI — truy cập: http://192.168.1.51:3000
    environment:
      # Admin credentials lần đầu đăng nhập
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=Admin@Grafana2025
      # Tắt bắt buộc đổi password lần đầu (tiện cho lab)
      - GF_USERS_ALLOW_SIGN_UP=false
      # Token InfluxDB — inject vào để provisioning datasource dùng
      - INFLUXDB_TOKEN=${INFLUXDB_TOKEN:-lab-influxdb-token-placeholder}
      # Cho phép embed dashboard vào iframe (optional — nếu muốn nhúng vào portal nội bộ)
      # - GF_SECURITY_ALLOW_EMBEDDING=true
      # Plugin cài thêm khi start (tách bằng ;)
      # - GF_INSTALL_PLUGINS=grafana-clock-panel;grafana-simple-json-datasource
    volumes:
      - grafana-data:/var/lib/grafana                              # Grafana DB (sqlite), sessions, plugins
      - ./grafana/provisioning:/etc/grafana/provisioning:ro       # auto-provision datasource + dashboard
      - ./grafana/dashboards:/var/lib/grafana/dashboards:rw       # file JSON của dashboard
    networks:
      - monitoring
    depends_on:
      - prometheus
      - influxdb
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:3000/api/health"]
      interval: 15s
      timeout: 5s
      retries: 3

  # ─── INFLUXDB ─────────────────────────────────────────────────────────────
  influxdb:
    image: influxdb:2.7
    container_name: influxdb
    ports:
      - "8086:8086"     # HTTP API + UI — truy cập: http://192.168.1.51:8086
    environment:
      # Setup ban đầu — InfluxDB v2 cần setup lần đầu
      # Nếu volume đã có data thì các biến DOCKER_INFLUXDB_INIT_* bị bỏ qua
      - DOCKER_INFLUXDB_INIT_MODE=setup
      - DOCKER_INFLUXDB_INIT_USERNAME=influx-admin
      - DOCKER_INFLUXDB_INIT_PASSWORD=Admin@InfluxDB2025
      - DOCKER_INFLUXDB_INIT_ORG=lab-org
      - DOCKER_INFLUXDB_INIT_BUCKET=k6-authdemo    # bucket đầu tiên — cho project authdemo
      - DOCKER_INFLUXDB_INIT_RETENTION=30d         # giữ data 30 ngày
      - DOCKER_INFLUXDB_INIT_ADMIN_TOKEN=lab-influxdb-super-token-change-in-prod
                  # Token này dùng để tạo bucket mới và grant quyền
                  # THAY ĐỔI trong production — dùng giá trị ngẫu nhiên dài
    volumes:
      - influxdb-data:/var/lib/influxdb2    # lưu data InfluxDB
      - influxdb-config:/etc/influxdb2      # lưu config InfluxDB
    networks:
      - monitoring
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "influx", "ping"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 30s

  # ─── cADVISOR — container metric của chính VM monitoring ──────────────────
  # cAdvisor = Container Advisor: đọc /sys và /var/run/docker.sock để lấy metric container
  # Cần chạy cADVISOR trên TỪNG VM Docker (không chỉ trên monitoring VM)
  # Trên monitoring VM này: theo dõi Prometheus/Grafana/InfluxDB container
  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    container_name: cadvisor
    ports:
      - "8081:8080"     # UI và /metrics endpoint
                        # Port 8081 (host) vì 8080 thường bị dùng bởi app khác
    volumes:
      - /:/rootfs:ro                    # filesystem của host — để cAdvisor đọc cgroup
      - /var/run:/var/run:ro            # docker.sock
      - /sys:/sys:ro                    # kernel info
      - /var/lib/docker:/var/lib/docker:ro   # Docker data
    devices:
      - /dev/kmsg:/dev/kmsg            # kernel message buffer — cần cho một số metric
    privileged: true                   # cAdvisor cần quyền cao để đọc cgroup
    networks:
      - monitoring
    restart: unless-stopped

  # ─── NODE EXPORTER — host metric của chính VM monitoring ──────────────────
  # Node Exporter lấy metric từ /proc và /sys của host
  # Không dùng Docker exec hay SSH — đọc thẳng kernel interface
  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    ports:
      - "9100:9100"
    command:
      # Không export metric của các filesystem tạm thời
      - '--path.procfs=/host/proc'
      - '--path.rootfs=/rootfs'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    network_mode: host    # host network: node-exporter dùng network của host, không qua bridge
                          # Lý do: để Prometheus scrape đến đúng IP của host, không phải IP container
    pid: host             # chia sẻ PID namespace với host — cần để đọc process metric
    restart: unless-stopped

  # ─── NGINX EXPORTER — metric của Nginx stub_status ────────────────────────
  # Chú ý: exporter này KHÔNG chạy trên monitoring VM
  # Nó phải chạy trên VM .37 và .43 (cùng VM với Nginx)
  # Đây là section tham khảo — xem Bước MON.8

networks:
  monitoring:
    driver: bridge

volumes:
  prometheus-data:    # time-series database của Prometheus (~480MB cho 8 targets x 30 ngày)
  grafana-data:       # Grafana sqlite DB, sessions, installed plugins
  influxdb-data:      # InfluxDB data files
  influxdb-config:    # InfluxDB config files
EOF
```

```bash
# Tạo thư mục dashboards (Grafana sẽ mount vào đây)
mkdir -p ~/monitoring/grafana/dashboards

# Khởi động stack
cd ~/monitoring
docker compose up -d

# Theo dõi quá trình start
docker compose ps
docker compose logs -f --tail=20
```

---

### Bước MON.7 — Cài Node Exporter và cAdvisor trên TẤT CẢ các VM còn lại

Node Exporter phải chạy trên **mỗi VM** mà bạn muốn theo dõi. Làm nhanh nhất bằng script:

```bash
# Tạo script cài trên từng VM — chạy script này trên .37, .38, .39, .40, .41, .42
# (Không cần chạy trên .51 vì đã có trong docker-compose trên)

cat > ~/deploy-exporters.sh << 'EOF'
#!/bin/bash
# Script này chạy trên BẤT KỲ VM nào cần export metric
# Chạy bằng: bash deploy-exporters.sh

echo "=== Deploying Node Exporter ==="
# node_exporter dùng host network để bind thẳng port 9100 trên host IP
docker run -d \
  --name node-exporter \
  --restart unless-stopped \
  --network host \           # host network: bind thẳng 9100 trên host IP
  --pid host \               # đọc được process list của host
  -v /:/rootfs:ro \
  -v /proc:/host/proc:ro \
  -v /sys:/host/sys:ro \
  prom/node-exporter:latest \
  --path.procfs=/host/proc \
  --path.rootfs=/rootfs \
  --path.sysfs=/host/sys \
  --collector.filesystem.mount-points-exclude="^/(sys|proc|dev|host|etc)($|/)"

echo "=== Deploying cAdvisor ==="
docker run -d \
  --name cadvisor \
  --restart unless-stopped \
  --privileged \             # cần để đọc cgroup (CPU/memory của container)
  -p 8081:8080 \             # map sang 8081 tránh conflict với app
  -v /:/rootfs:ro \
  -v /var/run:/var/run:ro \
  -v /sys:/sys:ro \
  -v /var/lib/docker:/var/lib/docker:ro \
  --device /dev/kmsg:/dev/kmsg \
  gcr.io/cadvisor/cadvisor:latest

echo "=== Verify ==="
sleep 3
# Test Node Exporter có trả metric không
curl -s http://localhost:9100/metrics | head -5
echo "Node Exporter: OK nếu thấy metric HELP/TYPE"
# Test cAdvisor
curl -s http://localhost:8081/metrics | head -5
echo "cAdvisor: OK nếu thấy metric HELP/TYPE"
EOF

chmod +x ~/deploy-exporters.sh
```

```bash
# Chạy script trên từng VM (SSH vào từng máy)
# Trên Swarm Manager (.40):
ssh bank@192.168.1.40 "bash <(curl -s http://192.168.1.51/deploy-exporters.sh)"

# Hoặc đơn giản hơn: SSH vào từng VM và chạy thủ công
for VM_IP in 192.168.1.37 192.168.1.38 192.168.1.39 192.168.1.40 192.168.1.41 192.168.1.42; do
    echo "=== Deploying to $VM_IP ==="
    scp ~/deploy-exporters.sh bank@$VM_IP:~/
    ssh bank@$VM_IP "bash ~/deploy-exporters.sh"
done
```

**Mở firewall trên TẤT CẢ VM để Prometheus scrape được:**

```bash
# Chạy trên MỖI VM (không phải monitoring VM — monitoring VM là nơi Prometheus đứng)
# Prometheus cần vào các port này để lấy metric

sudo ufw allow from 192.168.1.51 to any port 9100   # Node Exporter — chỉ cho monitoring VM vào
sudo ufw allow from 192.168.1.51 to any port 8081   # cAdvisor
sudo ufw reload

# Giải thích "from 192.168.1.51": giới hạn chỉ monitoring VM mới scrape được
# Không mở public vì metric chứa thông tin nhạy cảm (process list, disk path...)
```

---

### Bước MON.8 — Cài Nginx Prometheus Exporter trên VM .37 và .43

Nginx Exporter đọc endpoint `/nginx-status` (đã cấu hình ở Part 1, Bước 1.3) và convert sang format Prometheus.

```bash
# Chạy trên 192.168.1.37 (Nginx Master)
docker run -d \
  --name nginx-exporter \
  --restart unless-stopped \
  -p 9113:9113 \
  nginx/nginx-prometheus-exporter:latest \
  --nginx.scrape-uri="http://192.168.1.37:8080/nginx-status"
  # Exporter gọi endpoint này mỗi khi Prometheus scrape
  # nginx-status là stub_status endpoint — đã bật trong nginx.conf Part 1

# Verify: exporter chuyển stub_status sang Prometheus format
curl http://localhost:9113/metrics
# Kỳ vọng: thấy các metric như:
# nginx_connections_active 5
# nginx_connections_reading 0
# nginx_http_requests_total 1234
# nginx_up 1  ← quan trọng: 1=Nginx đang chạy, 0=Nginx chết

# Chạy tương tự trên 192.168.1.43 (Nginx Backup) nếu đã tạo
```

```bash
# Mở firewall trên VM .37 và .43
sudo ufw allow from 192.168.1.51 to any port 9113
sudo ufw reload
```

---

### Bước MON.9 — Verify Prometheus đang scrape

```bash
# Truy cập Prometheus UI
# http://192.168.1.51:9090

# Kiểm tra targets:
# Vào: Status → Targets
# Kỳ vọng: tất cả target đều "UP" (màu xanh)
# Nếu target "DOWN" (màu đỏ) → xem cột Error để biết lý do

# Hoặc query qua API:
curl http://192.168.1.51:9090/api/v1/targets | jq '.data.activeTargets[] | {job: .labels.job, instance: .labels.instance, health: .health}'

# Query thử metric:
# http://192.168.1.51:9090/graph
# Nhập vào ô Expression:
#   node_cpu_seconds_total    → thấy raw CPU counter
#   rate(node_cpu_seconds_total{mode="idle"}[5m])   → CPU idle rate 5 phút
#   100 - (avg by(instance)(rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100)
#   → CPU usage % của từng VM
```

---

### Bước MON.10 — Cấu hình Grafana và import Dashboard

```bash
# Truy cập Grafana
# http://192.168.1.51:3000
# User: admin
# Pass: Admin@Grafana2025

# Datasource đã được auto-provision (bước MON.5) → không cần add tay
# Vào: Connections → Data sources → Verify thấy "Prometheus" và "InfluxDB-k6"
```

**Import Dashboard có sẵn từ Grafana.com:**

```
Grafana.com có kho dashboard cộng đồng — không cần tự vẽ từ đầu.

1. Vào Grafana UI → Dashboards → Import

2. Node Exporter Full (ID: 1860)
   Nhập ID: 1860 → Load → Chọn datasource "Prometheus" → Import
   → Dashboard đầy đủ: CPU, RAM, Disk, Network của tất cả VM

3. Docker cAdvisor (ID: 14282)
   Nhập ID: 14282 → Load → Datasource: Prometheus → Import
   → Dashboard container: CPU/RAM per container, restart count

4. Nginx (ID: 9614)
   Nhập ID: 9614 → Load → Datasource: Prometheus → Import
   → Dashboard Nginx: requests/s, error rate, upstream latency

5. k6 Load Testing Results (ID: 2587)
   Nhập ID: 2587 → Load → Datasource: InfluxDB-k6 → Import
   → Dashboard k6: VU count, http_req_duration, error rate theo thời gian
```

---

### Bước MON.11 — Cấu hình InfluxDB cho k6

InfluxDB v2 cần tạo **bucket** (không gian lưu data) và **API token** (xác thực) cho k6.

```bash
# Cách 1: Dùng InfluxDB UI
# http://192.168.1.51:8086
# User: influx-admin / Admin@InfluxDB2025
#
# Load Data → Buckets → Create Bucket
#   Name: k6-authdemo
#   Data Retention: 30 days
#   → Create
#
# Data → API Tokens → Generate API Token → All Access Token
#   Description: k6-token
#   → Generate → Copy token (chỉ hiển thị 1 lần!)

# Cách 2: Dùng CLI bên trong container (nếu token đã được tạo qua DOCKER_INFLUXDB_INIT_ADMIN_TOKEN)
docker exec influxdb influx bucket create \
  --name k6-authdemo \
  --org lab-org \
  --retention 30d \
  --token lab-influxdb-super-token-change-in-prod
# Giải thích: tạo bucket mới để k6 push kết quả vào
# retention 30d: sau 30 ngày data tự xóa (tránh disk đầy)

# Tạo token riêng cho k6 (quyền write-only vào bucket k6-authdemo)
docker exec influxdb influx auth create \
  --org lab-org \
  --write-bucket k6-authdemo \
  --description "k6 write token" \
  --token lab-influxdb-super-token-change-in-prod
# Output: hiện token mới — COPY lại, dùng ở bước MON.12

# Verify bucket tồn tại
docker exec influxdb influx bucket list \
  --org lab-org \
  --token lab-influxdb-super-token-change-in-prod
```

**Bật v1 Compatibility API** để k6 dùng được InfluxDB v2 với output `--out influxdb` mặc định:

```bash
# k6 built-in InfluxDB output chỉ hỗ trợ InfluxDB v1 API (/write endpoint)
# InfluxDB v2 có v1 compatibility layer — cần map bucket v2 sang "database" v1

# Tạo DBRP mapping: "database k6db" = bucket "k6-authdemo" trong InfluxDB v2
docker exec influxdb influx v1 dbrp create \
  --db k6db \                        # tên "database" mà k6 sẽ gọi (v1 style)
  --rp autogen \                     # retention policy (v1 concept, bắt buộc điền)
  --bucket-id $(docker exec influxdb influx bucket list \
    --org lab-org \
    --token lab-influxdb-super-token-change-in-prod \
    --name k6-authdemo \
    --json | jq -r '.[0].id') \      # lấy ID của bucket k6-authdemo
  --default \                        # đây là default DBRP
  --token lab-influxdb-super-token-change-in-prod

# Tạo v1 credentials (username/password cho v1 API)
docker exec influxdb influx v1 auth create \
  --username k6-user \
  --password K6@InfluxDB2025 \
  --write-bucket k6-authdemo \
  --org lab-org \
  --token lab-influxdb-super-token-change-in-prod

# Giờ k6 có thể push vào InfluxDB v2 qua v1 API với:
# URL: http://192.168.1.51:8086/k6db
# Username: k6-user / Password: K6@InfluxDB2025
```

---

### Bước MON.12 — Cài k6 và chạy load test với monitoring

**Cài k6 trên máy chạy test (Windows hoặc Ubuntu):**

```bash
# Ubuntu:
sudo gpg -k
sudo gpg --no-default-keyring \
  --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 \
  --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6

# Windows (Chocolatey):
choco install k6

# Windows (Winget):
winget install k6 --source winget

# Verify
k6 version
```

**Viết k6 script cho AuthDemo API:**

```javascript
// k6-authdemo.js — load test script
// k6 dùng JavaScript ES6

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Counter, Trend } from 'k6/metrics';

// ─── Custom metrics ─────────────────────────────────────────────────────────
// k6 có built-in metrics (http_req_duration, http_reqs...)
// Thêm custom metric để track nghiệp vụ cụ thể
const loginSuccess = new Rate('login_success_rate');
const tokenRefreshCount = new Counter('token_refresh_count');
const apiLatency = new Trend('api_latency_custom', true);  // true = đơn vị ms

// ─── Test config ─────────────────────────────────────────────────────────────
export const options = {
  // Kịch bản: tăng dần từ 0 → 50 VU trong 2 phút, giữ 3 phút, giảm về 0
  stages: [
    { duration: '2m', target: 10  },   // ramp-up: tăng lên 10 VU trong 2 phút
    { duration: '3m', target: 10  },   // steady state: giữ 10 VU trong 3 phút
    { duration: '1m', target: 50  },   // spike: tăng lên 50 VU
    { duration: '2m', target: 50  },   // giữ spike
    { duration: '2m', target: 0   },   // ramp-down: giảm về 0
  ],

  // Threshold: test FAIL nếu các điều kiện này không đạt
  thresholds: {
    // 95th percentile của response time phải < 500ms
    'http_req_duration': ['p(95)<500'],
    // Error rate phải < 1%
    'http_req_failed': ['rate<0.01'],
    // Custom: login success rate > 99%
    'login_success_rate': ['rate>0.99'],
  },
};

const BASE_URL = 'http://192.168.1.36';   // VIP của Nginx Load Balancer

// ─── Setup: chạy 1 lần trước khi test bắt đầu ───────────────────────────────
export function setup() {
  // Có thể dùng để tạo test data, lấy token admin...
  console.log(`Load test target: ${BASE_URL}`);
  return { startTime: new Date().toISOString() };
}

// ─── Default function: chạy lặp lại bởi mỗi VU ──────────────────────────────
export default function(data) {

  // --- Kịch bản 1: Đăng nhập lấy token ---
  const loginPayload = JSON.stringify({
    grant_type: 'password',
    username: 'testuser@lab.local',
    password: 'Test@Lab2025!',
    client_id: 'authdemo-client',
    scope: 'openid profile',
  });

  const loginRes = http.post(
    `${BASE_URL}/connect/token`,
    loginPayload,
    {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      tags: { name: 'login' },   // tag để phân biệt trong dashboard
    }
  );

  // check: validate response — kết quả được tính vào loginSuccess metric
  const loginOk = check(loginRes, {
    'login status 200':       (r) => r.status === 200,
    'has access_token':       (r) => r.json('access_token') !== null,
    'response time < 300ms':  (r) => r.timings.duration < 300,
  });

  loginSuccess.add(loginOk);   // cộng vào custom Rate metric
  apiLatency.add(loginRes.timings.duration);  // cộng vào custom Trend metric

  if (!loginOk) {
    console.error(`Login failed: ${loginRes.status} ${loginRes.body}`);
    return;  // không tiếp tục nếu login fail
  }

  const token = loginRes.json('access_token');
  sleep(1);   // nghỉ 1 giây giữa các request — simulate user thực

  // --- Kịch bản 2: Gọi API với token ---
  const apiRes = http.get(
    `${BASE_URL}/api/userinfo`,
    {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/json',
      },
      tags: { name: 'api-userinfo' },
    }
  );

  check(apiRes, {
    'userinfo status 200': (r) => r.status === 200,
    'has sub claim':       (r) => r.json('sub') !== null,
  });

  sleep(0.5);
}

// ─── Teardown: chạy 1 lần sau khi test kết thúc ──────────────────────────────
export function teardown(data) {
  console.log(`Test started at: ${data.startTime}`);
  console.log(`Test ended at:   ${new Date().toISOString()}`);
}
```

**Chạy k6 với output vào InfluxDB:**

```bash
# Chạy k6 và push kết quả vào InfluxDB để Grafana visualize realtime
K6_INFLUXDB_USERNAME=k6-user \
K6_INFLUXDB_PASSWORD=K6@InfluxDB2025 \
k6 run \
  --out influxdb=http://192.168.1.51:8086/k6db \
  k6-authdemo.js

# Giải thích:
# --out influxdb=...: output kết quả sang InfluxDB (thay vì chỉ hiển thị trên terminal)
# http://192.168.1.51:8086/k6db: URL của InfluxDB, /k6db là tên database (v1 compat)
# K6_INFLUXDB_USERNAME/PASSWORD: credentials v1 đã tạo ở bước MON.11
```

```bash
# Trong lúc k6 chạy, mở Grafana → Dashboard "k6 Load Testing Results"
# http://192.168.1.51:3000
# Sẽ thấy realtime:
#   - Virtual Users (VU) đang chạy
#   - Requests per second
#   - Response time percentiles (p50, p90, p95, p99)
#   - Error rate
#   - Dữ liệu break down theo URL (login vs api-userinfo)
```

**Chạy kịch bản stress test để tìm điểm gãy:**

```bash
# Stress test: tăng dần đến khi system fail
cat > k6-stress.js << 'EOF'
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 50   },   // bắt đầu từ 50
    { duration: '2m', target: 100  },
    { duration: '2m', target: 200  },
    { duration: '2m', target: 300  },
    { duration: '2m', target: 500  },
    { duration: '5m', target: 500  },   // giữ max load 5 phút
    { duration: '2m', target: 0    },
  ],
  thresholds: {
    'http_req_duration': ['p(99)<1000'],   // 99th percentile < 1 giây
    'http_req_failed': ['rate<0.05'],      // error rate < 5%
  },
};

export default function() {
  const res = http.get(`http://192.168.1.36/health`);
  check(res, { 'status 200': (r) => r.status === 200 });
}
EOF

K6_INFLUXDB_USERNAME=k6-user \
K6_INFLUXDB_PASSWORD=K6@InfluxDB2025 \
k6 run \
  --out influxdb=http://192.168.1.51:8086/k6db \
  k6-stress.js
```

---

### Bước MON.13 — Đọc kết quả và giải thích metric quan trọng

**Trong terminal k6:**

```
✓ login status 200
✓ has access_token

checks.........................: 99.82% ✓ 5982 ✗ 11
data_received..................: 4.5 MB  25 kB/s
data_sent......................: 1.2 MB  6.7 kB/s
http_req_blocked...............: avg=2.1ms   p(90)=3.2ms   p(95)=4.1ms
http_req_connecting............: avg=1.8ms   p(90)=2.9ms
http_req_duration..............: avg=87ms    p(90)=145ms   p(95)=210ms   p(99)=380ms
  { expected_response:true }...: avg=86ms    p(90)=143ms   p(95)=208ms
http_req_failed................: 0.18%  ✓ 5982 ✗ 11
http_req_receiving.............: avg=0.87ms  p(90)=1.4ms
http_req_sending...............: avg=0.25ms  p(90)=0.43ms
http_req_tls_handshaking.......: avg=0s
http_req_waiting...............: avg=86ms    p(90)=144ms   ← TTFB (Time To First Byte)
http_reqs......................: 5993   33.2/s
iteration_duration.............: avg=1.9s    p(90)=2.1s
iterations.....................: 2996   16.6/s
login_success_rate.............: 99.81% ✓ 2991 ✗ 5
vus............................: 50     min=1 max=50
vus_max........................: 50     min=50 max=50
```

**Giải thích các metric quan trọng nhất:**

```
http_req_duration p(95)=210ms
  → 95% request trả về trong 210ms
  → 5% còn lại (outliers) lâu hơn
  → Thường dùng p(95) hoặc p(99) làm SLA benchmark

http_req_waiting (TTFB = Time To First Byte) avg=86ms
  → Server mất 86ms để xử lý và gửi byte đầu tiên
  → Nếu TTFB cao = backend chậm (DB query, CPU)
  → Nếu TTFB thấp nhưng http_req_duration cao = network hoặc slow response transfer

http_req_failed 0.18%
  → 0.18% request bị lỗi (status 4xx/5xx hoặc timeout)
  → Mục tiêu: < 0.1% ở tải bình thường

http_reqs 33.2/s (RPS = Requests Per Second)
  → Throughput của hệ thống ở 50 VU
  → Tăng VU → RPS tăng (đến điểm bão hòa thì RPS không tăng nữa = bottleneck)

vus 50
  → 50 người dùng đang gửi request đồng thời
```

**Trong Grafana, xem cùng lúc với k6 đang chạy:**

```
Node Exporter dashboard:
  → CPU của Swarm nodes tăng lên khi k6 tăng VU
  → RAM của DB server tăng (nhiều query hơn → buffer pool hoạt động nhiều)
  → Network in/out của Nginx tăng tương ứng với RPS

cAdvisor dashboard:
  → Container CPU% của authdemo_api container
  → Nếu container bị throttle (CPU limit) → tăng replicas hoặc tăng limit

Nginx dashboard:
  → active connections tăng dần
  → upstream response time (latency giữa Nginx và Swarm backend)
```

---

### Bước MON.14 — Multi-project: 1 VM monitoring cho 10 dự án

#### Cấu trúc phân tách theo project

**Prometheus — dùng labels:**

```bash
# Thêm vào prometheus.yml khi có project mới

cat >> ~/monitoring/prometheus/prometheus.yml << 'EOF'

  # Project 2 — Banking App
  - job_name: 'node-exporter-banking'
    static_configs:
      - targets:
          - '10.0.2.40:9100'
          - '10.0.2.41:9100'
    labels:
      project: 'banking'         # label phân biệt project
      team: 'team-banking'
      environment: 'production'

  # Project 3 — HR System
  - job_name: 'node-exporter-hr'
    static_configs:
      - targets:
          - '10.0.3.10:9100'
    labels:
      project: 'hr-system'
      team: 'team-hr'
EOF

# Reload Prometheus config (không cần restart)
curl -X POST http://192.168.1.51:9090/-/reload
# Prometheus tự reload và bắt đầu scrape target mới
```

**InfluxDB — tạo bucket riêng cho mỗi project:**

```bash
# Project 2: Banking
docker exec influxdb influx bucket create \
  --name k6-banking \
  --org lab-org \
  --retention 30d \
  --token lab-influxdb-super-token-change-in-prod

# Project 3: HR
docker exec influxdb influx bucket create \
  --name k6-hr \
  --org lab-org \
  --retention 30d \
  --token lab-influxdb-super-token-change-in-prod

# Tạo token write riêng cho từng team
docker exec influxdb influx auth create \
  --org lab-org \
  --write-bucket k6-banking \
  --description "Banking team k6 token" \
  --token lab-influxdb-super-token-change-in-prod
# → Copy token banking team nhận được

docker exec influxdb influx auth create \
  --org lab-org \
  --write-bucket k6-hr \
  --description "HR team k6 token" \
  --token lab-influxdb-super-token-change-in-prod
```

**Grafana — phân quyền theo Organization:**

```
Grafana multi-org: mỗi team/project có Organization riêng, không thấy dashboard của nhau

Tạo Organization mới:
1. Vào Grafana → Administration → Organizations → New Organization
   Name: "Banking Team" → Create

2. Switch sang org Banking: Profile (góc trái dưới) → Switch Organization → Banking Team

3. Thêm datasource cho org Banking:
   Connections → Data sources → Add new data source
   - Prometheus: filter metric có label project="banking"
     URL: http://prometheus:9090
     HTTP Headers: X-Scope-OrgID: banking  ← optional, nếu dùng Prometheus multi-tenant
   - InfluxDB: bucket k6-banking

4. Tạo user và gán vào org:
   Administration → Users → Invite user
   → Banking team member chỉ thấy dashboard của Banking org
```

**Grafana Dashboard với Variable để chọn project (cách đơn giản hơn multi-org):**

```
1. Vào Dashboard → Settings → Variables → Add variable
   Name: project
   Type: Query
   Data source: Prometheus
   Query: label_values(up, project)   ← lấy tất cả giá trị label "project" từ Prometheus
   → Grafana tự tạo dropdown "Project" ở đầu dashboard

2. Trong panel, thêm filter vào query:
   up{project="$project"}    ← $project là biến vừa tạo
   
3. Giờ 1 dashboard dùng được cho tất cả 10 project — chỉ cần chọn project ở dropdown
```

---

### Bước MON.15 — Cập nhật docker-compose cho production-like setup

```bash
# Tạo .env file chứa thông tin nhạy cảm (không commit lên git)
cat > ~/monitoring/.env << 'EOF'
# Thay các giá trị này bằng giá trị thật
GRAFANA_ADMIN_PASSWORD=Admin@Grafana2025
INFLUXDB_ADMIN_PASSWORD=Admin@InfluxDB2025
INFLUXDB_TOKEN=lab-influxdb-super-token-change-in-prod
# Giá trị trên CHỈ DÙNG CHO LAB
# Production: tạo bằng: openssl rand -hex 32
EOF

# docker-compose tự đọc .env khi chạy "docker compose up"
# Các biến ${VAR} trong docker-compose.yml sẽ được thay bằng giá trị từ .env
```

```bash
# Khởi động lại stack với config mới
cd ~/monitoring
docker compose down
docker compose up -d

# Verify tất cả container đang chạy
docker compose ps
# Kỳ vọng: tất cả STATUS là "running (healthy)"

# Xem log nếu có lỗi
docker compose logs prometheus --tail=20
docker compose logs grafana --tail=20
docker compose logs influxdb --tail=20
```

---

### Bước MON.16 — Checklist Monitoring

```
Monitoring VM (192.168.1.51):
□ Docker cài xong, docker compose ps thấy 4 container healthy
□ Prometheus: http://192.168.1.51:9090 → Status → Targets → TẤT CẢ UP (xanh)
□ Grafana: http://192.168.1.51:3000 → login được, thấy datasource Prometheus + InfluxDB
□ InfluxDB: http://192.168.1.51:8086 → login được, thấy bucket k6-authdemo

Exporters trên các VM:
□ .37 (Nginx): curl http://192.168.1.37:9100/metrics trả về metric
□ .37 (Nginx): curl http://192.168.1.37:9113/metrics trả về nginx_up 1
□ .38 (DB Primary): port 9100 accessible từ monitoring VM
□ .39 (DB Secondary): port 9100 accessible từ monitoring VM
□ .40/.41/.42 (Swarm): port 9100 và 8081 accessible từ monitoring VM

Grafana Dashboards:
□ Node Exporter Full (ID 1860): thấy CPU/RAM graph của tất cả VM
□ Docker cAdvisor (ID 14282): thấy container metric của Swarm nodes
□ Nginx dashboard (ID 9614): thấy request rate và connection count

k6 + InfluxDB:
□ k6 chạy được với --out influxdb=http://192.168.1.51:8086/k6db
□ Grafana dashboard k6 (ID 2587) hiện data realtime khi k6 chạy
□ Thresholds được set và k6 báo PASS/FAIL đúng
```

---

### Tóm tắt kiến trúc VM hoàn chỉnh sau khi thêm Monitoring

| VM IP | Vai trò | Components |
|---|---|---|
| 192.168.1.36 | Virtual IP (Keepalived) | — |
| 192.168.1.37 | Nginx Master | Nginx, Keepalived, Node Exporter :9100, Nginx Exporter :9113, cAdvisor :8081 |
| 192.168.1.43 | Nginx Backup | (giống .37) |
| 192.168.1.38 | DB Primary | SQL Server / PostgreSQL, Node Exporter :9100 |
| 192.168.1.39 | DB Secondary | SQL Server / PostgreSQL, Node Exporter :9100 |
| 192.168.1.40 | Swarm Manager | Docker, Node Exporter :9100, cAdvisor :8081 |
| 192.168.1.41 | Swarm Worker1 | Docker, Node Exporter :9100, cAdvisor :8081 |
| 192.168.1.42 | Swarm Worker2 | Docker, Node Exporter :9100, cAdvisor :8081 |
| 192.168.1.50 | Private Registry | Docker Registry / Harbor |
| **192.168.1.51** | **Monitoring** | **Prometheus :9090, Grafana :3000, InfluxDB :8086, Node Exporter :9100, cAdvisor :8081** |

---

## Part 7 — Thực hành Nginx Load Balancing (192.168.1.60) — Làm quen trước khi setup VIP

### Mục tiêu của session này

Đây là bước **bắt buộc trước khi setup Keepalived + VIP**. Nginx thuần trên 1 VM đơn là nền tảng — phải nắm chắc trước khi thêm complexity.

**VM mới: 192.168.1.60** — dùng riêng để thực hành, không chạm vào hệ thống hiện tại (.37, .43).

```
Kiến trúc session này (KHÔNG dùng VIP, KHÔNG dùng Keepalived):

[Client / Postman / curl / ab / wrk / k6]
               │
               ▼  (gọi trực tiếp IP .60)
     ┌─────────────────────┐
     │   Nginx Practice     │  192.168.1.60
     │   port 80 / 8080     │  ← VM mới, chỉ Nginx
     └──────────┬──────────┘
                │
        ┌───────┼───────┐
        ▼       ▼       ▼
   192.168.1.40 .41    .42    ← Swarm backend — giữ nguyên từ Part 3
      port 80  port 80 port 80
```

> **Backend .40/.41/.42 giữ nguyên** — chỉ thêm VM .60 làm Nginx practice node. Sau khi thành thạo session này, chuyển sang cấu hình Keepalived + VIP trên .37/.43 sẽ dễ hơn nhiều.

---

### Bước NX.1 — Tạo VM 192.168.1.60 và cài Docker + Nginx

```bash
# Clone từ một VM Ubuntu Server 22.04 đang có trong lab
# Sau khi clone: đổi hostname và IP

# 1. Đổi hostname
sudo hostnamectl set-hostname nginx-practice

# 2. Đổi IP trong netplan
sudo nano /etc/netplan/00-installer-config.yaml
```

```yaml
# /etc/netplan/00-installer-config.yaml
network:
  version: 2
  ethernets:
    ens33:
      addresses:
        - 192.168.1.60/24
      gateway4: 192.168.1.1
      nameservers:
        addresses: [8.8.8.8, 8.8.4.4]
```

```bash
sudo netplan apply
ip a   # verify: thấy 192.168.1.60

# 3. Cài Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
newgrp docker
docker --version   # Kỳ vọng: Docker 24.x+

# 4. Kiểm tra trạng thái firewall trước
sudo ufw status
# Có 2 trường hợp:
#
#   "Status: inactive"  → ufw chưa bật → tất cả port mở mặc định
#                         Không cần làm gì thêm, bỏ qua các lệnh ufw bên dưới
#                         "ufw reload" sẽ báo: Firewall not enabled (skipping reload)
#                         → bình thường, không phải lỗi
#
#   "Status: active"    → ufw đang bật → phải add rule mới kết nối được
#                         Chạy các lệnh bên dưới

# Chỉ chạy khi "Status: active":
sudo ufw allow 80/tcp
sudo ufw allow 8080/tcp
sudo ufw reload

# ── Nếu muốn bật ufw cho lab (tuỳ chọn) ────────────────────────────────
# QUAN TRỌNG: allow SSH trước khi enable — nếu không sẽ mất SSH vào VM
# sudo ufw allow 22/tcp
# sudo ufw enable       ← nhập "y" khi được hỏi
# sudo ufw status       ← verify: Status: active

# 5. Verify ping đến Swarm backend
ping -c 2 192.168.1.40
ping -c 2 192.168.1.41
ping -c 2 192.168.1.42
# Kỳ vọng: tất cả ping OK
```

---

### Bước NX.2 — Cấu hình Nginx cơ bản + Upstream

```bash
mkdir -p ~/nginx-practice
cd ~/nginx-practice
```

```bash
cat > ~/nginx-practice/nginx.conf << 'EOF'
# ══════════════════════════════════════════════════════════════════
# nginx.conf — Practice node 192.168.1.60
# Load balance đến Swarm cluster (.40 / .41 / .42)
# ══════════════════════════════════════════════════════════════════

worker_processes auto;

events {
    worker_connections 1024;
    use epoll;
    multi_accept on;
}

http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;

    sendfile        on;
    tcp_nopush      on;
    tcp_nodelay     on;
    server_tokens   off;
    keepalive_timeout 65;

    # ─── Log format chi tiết để quan sát load balancing ─────────────────
    # upstream_addr    : backend nào xử lý request → verify round-robin
    # upstream_rt      : backend mất bao lâu → phát hiện backend chậm
    # total_rt         : latency thật của client (bao gồm thời gian Nginx)
    log_format detail '$remote_addr [$time_local] "$request" '
                      '$status $body_bytes_sent '
                      'upstream="$upstream_addr" '
                      'upstream_rt=$upstream_response_time '
                      'total_rt=$request_time';

    access_log /var/log/nginx/access.log detail;
    error_log  /var/log/nginx/error.log warn;

    # ─── Upstream group: Swarm backend cluster ──────────────────────────
    upstream swarm_backend {
        # Thuật toán hiện tại: round-robin (default)
        # Bỏ comment 1 dòng để thực hành từng thuật toán:
        # least_conn;                ← tốt nhất cho API xử lý không đồng đều
        # ip_hash;                   ← sticky session (cẩn thận: không cân bằng tốt)
        # random two least_conn;     ← tốt cho >10 backend

        # max_fails + fail_timeout: passive health check
        # Nếu .40 fail 3 lần trong 30s → đánh dấu down, bỏ qua 30s → thử lại
        server 192.168.1.40:80 weight=1 max_fails=3 fail_timeout=30s;
        server 192.168.1.41:80 weight=1 max_fails=3 fail_timeout=30s;
        server 192.168.1.42:80 weight=1 max_fails=3 fail_timeout=30s;

        # Giữ sẵn 32 TCP connection persistent đến mỗi backend
        # Tránh overhead TCP handshake khi có nhiều request liên tiếp
        keepalive 32;
    }

    # ─── Server block chính ─────────────────────────────────────────────
    server {
        listen 80;
        server_name _;

        proxy_connect_timeout 10s;
        proxy_send_timeout    60s;
        proxy_read_timeout    60s;

        # HTTP/1.1 + xóa Connection header để keepalive upstream hoạt động
        proxy_http_version 1.1;
        proxy_set_header Connection "";

        # Truyền thông tin client thật lên backend
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Buffer response từ backend trước khi gửi cho client
        proxy_buffering         on;
        proxy_buffer_size       4k;
        proxy_buffers           8 16k;
        proxy_busy_buffers_size 32k;

        location / {
            proxy_pass http://swarm_backend;
        }

        location /nginx-health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }

    # ─── Server block status (port 8080) ────────────────────────────────
    server {
        listen 8080;

        location /nginx-status {
            stub_status on;
            access_log  off;
            # Truy cập bằng: docker exec nginx-practice curl http://localhost:8080/nginx-status
            # Không dùng "curl localhost:8080" từ VM host — Docker NAT đổi source IP
            # thành 172.17.0.1 (bridge gateway), không match 127.0.0.1
            allow 127.0.0.1;          # bên trong container
            allow 192.168.1.0/24;     # các VM khác trong lab (Prometheus scrape)
            deny all;
        }

        location /nginx-health {
            access_log off;
            return 200 "ok\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF
```

```bash
cat > ~/nginx-practice/docker-compose.yml << 'EOF'
services:
  nginx:
    image: nginx:1.25-alpine
    container_name: nginx-practice
    ports:
      - "80:80"
      - "8080:8080"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - nginx-logs:/var/log/nginx
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/nginx-health"]
      interval: 10s
      timeout: 5s
      retries: 3

volumes:
  nginx-logs:
EOF
```

```bash
# Khởi động
docker compose up -d

# Verify
docker ps
curl http://localhost:8080/nginx-health                                    # Kỳ vọng: ok
docker exec nginx-practice curl http://localhost:8080/nginx-status         # Kỳ vọng: Active connections: 1
# Lưu ý: dùng docker exec để curl chạy BÊN TRONG container (127.0.0.1 đúng)
# KHÔNG dùng "curl localhost:8080/nginx-status" từ VM host — Docker NAT đổi
# source IP thành bridge gateway, không match allow 127.0.0.1 trong config
```

---

### Bước NX.3 — Test Load Balancing với curl, ab và wrk

**Test cơ bản với curl — quan sát upstream trong log:**

```bash
# Gửi 9 request, mỗi request in ra HTTP status code
for i in $(seq 1 9); do
    curl -s http://192.168.1.60/ -o /dev/null -w "request $i: HTTP %{http_code}\n"
done

# Xem log — cột upstream= phải luân phiên .40 / .41 / .42
# LƯU Ý: access.log trong image Nginx chính thức là symlink → /dev/stdout
# Dùng "docker exec ... cat/tail" sẽ treo terminal vì đọc stdout vô tận.
# Thay bằng "docker logs" để Docker đọc stream đã capture sẵn.
docker logs --tail 20 nginx-practice
# Tìm trường upstream="..." để xác nhận round-robin đang hoạt động
```

**Đếm phân phối request bằng awk:**

```bash
# Gửi 30 request
for i in $(seq 1 30); do curl -s http://192.168.1.60/ -o /dev/null; done

# Đếm mỗi backend nhận bao nhiêu
docker logs nginx-practice \
  | grep -oP 'upstream="\K[^"]+' \
  | sort | uniq -c | sort -rn
# Kỳ vọng round-robin: 3 backend mỗi cái ~10 request
```

**Test với Apache Benchmark (ab) — nhiều request đồng thời:**

```bash
# Cài ab trên VM .60
sudo apt install -y apache2-utils

# 100 request, 10 concurrent
ab -n 100 -c 10 http://192.168.1.60/

# 1000 request, 50 concurrent — stress test nhẹ
ab -n 1000 -c 50 http://192.168.1.60/

# Output quan trọng cần đọc:
# Requests per second:    500.00 [#/sec]   ← throughput
# Time per request:         2.000 [ms]      ← latency trung bình
# Time per request:       100.000 [ms]      ← latency per request (concurrent)
# Transfer rate:           xxx [Kbytes/sec]
# Failed requests:           0               ← phải là 0

# Sau khi chạy xong, kiểm tra phân phối:
docker logs nginx-practice \
  | grep -oP 'upstream="\K[^"]+' \
  | sort | uniq -c
# round-robin: mỗi backend ~333 request
```

**Test với wrk — realistic hơn ab:**

```bash
# Cài wrk
sudo apt install -y wrk

# 30 giây, 10 thread, 50 concurrent connections
wrk -t10 -c50 -d30s http://192.168.1.60/

# Output:
# Running 30s test @ http://192.168.1.60/
#   10 threads and 50 connections
#   Thread Stats   Avg      Stdev     Max   ±Stdev
#     Latency    40.12ms   15.23ms 120.45ms   75.00%
#     Req/Sec   125.40     20.10   200.00     68.00%
#   Latency Distribution
#      50%   35.23ms
#      75%   50.12ms
#      90%   65.43ms
#      99%  120.45ms    ← P99: latency của 1% request chậm nhất
#   37620 requests in 30.00s, 12.40MB read
# Requests/sec:   1254.00    ← throughput tổng
```

**Xem stub_status realtime trong khi đang test:**

```bash
# Terminal 1: chạy wrk liên tục
wrk -t5 -c100 -d60s http://192.168.1.60/ &

# Terminal 2: watch stub_status mỗi giây (chạy curl bên trong container)
watch -n 1 'docker exec nginx-practice curl -s http://localhost:8080/nginx-status'

# Output mẫu khi đang load:
# Active connections: 105
# server accepts handled requests
#  50000 50000 100000
# Reading: 0 Writing: 105 Waiting: 0
#
# Reading  : đang đọc request header từ client
# Writing  : đang gửi response về client (105 = 105 concurrent)
# Waiting  : keep-alive connection đang nhàn, chờ request mới
```

---

### Bước NX.4 — Thử các thuật toán phân tải

**Thí nghiệm 1: Round-robin (default)**

```bash
# Config mặc định (không comment gì thêm trong upstream block)
# Ghi timestamp trước khi test — dùng --since để lọc log của đúng lần chạy này
# (không thể truncate vì access.log → /dev/stdout trong container Nginx)
START=$(date +%Y-%m-%dT%H:%M:%S)

ab -n 300 -c 30 http://192.168.1.60/ 2>/dev/null

docker logs --since "$START" nginx-practice \
  | grep -oP 'upstream="\K[^"]+' \
  | sort | uniq -c
# Kỳ vọng: 3 backend mỗi cái ~100 request (đều nhau)
```

**Thí nghiệm 2: least_conn — tốt hơn khi request có thời gian xử lý khác nhau**

```bash
# Sửa nginx.conf: bỏ comment dòng least_conn; trong upstream block
nano ~/nginx-practice/nginx.conf
# → uncomment:  least_conn;

# Test config trước khi reload
docker exec nginx-practice nginx -t

# Reload không downtime
docker exec nginx-practice nginx -s reload

START=$(date +%Y-%m-%dT%H:%M:%S)
ab -n 300 -c 30 http://192.168.1.60/ 2>/dev/null

docker logs --since "$START" nginx-practice \
  | grep -oP 'upstream="\K[^"]+' \
  | sort | uniq -c
# least_conn: phân phối ít đều hơn về số lượng nhưng "thông minh" hơn khi 1 backend chậm
```

**Thí nghiệm 3: weight — backend không đồng đều**

```bash
# Sửa weight: .40 mạnh gấp đôi
# server 192.168.1.40:80 weight=2;
# server 192.168.1.41:80 weight=1;
# server 192.168.1.42:80 weight=1;
nano ~/nginx-practice/nginx.conf

docker exec nginx-practice nginx -t && docker exec nginx-practice nginx -s reload

START=$(date +%Y-%m-%dT%H:%M:%S)
ab -n 400 -c 20 http://192.168.1.60/ 2>/dev/null

docker logs --since "$START" nginx-practice \
  | grep -oP 'upstream="\K[^"]+' \
  | sort | uniq -c
# Kỳ vọng: .40 ~200, .41 ~100, .42 ~100 (tỉ lệ 2:1:1)
```

**Thí nghiệm 4: Passive health check — tự loại backend chết**

```bash
# Đảm bảo max_fails=3 fail_timeout=30s đang có trong config

# Mô phỏng backend .40 chết: tắt service trên Swarm node .40
# (hoặc tắt port forwarding tạm thời để test)
ssh bank@192.168.1.40 "sudo ufw deny 80/tcp"

# Gửi request và observe:
for i in $(seq 1 20); do
    curl -s http://192.168.1.60/ -o /dev/null -w "%{http_code}\n"
done

# Xem log error — Nginx sẽ ghi lại khi .40 fail
# error.log → /dev/stderr trong container; docker logs capture cả stderr
docker logs --tail 20 nginx-practice 2>&1 | grep -i "failed\|upstream\|error"
# → "connect() failed (111: Connection refused) while connecting to upstream"

# Sau 3 fail: Nginx tự loại .40, chỉ dùng .41 và .42
docker logs --tail 20 nginx-practice \
  | grep -oP 'upstream="\K[^"]+'
# Kỳ vọng: chỉ thấy .41 và .42

# Bật lại .40
ssh bank@192.168.1.40 "sudo ufw allow 80/tcp"
# Sau 30s (fail_timeout), .40 được đưa trở lại pool tự động
```

---

### NX.5 Advanced — Upload file lớn (client_max_body_size + Timeout)

**Vấn đề thực tế Tech Lead gặp:**

```
Symptom                      | HTTP Error | Nguyên nhân
──────────────────────────────|────────────|────────────────────────────────────────
Upload file 10MB trả lỗi     | 413        | client_max_body_size mặc định 1m
Upload mất quá lâu bị cắt    | 504        | proxy_send_timeout hoặc proxy_read_timeout
Backend xử lý nặng bị timeout| 504        | proxy_read_timeout quá nhỏ
Report xuất CSV mất 5 phút   | 504        | proxy_read_timeout < 300s
API streaming bị đứt          | 502        | proxy_buffering on (cần off cho stream)
```

**Cấu hình xử lý upload lớn — thêm vào server block:**

```bash
cat > ~/nginx-practice/nginx.conf << 'EOF'
worker_processes auto;
events { worker_connections 1024; use epoll; multi_accept on; }

http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;
    sendfile on; tcp_nopush on; tcp_nodelay on;
    server_tokens off;

    log_format detail '$remote_addr [$time_local] "$request" $status $body_bytes_sent upstream="$upstream_addr" upstream_rt=$upstream_response_time total_rt=$request_time';
    access_log /var/log/nginx/access.log detail;
    error_log  /var/log/nginx/error.log warn;

    upstream swarm_backend {
        least_conn;
        server 192.168.1.40:80 weight=1 max_fails=3 fail_timeout=30s;
        server 192.168.1.41:80 weight=1 max_fails=3 fail_timeout=30s;
        server 192.168.1.42:80 weight=1 max_fails=3 fail_timeout=30s;
        keepalive 32;
    }

    server {
        listen 80;
        server_name _;

        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # ─── UPLOAD LỚN ─────────────────────────────────────────────────
        # Mặc định Nginx chỉ nhận body tối đa 1MB → upload file lớn bị 413
        client_max_body_size 100m;

        # Buffer body trong RAM trước khi gửi backend
        # Nếu body > buffer → Nginx ghi tạm ra /tmp (chậm hơn)
        client_body_buffer_size 1m;

        # Thời gian chờ client gửi xong body (từng chunk, không phải toàn bộ)
        # Tăng nếu client upload qua mạng chậm (mobile, 4G)
        client_body_timeout 120s;

        # ─── TIMEOUT BACKEND ────────────────────────────────────────────
        proxy_connect_timeout 10s;    # TCP connect đến backend (hiếm khi cần tăng)
        proxy_send_timeout    300s;   # gửi request body từ Nginx → backend
        proxy_read_timeout    300s;   # chờ backend trả response ← quan trọng nhất

        # proxy_request_buffering:
        #   on (default) : Nginx buffer toàn bộ request body → gửi backend 1 lần
        #                  Ổn với file < 100MB, không ổn với file cỡ GB
        #   off           : Nginx stream trực tiếp client → backend, không cần buffer RAM
        #                  Phù hợp upload lớn, backend phải hỗ trợ streaming
        proxy_request_buffering off;

        # ─── API thông thường ────────────────────────────────────────────
        location /api/ {
            proxy_pass         http://swarm_backend;
            proxy_read_timeout 30s;   # API nhanh: fail nhanh nếu bất thường
        }

        # ─── Endpoint upload: override timeout riêng ─────────────────────
        location /api/upload {
            proxy_pass              http://swarm_backend;
            client_max_body_size    500m;   # cho phép đến 500MB riêng endpoint này
            proxy_read_timeout      600s;   # backend có thể mất đến 10 phút
            proxy_send_timeout      600s;
            proxy_request_buffering off;
        }

        # ─── API export/report nặng ──────────────────────────────────────
        location /api/reports/ {
            proxy_pass         http://swarm_backend;
            proxy_read_timeout 600s;   # export lớn mất nhiều phút
            proxy_send_timeout 600s;
        }

        # ─── Streaming / SSE / Long-poll ─────────────────────────────────
        location /api/events {
            proxy_pass              http://swarm_backend;
            proxy_read_timeout      3600s;  # 1 giờ cho long-lived connection
            proxy_buffering         off;    # stream ngay về client, không buffer
            proxy_cache             off;
        }

        # ─── WebSocket proxy ─────────────────────────────────────────────
        location /ws/ {
            proxy_pass         http://swarm_backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade    $http_upgrade;  # protocol upgrade
            proxy_set_header Connection "upgrade";
            proxy_read_timeout 3600s;   # WebSocket cần timeout rất dài
        }

        location / {
            proxy_pass http://swarm_backend;
        }

        location /nginx-health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }

    server {
        listen 8080;
        location /nginx-status {
            stub_status on; access_log off;
            allow 127.0.0.1;
            allow 192.168.1.0/24; deny all;
        }
        location /nginx-health {
            access_log off; return 200 "ok\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF

docker exec nginx-practice nginx -t && docker exec nginx-practice nginx -s reload
```

**Test upload lớn:**

```bash
# Tạo file test 10MB
dd if=/dev/urandom of=/tmp/test_10mb.bin bs=1M count=10
dd if=/dev/urandom of=/tmp/test_50mb.bin bs=1M count=50

# Upload (giả sử backend có endpoint nhận multipart)
curl -X POST http://192.168.1.60/api/upload \
  -F "file=@/tmp/test_10mb.bin" \
  -v 2>&1 | grep "< HTTP"

# Test với file vượt giới hạn — phải nhận 413
dd if=/dev/urandom of=/tmp/test_600mb.bin bs=1M count=600
curl -X POST http://192.168.1.60/api/upload \
  -F "file=@/tmp/test_600mb.bin" \
  -v 2>&1 | grep "< HTTP"
# Kỳ vọng: HTTP/1.1 413 Request Entity Too Large

# Đo thời gian upload
time curl -X POST http://192.168.1.60/api/upload \
  -F "file=@/tmp/test_50mb.bin" \
  -o /dev/null -s -w "HTTP: %{http_code}, Time: %{time_total}s\n"
```

**Bảng quick-reference timeout — in ra để dán lên bàn làm việc:**

```
Directive                | Default | Khi nào cần tăng
─────────────────────────|─────────|──────────────────────────────────────────
client_max_body_size     | 1m      | Upload file > 1MB
client_body_buffer_size  | 8k/16k  | Upload nhiều file trung bình
client_body_timeout      | 60s     | Client upload qua mạng chậm (mobile)
client_header_timeout    | 60s     | Hiếm khi cần
proxy_connect_timeout    | 60s     | Backend cold start / khởi động chậm
proxy_send_timeout       | 60s     | Forward upload lớn từ client → backend
proxy_read_timeout       | 60s     | Backend xử lý lâu (report, AI, export)
keepalive_timeout        | 75s     | Client gọi nhiều API liên tiếp
send_timeout             | 60s     | Gửi response lớn về client chậm
```

---

### NX.6 Advanced — Timeout Tuning cho API chậm

**Giải thích chi tiết từng timeout và khi nào timeout xảy ra:**

```
[Client] ──────────────────────────────────────────── [Nginx] ──── [Backend]
   │                                                    │               │
   │ ← client_header_timeout: chờ client gửi header ──▶│               │
   │ ← client_body_timeout: chờ mỗi chunk body ────────▶│               │
   │                                                    │─ proxy_connect_timeout ─▶│
   │                                                    │─ proxy_send_timeout ────▶│ (gửi request)
   │                                                    │◀─ proxy_read_timeout ────│ (chờ response)
   │◀─ send_timeout: gửi response về client ───────────│               │
```

```nginx
http {
    # Timeout mặc định cho toàn bộ server (áp dụng khi location không override)
    proxy_connect_timeout 10s;
    proxy_send_timeout    60s;
    proxy_read_timeout    60s;

    server {
        listen 80;

        # API thông thường: fail nhanh nếu bất thường
        location /api/users {
            proxy_pass         http://swarm_backend;
            proxy_read_timeout 15s;   # query đơn giản — nếu > 15s là có vấn đề
        }

        # API tìm kiếm / filter phức tạp
        location /api/search {
            proxy_pass         http://swarm_backend;
            proxy_read_timeout 60s;
        }

        # API export / báo cáo nặng
        location /api/export {
            proxy_pass         http://swarm_backend;
            proxy_read_timeout 600s;   # export lớn mất nhiều phút
            proxy_send_timeout 600s;
            # Tắt buffer để gửi dần về client (không chờ backend xong hết)
            proxy_buffering    off;
        }

        # API gọi external service (payment gateway, SMS, email)
        location /api/payment {
            proxy_pass         http://swarm_backend;
            proxy_read_timeout 90s;    # external service có thể chậm
            proxy_connect_timeout 15s; # tăng vì backend phải open connection đến external
        }

        # Realtime / SSE (Server-Sent Events)
        location /api/notifications {
            proxy_pass              http://swarm_backend;
            proxy_read_timeout      3600s;  # 1 giờ — connection sống lâu
            proxy_buffering         off;    # gửi event ngay khi có, không gom buffer
            proxy_cache             off;
            proxy_set_header        Connection "";
            # Thêm header để client biết đây là SSE
            add_header              Cache-Control no-cache;
            add_header              X-Accel-Buffering no;  # disable buffering ở Nginx proxy
        }

        # WebSocket
        location /ws/ {
            proxy_pass         http://swarm_backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade    $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_read_timeout 3600s;
            proxy_send_timeout 3600s;
        }
    }
}
```

---

### NX.7 Advanced — Rate Limiting (chặn DDoS cơ bản)

**Use case:** Bảo vệ API login khỏi brute-force, giới hạn request per IP để chặn DDoS tầng ứng dụng.

```nginx
http {
    # ─── Định nghĩa zone rate limit (đặt TRONG http { } NGOÀI server { }) ──────
    #
    # limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    # │                │                  │            │     │
    # │                │                  │            │     └─ tốc độ tối đa: 10 req/giây
    # │                │                  │            └─ 10MB RAM cho zone
    # │                │                  └─ tên zone
    # │                └─ key = IP client dạng binary (compact hơn string)
    # └─ directive
    #
    # 1MB RAM ≈ 16.000 IP → 10MB ≈ 160.000 IP khác nhau có thể track đồng thời

    limit_req_zone $binary_remote_addr zone=api_limit:10m   rate=10r/s;  # API chung
    limit_req_zone $binary_remote_addr zone=login_limit:10m rate=3r/m;   # Login: 3 lần/phút
    limit_req_zone $binary_remote_addr zone=upload_limit:10m rate=2r/m;  # Upload: 2 lần/phút

    # Zone cho limit_conn (giới hạn số connection đồng thời, khác với request)
    limit_conn_zone $binary_remote_addr zone=per_ip:10m;

    server {
        listen 80;

        # API chung: tối đa 10 req/s
        # burst=20: cho phép "bùng" thêm 20 request vượt rate (queue hoặc xử lý ngay)
        # nodelay: xử lý burst ngay thay vì delay queue → latency thấp hơn
        # Không có nodelay: Nginx delay request trong burst → latency tăng theo rate
        location /api/ {
            limit_req        zone=api_limit burst=20 nodelay;
            limit_req_status 429;   # trả 429 Too Many Requests (đúng hơn 503)

            proxy_pass http://swarm_backend;
        }

        # Login endpoint: 3 lần/phút, burst 5
        location /api/auth/login {
            limit_req        zone=login_limit burst=5 nodelay;
            limit_req_status 429;

            # Giới hạn cả connection đồng thời: tối đa 5 connection từ 1 IP
            limit_conn       per_ip 5;
            limit_conn_status 429;

            proxy_pass http://swarm_backend;
        }

        # Upload: rate limit thấp hơn API thường
        location /api/upload {
            limit_req        zone=upload_limit burst=2 nodelay;
            limit_req_status 429;

            client_max_body_size 200m;
            proxy_read_timeout   300s;
            proxy_pass           http://swarm_backend;
        }

        # Whitelist IP nội bộ: không áp dụng rate limit
        location /api/internal/ {
            allow  192.168.1.0/24;
            deny   all;
            proxy_pass http://swarm_backend;
        }
    }
}
```

**Test rate limit:**

```bash
# Gửi 30 request liên tiếp nhanh → thấy 429 xuất hiện sau vài request
for i in $(seq 1 30); do
    curl -s -o /dev/null -w "[$i] %{http_code}\n" http://192.168.1.60/api/test
done
# Kỳ vọng: [1] 200, [2] 200, ..., [11] 429, [12] 429...

# Test với ab — đếm 429
ab -n 200 -c 20 http://192.168.1.60/api/test 2>&1 | grep -E "(Requests per|Non-2xx)"
# Non-2xx responses: 180  ← số request bị rate limit (trả 429)

# Verify log ghi lại rate limit event
docker exec nginx-practice grep "limiting requests" /var/log/nginx/error.log | tail -5
```

---

### NX.8 Advanced — Buffer Tuning

**Hiểu proxy buffer ảnh hưởng performance:**

```
proxy_buffering ON (default):
  Backend ──response──▶ [Nginx buffer trong RAM/disk] ──▶ Client
  Backend giải phóng connection sớm hơn (backend không phải chờ client chậm)
  Tốt cho: API response thông thường

proxy_buffering OFF:
  Backend ──response──▶ [Nginx pass-through] ──▶ Client  (không buffer)
  Backend phải giữ connection đến khi client nhận xong
  Tốt cho: Streaming, SSE, WebSocket, download file lớn
```

```nginx
http {
    server {
        # ─── Buffer defaults (áp dụng cho tất cả location không override) ──
        proxy_buffering on;

        # Đọc response header từ backend (bao gồm cả response header của backend)
        # 4k đủ cho header thông thường; tăng lên 8k nếu backend có nhiều header
        proxy_buffer_size 8k;

        # Bộ buffer đọc response body: 8 buffer × 32k = 256k RAM
        # Nếu response > 256k → Nginx tràn ra temp file (chậm hơn RAM)
        proxy_buffers 8 32k;

        # Lượng buffer đang "busy" (đang ghi về client) được giữ đồng thời
        # Phải ≤ tổng proxy_buffers
        proxy_busy_buffers_size 64k;

        # Mỗi lần ghi temp file trên disk (khi response vượt RAM buffer)
        proxy_temp_file_write_size 64k;

        # ─── API response thông thường (< 256k) ──────────────────────────
        location /api/ {
            proxy_pass http://swarm_backend;
            # Dùng defaults ở trên, đủ cho hầu hết API
        }

        # ─── Export CSV / PDF lớn ─────────────────────────────────────────
        location /api/export {
            proxy_buffers              16 64k;   # 16 × 64k = 1MB RAM buffer
            proxy_buffer_size          64k;
            proxy_busy_buffers_size    128k;
            proxy_temp_file_write_size 256k;
            proxy_pass http://swarm_backend;
        }

        # ─── Streaming / download file: tắt buffer ───────────────────────
        location /api/download {
            proxy_buffering    off;   # stream thẳng về client
            proxy_read_timeout 300s;
            proxy_pass         http://swarm_backend;
        }

        # ─── SSE / Realtime events ────────────────────────────────────────
        location /api/events {
            proxy_buffering  off;    # BẮT BUỘC tắt cho SSE
            proxy_cache      off;
            proxy_read_timeout 3600s;
            proxy_pass       http://swarm_backend;
        }
    }
}
```

**Rule of thumb buffer:**

```
Response size    | Behavior                               | Action
─────────────────|────────────────────────────────────────|──────────────────────────
< 256k           | Toàn bộ trong RAM, nhanh               | Default buffer đủ dùng
256k – 10MB      | Một phần ghi temp file, chậm hơn       | Tăng proxy_buffers
> 10MB           | Nhiều I/O disk, overhead lớn            | proxy_buffering off
Streaming / SSE  | Không buffering                         | proxy_buffering off (bắt buộc)
```

---

### NX.9 Advanced — Gzip Compression

**Use case:** Giảm băng thông 60-80% cho JSON response lớn, tăng tốc trải nghiệm người dùng.

```nginx
http {
    # ─── Gzip settings ─────────────────────────────────────────────────
    gzip on;

    # Compress cả response từ proxied backend (default: off cho proxied)
    gzip_proxied any;

    # Mức nén: 1 (nhanh, ít nén) → 9 (chậm, nén nhiều)
    # Level 4-6: sweet spot — giảm 60-70% size với CPU overhead chấp nhận được
    gzip_comp_level 5;

    # Không compress response < 1KB — overhead gzip header > lợi ích
    gzip_min_length 1024;

    # Gzip cả HTTP/1.0 proxied response
    gzip_http_version 1.0;

    # Loại content được compress — text/html mặc định, khai báo thêm:
    gzip_types
        text/plain
        text/css
        text/javascript
        application/javascript
        application/json
        application/xml
        application/xml+rss
        image/svg+xml;

    # Thêm Vary: Accept-Encoding vào response
    # Giúp CDN và browser cache phân biệt compressed vs uncompressed version
    gzip_vary on;

    # Buffer cho quá trình nén (default 32×4k = 128k, đủ dùng)
    gzip_buffers 32 4k;

    server {
        listen 80;
        location / { proxy_pass http://swarm_backend; }
        location /nginx-health { access_log off; return 200 "healthy\n"; add_header Content-Type text/plain; }
    }
    server {
        listen 8080;
        location /nginx-status { stub_status on; access_log off; allow 127.0.0.1; allow 192.168.1.0/24; deny all; }
        location /nginx-health { access_log off; return 200 "ok\n"; add_header Content-Type text/plain; }
    }
}
```

**Test gzip hoạt động:**

```bash
# Gọi với Accept-Encoding: gzip
curl -H "Accept-Encoding: gzip" -I http://192.168.1.60/api/users
# Kỳ vọng trong header: Content-Encoding: gzip  + Vary: Accept-Encoding

# So sánh kích thước có và không có gzip
echo "=== Không gzip ==="
curl -s http://192.168.1.60/api/users | wc -c

echo "=== Có gzip (decompress tự động) ==="
curl -s --compressed http://192.168.1.60/api/users | wc -c
# Kỳ vọng: gzip nhỏ hơn 60-80% với JSON

# Kiểm tra mức nén
curl -s -H "Accept-Encoding: gzip" http://192.168.1.60/api/users -o /tmp/resp.gz
wc -c /tmp/resp.gz   # kích thước compressed
gzip -d /tmp/resp.gz && wc -c /tmp/resp    # kích thước sau decompress
```

---

### NX.10 Advanced — Proxy Cache

**Use case:** Cache response của API ít thay đổi (danh mục, config, static data) → giảm tải backend đáng kể.

```nginx
http {
    # ─── Định nghĩa cache zone (NGOÀI server block) ─────────────────────
    # /tmp/nginx-cache  : thư mục lưu file cache trên disk
    # levels=1:2        : cấu trúc thư mục 2 cấp (a/bc/... tránh quá nhiều file trong 1 folder)
    # keys_zone=c:10m   : tên zone "c", 10MB RAM lưu index (1MB ≈ 8000 key → 10MB ≈ 80000 key)
    # max_size=1g       : tối đa 1GB disk lưu nội dung cache
    # inactive=60m      : xóa cache không được access trong 60 phút
    # use_temp_path=off : ghi thẳng vào cache dir (bỏ bước ghi tạm, ít I/O hơn)
    proxy_cache_path /tmp/nginx-cache
                     levels=1:2
                     keys_zone=api_cache:10m
                     max_size=1g
                     inactive=60m
                     use_temp_path=off;

    server {
        listen 80;

        # Không cache mặc định (phải opt-in từng location)
        proxy_cache off;

        # ─── Cache danh mục, config ít thay đổi ─────────────────────────
        location /api/categories {
            proxy_pass  http://swarm_backend;
            proxy_cache api_cache;

            # Cache 200 OK trong 10 phút; cache 404 trong 1 phút
            proxy_cache_valid 200 10m;
            proxy_cache_valid 404  1m;

            # Cache key = method + host + URI + query string
            # Mặc định không include $args → ?page=1 và ?page=2 được cache chung!
            proxy_cache_key "$request_method$host$request_uri$args";

            # Nếu backend đang cập nhật (updating), dùng cache cũ thay vì trả lỗi
            proxy_cache_use_stale error timeout updating
                                  http_500 http_502 http_503 http_504;

            # Lock: khi có nhiều request cùng lúc miss cache, chỉ 1 request đi backend
            # Các request còn lại chờ cache được fill → tránh "cache stampede"
            proxy_cache_lock on;

            # Header debug — client thấy HIT/MISS/EXPIRED/BYPASS
            add_header X-Cache-Status $upstream_cache_status;

            proxy_pass http://swarm_backend;
        }

        # ─── KHÔNG cache: các request thay đổi data ─────────────────────
        location /api/users/ {
            proxy_cache off;
            proxy_pass  http://swarm_backend;
        }

        # KHÔNG cache POST, PUT, DELETE (mặc định Nginx không cache nhưng nên explicit)
        location /api/ {
            proxy_cache off;
            proxy_pass  http://swarm_backend;
        }
    }
}
```

**Test cache hoạt động:**

```bash
# Lần 1: MISS (backend được gọi)
curl -I http://192.168.1.60/api/categories
# X-Cache-Status: MISS

# Lần 2: HIT (từ cache Nginx, backend KHÔNG được gọi)
curl -I http://192.168.1.60/api/categories
# X-Cache-Status: HIT

# Verify backend không nhận request thứ 2: xem access log backend
# Không thấy request thứ 2 → cache đang hoạt động đúng

# Xem file cache trên disk
ls /tmp/nginx-cache/ -la

# Force bypass cache (test / debug)
curl -H "Cache-Control: no-cache" http://192.168.1.60/api/categories
# X-Cache-Status: BYPASS

# Xóa toàn bộ cache khi cần purge thủ công
rm -rf /tmp/nginx-cache/*
docker exec nginx-practice nginx -s reload
```

---

### NX.11 Advanced — Security Headers + Connection Limits

```nginx
http {
    # Zone giới hạn connection đồng thời
    limit_conn_zone $binary_remote_addr zone=per_ip:10m;
    limit_conn_zone $server_name        zone=per_server:10m;

    server {
        listen 80;

        # ─── Security Headers ────────────────────────────────────────────
        # Chặn clickjacking: không cho nhúng trang vào iframe ngoài domain
        add_header X-Frame-Options "SAMEORIGIN" always;

        # Ngăn browser đoán content-type (MIME sniffing attack)
        add_header X-Content-Type-Options "nosniff" always;

        # Referrer Policy: không gửi full URL khi điều hướng sang domain khác
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;

        # Permissions Policy: tắt các tính năng browser không dùng
        add_header Permissions-Policy "geolocation=(), microphone=(), camera=()" always;

        # ─── Giới hạn connection ─────────────────────────────────────────
        # Tối đa 100 concurrent connection từ 1 IP
        limit_conn per_ip 100;
        # Tối đa 2000 concurrent connection đến toàn server
        limit_conn per_server 2000;
        limit_conn_status 429;

        # ─── Chặn bad bots / scanner ─────────────────────────────────────
        # 444 = Nginx đóng TCP ngay, không trả response gì (bảo tồn resource)
        if ($http_user_agent = "") { return 444; }

        if ($http_user_agent ~* (nmap|nikto|sqlmap|masscan|zgrab|dirbuster|hydra)) {
            return 444;
        }

        # Chặn truy cập file nhạy cảm
        location ~ /\.(env|git|htaccess|DS_Store) {
            deny all;
            return 404;
        }

        # Chặn path phổ biến của scanner (không có trong ứng dụng)
        location ~* \.(php|asp|aspx|jsp|cgi)$ {
            return 444;
        }

        # Admin panel: chỉ cho phép IP nội bộ
        location /admin/ {
            allow  192.168.1.0/24;
            deny   all;
            proxy_pass http://swarm_backend;
        }

        location / { proxy_pass http://swarm_backend; }
        location /nginx-health { access_log off; return 200 "healthy\n"; add_header Content-Type text/plain; }
    }

    server {
        listen 8080;
        location /nginx-status { stub_status on; access_log off; allow 127.0.0.1; allow 192.168.1.0/24; deny all; }
        location /nginx-health { access_log off; return 200 "ok\n"; add_header Content-Type text/plain; }
    }
}
```

---

### NX.12 — Cheatsheet Tech Lead: Nginx Directives quan trọng

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                 NGINX DIRECTIVES — TECH LEAD MUST KNOW                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  UPLOAD / BODY                                                               │
│    client_max_body_size 100m      ← default 1m → upload > 1MB bị 413       │
│    client_body_buffer_size 1m     ← RAM buffer body (default 8/16k)        │
│    client_body_timeout 120s       ← chờ client gửi mỗi chunk body          │
│    proxy_request_buffering off    ← stream upload, không cần buffer RAM     │
├─────────────────────────────────────────────────────────────────────────────┤
│  TIMEOUT (hay gặp nhất)                                                     │
│    proxy_connect_timeout 10s      ← TCP connect đến backend                │
│    proxy_send_timeout 300s        ← gửi request body tới backend           │
│    proxy_read_timeout 300s        ← chờ RESPONSE từ backend ← quan trọng! │
│    keepalive_timeout 65s          ← giữ keep-alive connection               │
│    client_body_timeout 120s       ← client upload qua mạng chậm            │
├─────────────────────────────────────────────────────────────────────────────┤
│  LOAD BALANCING                                                              │
│    round-robin (default)          ← request đều nhau, xử lý nhanh          │
│    least_conn                     ← tốt nhất cho API xử lý không đồng đều  │
│    ip_hash                        ← sticky session (cẩn thận scalability!)  │
│    weight=N                       ← backend mạnh hơn nhận nhiều hơn        │
│    max_fails=3 fail_timeout=30s   ← passive health check                   │
│    keepalive 32                   ← persistent TCP connection → giảm RTT   │
├─────────────────────────────────────────────────────────────────────────────┤
│  RATE LIMITING                                                               │
│    limit_req_zone $binary_remote_addr zone=z:10m rate=10r/s  ← khai báo   │
│    limit_req zone=z burst=20 nodelay  ← áp dụng, burst không delay        │
│    limit_conn_zone / limit_conn       ← giới hạn connection (≠ request)   │
│    limit_req_status 429               ← trả 429, không 503                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  BUFFER (ảnh hưởng performance và memory)                                   │
│    proxy_buffer_size 8k           ← đọc response header từ backend         │
│    proxy_buffers 8 32k            ← đọc response body (8×32k = 256k RAM)  │
│    proxy_busy_buffers_size 64k    ← đang gửi về client, giữ tối đa bao nhiêu│
│    proxy_buffering off            ← bắt buộc cho streaming / SSE / WS      │
├─────────────────────────────────────────────────────────────────────────────┤
│  GZIP                                                                        │
│    gzip on + gzip_proxied any     ← bật gzip kể cả response từ proxy       │
│    gzip_comp_level 5              ← 1-9, sweet spot 4-6                     │
│    gzip_min_length 1024           ← chỉ nén response > 1KB                 │
│    gzip_types application/json... ← khai báo content type cần nén          │
│    gzip_vary on                   ← Vary header cho CDN/browser cache       │
├─────────────────────────────────────────────────────────────────────────────┤
│  PROXY CACHE                                                                 │
│    proxy_cache_path /tmp/c levels=1:2 keys_zone=c:10m max_size=1g         │
│    proxy_cache api_cache          ← bật cache cho location                  │
│    proxy_cache_valid 200 10m      ← cache 200 OK trong 10 phút             │
│    proxy_cache_use_stale error timeout updating  ← dùng cache cũ khi lỗi  │
│    proxy_cache_lock on            ← chống cache stampede                    │
│    add_header X-Cache-Status $upstream_cache_status  ← debug HIT/MISS      │
├─────────────────────────────────────────────────────────────────────────────┤
│  SECURITY                                                                    │
│    server_tokens off                  ← ẩn Nginx version                   │
│    X-Frame-Options SAMEORIGIN        ← chặn clickjacking                   │
│    X-Content-Type-Options nosniff    ← chặn MIME sniffing                  │
│    return 444                         ← đóng TCP không trả response        │
│    allow IP / deny all               ← whitelist IP                        │
│    limit_conn per_ip 100             ← giới hạn connection đồng thời       │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Debug commands thường dùng nhất:**

```bash
# Test config trước khi reload — LUÔN chạy bước này trước
docker exec nginx-practice nginx -t

# Reload không downtime (áp dụng config mới, không drop connection cũ)
docker exec nginx-practice nginx -s reload

# Dump full config đang dùng (bao gồm tất cả include)
docker exec nginx-practice nginx -T

# Theo dõi error log realtime
docker exec nginx-practice tail -f /var/log/nginx/error.log

# Xem phân phối request đến từng backend
docker exec nginx-practice tail -100 /var/log/nginx/access.log \
  | grep -oP 'upstream="\K[^"]+' | sort | uniq -c | sort -rn

# Xem latency của từng backend
docker exec nginx-practice tail -100 /var/log/nginx/access.log \
  | awk '{print $8, $9}' | sort -k2 -n | tail -20
# Cột 8: upstream_addr, cột 9: upstream_rt → backend nào chậm nhất

# Connection stats realtime (curl bên trong container — tránh Docker NAT)
watch -n 1 'docker exec nginx-practice curl -s http://localhost:8080/nginx-status'

# Xem process nginx đang chạy bao nhiêu worker
docker exec nginx-practice ps aux | grep "nginx: worker"
```

**Tình huống → Directive cần thêm/sửa ngay:**

```
Tình huống                            | Fix
──────────────────────────────────────|──────────────────────────────────────────
Upload file > 1MB bị 413             | client_max_body_size 100m+
Upload qua mạng chậm bị timeout      | client_body_timeout 120s+
API export/report 5 phút bị 504      | proxy_read_timeout 600s
API streaming/SSE bị buffering       | proxy_buffering off
WebSocket bị ngắt sau 60s            | proxy_read_timeout 3600s; Upgrade header
Brute-force login                     | limit_req rate=3r/m burst=5 + limit_conn 5
DDoS HTTP Layer 7 nhẹ                | limit_req rate=30r/s burst=50
API JSON response lớn, bandwidth cao | gzip on; gzip_types application/json
API danh mục gọi nhiều lần           | proxy_cache + proxy_cache_valid 200 10m
Backend không đều (size request khác)| least_conn trong upstream block
1 backend mạnh hơn                   | weight=2 cho backend mạnh hơn
Scanner / bad bot                    | if ($http_user_agent ~* ...) return 444
```

---

### Cập nhật bảng VM sau khi thêm 192.168.1.60

| VM IP | Vai trò | Components |
|---|---|---|
| 192.168.1.36 | Virtual IP (Keepalived) | — |
| 192.168.1.37 | Nginx Master (production) | Nginx, Keepalived, Node Exporter, Nginx Exporter, cAdvisor |
| 192.168.1.43 | Nginx Backup (production) | Nginx, Keepalived |
| **192.168.1.60** | **Nginx Practice (session này)** | **Nginx — thực hành, không Keepalived** |
| 192.168.1.38 | DB Primary | SQL Server / PostgreSQL, Node Exporter |
| 192.168.1.39 | DB Secondary | SQL Server / PostgreSQL, Node Exporter |
| 192.168.1.40 | Swarm Manager | Docker, Node Exporter, cAdvisor |
| 192.168.1.41 | Swarm Worker1 | Docker, Node Exporter, cAdvisor |
| 192.168.1.42 | Swarm Worker2 | Docker, Node Exporter, cAdvisor |
| 192.168.1.50 | Private Registry | Docker Registry / Harbor |
| 192.168.1.51 | Monitoring | Prometheus, Grafana, InfluxDB |

> **Lộ trình thực hành đề xuất:**
> 1. ✅ Thực hành với VM .60 (session này) — làm quen Nginx load balancing + các directive nâng cao
> 2. → Part 1 (VM .37) — Nginx production có monitoring + Nginx Exporter
> 3. → Keepalived + VIP (VM .36/.37/.43) — HA setup sau khi đã nắm Nginx
