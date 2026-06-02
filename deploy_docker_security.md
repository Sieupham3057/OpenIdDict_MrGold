# Bảo mật thông tin nhạy cảm trong Docker Deployment

## Vấn đề với file `.env`

Khi dùng `docker inspect <container>`, **tất cả biến môi trường đều hiển thị plaintext**, kể cả khi bạn không push `.env` lên Git:

```bash
docker inspect my-container
# Output hiển thị toàn bộ environment variables, bao gồm connection string, secret key...
```

Ngoài ra, `.env` file trên server cũng:
- Có thể bị đọc nếu server bị compromise
- Không có audit log ai đã đọc
- Khó rotate key khi bị lộ

---

## Docker Compose trên Linux Production

### Có cần cài thêm gì không?

**Không** — Docker Secrets là tính năng **có sẵn trong Docker Engine**, không cần plugin hay cài thêm gì.

Tuy nhiên cần phân biệt:

| Môi trường | Cơ chế Secrets | Mã hóa |
|-----------|---------------|--------|
| Docker Compose standalone | Mount file vào container | Không — file thường trên host |
| Docker Swarm | Raft consensus log | **Có** — AES-256 |

Bạn dùng Compose thì không cần Swarm, nhưng cũng không có mã hóa tự động. Dù vậy vẫn tốt hơn env var vì **không lộ qua `docker inspect`**.

---

### Cơ chế hoạt động của Docker Secrets

Docker Secrets trong Compose **không phải biến môi trường** — nó là **file được mount vào container**:

```
HOST (Linux server)                       CONTAINER
──────────────────────────────────────────────────────────
/run/secrets/db_connectionstring  ──────▶  /run/secrets/db_connectionstring
(file thường, chmod 600)    mount          (read-only, app đọc như file)
chỉ root trên host đọc được
```

**Tại sao `docker inspect` không thấy giá trị?**

Vì secret không được inject vào environment variables của process. Khi bạn chạy `docker inspect`, Docker chỉ trả về metadata của container — trong đó chỉ có *tên* secret, không có *nội dung*:

```bash
docker inspect my-container
# Thấy: "Secrets": [{"File": {"Name": "db_connectionstring"}}]
# KHÔNG thấy: "Server=prod-db;Password=SuperSecret123"
```

**Có đọc ra được không?**

- **Từ trong container** — Có, process (app .NET) đọc được vì đó là mục đích của nó. App cần đọc để kết nối database.
- **Từ ngoài container, trên host** — Chỉ user `root` mới đọc được file gốc. User thường không có quyền.
- **Qua `docker inspect`** — Không thấy giá trị.
- **Trong logs** — Không bị log ra, trừ khi code của bạn tự log nó (lỗi của developer).

---

### Giải thích `chmod 600` và `chown root:root`

Linux quản lý quyền file theo 3 nhóm: **owner** (chủ), **group** (nhóm), **others** (người khác).

```
chmod 600 = rw- --- ---
             │    │   └── others: không có quyền gì
             │    └────── group:  không có quyền gì
             └─────────── owner:  đọc (r) + ghi (w)
```

Ví dụ thực tế:
```bash
ls -la /run/secrets/
# -rw------- 1 root root 65 May 22 db_connectionstring
#  ↑↑↑↑↑↑↑↑↑   ↑↑↑↑ ↑↑↑↑
#  chmod 600   owner group
```

**Tại sao phải `chown root:root`?**

Vì nếu file thuộc user khác (ví dụ `ubuntu`, `deploy`), thì bất kỳ process nào chạy bằng user đó đều đọc được — kể cả process bị tấn công. Để file thuộc `root` thì chỉ root mới đọc được trực tiếp trên host.

```
Ai đọc được /run/secrets/db_connectionstring trên HOST?
├── root (sudo)          → Đọc được  ✓
├── user ubuntu          → Từ chối   ✗
├── user deploy          → Từ chối   ✗
├── hacker chiếm ubuntu  → Từ chối   ✗
└── container (app .NET) → Đọc được  ✓ (Docker mount riêng biệt)
```

Container đọc được vì Docker tạo một mount namespace riêng — app trong container thấy file đó như file của mình, không phụ thuộc quyền trên host.

---

### Phương án 1: Docker Secrets (Khuyến nghị cho Compose)

**Bước 1 — Tạo thư mục và file secret trên server:**

```bash
# Tạo thư mục (chỉ làm 1 lần)
sudo mkdir -p /run/secrets
sudo chmod 700 /run/secrets

# Tạo file secret — KHÔNG dùng echo (lưu vào shell history)
# Dùng nano hoặc printf
sudo nano /run/secrets/db_connectionstring
# Gõ vào nội dung: Server=prod-db;User=app;Password=SuperSecret123;Database=mydb
# Ctrl+X → Y → Enter để lưu

sudo nano /run/secrets/jwt_secret
# Gõ vào nội dung: your-super-secret-jwt-key-256-bits

# Khóa quyền
sudo chmod 600 /run/secrets/db_connectionstring
sudo chmod 600 /run/secrets/jwt_secret
sudo chown root:root /run/secrets/db_connectionstring
sudo chown root:root /run/secrets/jwt_secret

# Kiểm tra
sudo ls -la /run/secrets/
# -rw------- 1 root root 65 May 22 db_connectionstring
# -rw------- 1 root root 45 May 22 jwt_secret
```

