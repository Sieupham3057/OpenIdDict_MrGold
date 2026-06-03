#!/bin/bash
# Tạo certificate cho OpenIddict — chạy 1 lần duy nhất khi setup production/multi-instance
# Cert này dùng để ký JWT token — KHÔNG phải TLS certificate
#
# Cách dùng:
#   chmod +x create-certs.sh
#   ./create-certs.sh                         # password mặc định
#   ./create-certs.sh "MyStrongPassword"      # tự đặt password

CERT_DIR="./certs"
CERT_PASSWORD="${1:-OpenIddict@2024}"

echo "=== Tạo OpenIddict Certificate ==="

if ! command -v openssl &> /dev/null; then
    echo "Lỗi: openssl không tìm thấy. Trên Windows dùng Git Bash hoặc WSL."
    exit 1
fi

mkdir -p "$CERT_DIR"

echo "[1/3] Sinh RSA 2048-bit private key..."
openssl genrsa -out "$CERT_DIR/openiddict.key" 2048 2>/dev/null

echo "[2/3] Tạo self-signed certificate (10 năm)..."
openssl req -new -x509 \
    -key "$CERT_DIR/openiddict.key" \
    -out "$CERT_DIR/openiddict.crt" \
    -days 3650 \
    -subj "/CN=OpenIddict/O=AuthDemo" 2>/dev/null

echo "[3/3] Đóng gói thành .pfx..."
openssl pkcs12 -export \
    -out "$CERT_DIR/openiddict.pfx" \
    -inkey "$CERT_DIR/openiddict.key" \
    -in  "$CERT_DIR/openiddict.crt" \
    -password pass:"$CERT_PASSWORD" 2>/dev/null

echo ""
echo "Tạo xong: $CERT_DIR/openiddict.pfx"
echo "  Password: $CERT_PASSWORD"
echo ""
echo "Bước tiếp theo:"
echo "  1. Tạo file docker/.env:"
echo "     echo 'OPENIDDICT_CERT_PASSWORD=$CERT_PASSWORD' > .env"
echo ""
echo "  2. Thêm vào .gitignore:"
echo "     docker/certs/"
echo ""
echo "  3. Copy cert sang VM2 khi cần scale:"
echo "     scp $CERT_DIR/openiddict.pfx user@VM2-IP:~/project/docker/certs/"