**Bước 2 — Khai báo trong `docker-compose.yml`:**

```yaml
version: "3.8"

services:
  api:
    image: myapp:latest
    secrets:
      - db_connectionstring
      - jwt_secret
    # KHÔNG dùng environment để truyền giá trị secret

secrets:
  db_connectionstring:
    file: /run/secrets/db_connectionstring   # đường dẫn file trên host
  jwt_secret:
    file: /run/secrets/jwt_secret
```

**Bước 3 — Đọc secret trong .NET:**

```csharp
// Program.cs
// AddKeyPerFile đọc tất cả file trong thư mục, tên file = key, nội dung = value
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// Sau đó dùng bình thường
var connStr = builder.Configuration["db_connectionstring"];
// hoặc
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
```

Hoặc đọc thủ công từng file:

```csharp
var connectionString = File.Exists("/run/secrets/db_connectionstring")
    ? File.ReadAllText("/run/secrets/db_connectionstring").Trim()
    : builder.Configuration.GetConnectionString("DefaultConnection");
```

**Bước 4 — Chạy:**

```bash
sudo docker compose up -d
```

---

### Luồng hoạt động tổng quan

```
DevOps (có sudo) tạo file secret trên server
        │  sudo nano /run/secrets/db_connectionstring
        │  sudo chmod 600 ...
        ▼
File trên host — chỉ root đọc được
        │
        │  docker-compose.yml khai báo secrets:
        │    file: /run/secrets/db_connectionstring
        ▼
Docker mount file vào container tại /run/secrets/db_connectionstring
(read-only, isolated namespace)
        │
        ▼
App .NET đọc file → lấy connection string → kết nối DB
        │
        ▼
docker inspect → chỉ thấy tên "db_connectionstring", KHÔNG thấy giá trị
```

---

### Update / Rotate Secret khi cần thay đổi

Ví dụ: DB admin đổi password, hoặc JWT key bị lộ cần thay ngay.

```bash
# 1. Ghi đè file với giá trị mới
sudo nano /run/secrets/db_connectionstring
# Sửa password mới → lưu

# 2. Đảm bảo permission vẫn đúng (nano có thể reset)
sudo chmod 600 /run/secrets/db_connectionstring
sudo chown root:root /run/secrets/db_connectionstring

# 3. Restart container để app đọc lại file
sudo docker compose restart api

# Hoặc redeploy không downtime
sudo docker compose up -d --no-deps api
```

> **Lưu ý**: Secret là file trên host, container đọc lúc khởi động. Compose standalone **không có hot-reload** — phải restart container mới áp dụng giá trị mới.

---

### Phương án 2: External Secret Manager (Production chuẩn cao)

Dùng dịch vụ quản lý secret chuyên dụng khi có nhiều service hoặc cần audit log đầy đủ:

| Tool | Khi nào dùng |
|------|-------------|
| **HashiCorp Vault** | Self-hosted, kiểm soát hoàn toàn |
| **AWS Secrets Manager** | Deploy trên AWS |
| **Azure Key Vault** | Deploy trên Azure |
| **GCP Secret Manager** | Deploy trên GCP |

```csharp
// .NET đọc từ HashiCorp Vault
builder.Configuration.AddVaultConfiguration(options => {
    options.VaultUri = "http://vault:8200";
    options.SecretPath = "secret/myapp";
});
```

---

### Phương án 3: Environment Variables từ Systemd (Tối thiểu chấp nhận được)

```bash
# /etc/myapp/env — chmod 600, chỉ root đọc
DB_PASSWORD=SuperSecret123
JWT_SECRET=abc123
```

**Vẫn bị lộ qua `docker inspect`**, nhưng ít nhất file không thể đọc bởi user thường.

---

### So sánh các phương án Docker Compose

| Phương án | `docker inspect` | File trên host | Audit log | Độ phức tạp |
|-----------|-----------------|----------------|-----------|-------------|
| `.env` file thường | **Lộ** | Lộ nếu chmod sai | Không | Thấp |
| Systemd env file | **Lộ** | Ẩn (chmod 600) | Không | Thấp |
| Docker Secrets (file) | **Ẩn** | Ẩn (chmod 600) | Không | Thấp |
| Vault / Secret Manager | **Ẩn** | Không lưu trên host | **Có** | Cao |

---

## Docker Swarm

Swarm có **Secrets management tích hợp sẵn**, lưu encrypted trong Raft consensus log (cơ sở dữ liệu phân tán nội bộ của Swarm cluster):

```bash
# Tạo secret từ stdin — KHÔNG tạo file trung gian trên disk
echo "Server=prod-db;Password=Secret" | docker secret create db_connectionstring -

# Hoặc từ file rồi xóa ngay
docker secret create jwt_secret ./jwt_secret.txt
rm ./jwt_secret.txt   # xóa file gốc ngay sau khi tạo
```

**stack.yml:**
```yaml
version: "3.8"

services:
  api:
    image: myapp:latest
    secrets:
      - db_connectionstring
      - jwt_secret

secrets:
  db_connectionstring:
    external: true   # secret đã tạo qua CLI, không định nghĩa inline trong file
  jwt_secret:
    external: true
```

**Deploy:**
```bash
docker stack deploy -c stack.yml myapp
```

**Ưu điểm so với Compose standalone:**
- Encrypted at rest (AES-256) — ngay cả file trên disk của manager node cũng bị mã hóa
- Encrypted in transit (TLS) — khi truyền secret đến worker node
- Chỉ node đang chạy task mới nhận được secret
- Không hiển thị qua `docker inspect`
- Rotate: tạo secret mới → cập nhật service, không cần restart thủ công

---

## Kubernetes

K8s dùng **Secrets resource**, nhưng **mặc định chỉ base64 encode** (không encrypt — bất kỳ ai có quyền `kubectl get secret` đều đọc được):

```bash
# Tạo secret
kubectl create secret generic myapp-secrets \
  --from-literal=db-connectionstring="Server=prod;Password=Secret" \
  --from-literal=jwt-secret="abc123"
```

**deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
        - name: api
          image: myapp:latest
          # Cách 1: Mount thành file (an toàn hơn — không lộ qua env)
          volumeMounts:
            - name: secrets
              mountPath: /run/secrets
              readOnly: true
          # Cách 2: Inject làm env var (tiện nhưng kém an toàn hơn)
          env:
            - name: DB_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: myapp-secrets
                  key: db-connectionstring
      volumes:
        - name: secrets
          secret:
            secretName: myapp-secrets
```

**Tăng cường bảo mật K8s Secrets:**

1. **Encrypt at rest** — bật EncryptionConfiguration trong kube-apiserver:
```yaml
apiVersion: apiserver.config.k8s.io/v1
kind: EncryptionConfiguration
resources:
  - resources: [secrets]
    providers:
      - aescbc:
          keys:
            - name: key1
              secret: <base64-encoded-32-byte-key>
      - identity: {}
```

2. **External Secrets Operator** — đồng bộ từ Vault/AWS/Azure vào K8s Secrets tự động.

3. **Sealed Secrets** — encrypt secret trước khi commit vào Git (GitOps friendly).

---

## So sánh tổng quan 3 nền tảng

| | Docker Compose | Docker Swarm | Kubernetes |
|--|----------------|--------------|------------|
| **Cơ chế** | Mount file | Encrypted Raft log | Secrets Resource |
| **Encrypt at rest** | Không | **Có** (AES-256) | Không mặc định |
| **Encrypt in transit** | Không | **Có** (TLS) | **Có** (TLS) |
| **RBAC** | Không | Hạn chế | **Có** |
| **Rotate secret** | Thủ công (restart) | Có hỗ trợ | Có hỗ trợ |
| **Audit log** | Không | Hạn chế | **Có** |
| **Tích hợp Vault** | Cần tự làm | Cần tự làm | External Secrets Operator |
| **Độ phức tạp** | Thấp | Trung bình | Cao |

---

## Checklist bảo mật tối thiểu cho production

- [ ] Không commit `.env` lên Git (thêm vào `.gitignore`)
- [ ] Không để secret trong `Dockerfile` (lệnh `ENV`, `ARG`)
- [ ] File secret trên host: `chmod 600` + `chown root:root`
- [ ] Dùng Docker Secrets thay vì env var cho connection string, API key, JWT secret
- [ ] Không dùng `echo` để tạo file secret (lưu vào shell history) — dùng `nano` hoặc `printf`
- [ ] Rotate secret định kỳ hoặc khi có nhân sự rời đi
- [ ] Không log giá trị secret trong application log
- [ ] Audit ai có quyền `docker inspect` / `sudo` trên server
- [ ] Cân nhắc HashiCorp Vault nếu có nhiều service / nhiều môi trường

---

## Khuyến nghị cho dự án .NET + Docker Compose

```
Môi trường dev        → .env file (OK, không chứa production data)
Môi trường staging    → Docker Secrets (mount file, chmod 600)
Môi trường production → Docker Secrets + HashiCorp Vault (nếu budget cho phép)
```

Đọc secret trong .NET theo thứ tự ưu tiên:
```
/run/secrets/ (Docker Secret file)  →  Environment Variable  →  appsettings.json
```
