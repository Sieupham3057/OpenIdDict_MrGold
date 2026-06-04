#!/bin/bash
# Tạo self-signed certificate dùng chung cho tất cả API instance.
#
# Chạy 1 lần duy nhất trên VM1, sau đó scp file .pfx sang tất cả VM khác.
# Tất cả instance PHẢI dùng cùng 1 file .pfx để token có thể validate chéo.
#
# Cách dùng:
#   chmod +x gen-cert.sh
#   ./gen-cert.sh
#   scp openiddict.pfx bank@192.168.1.36:~/projects/OpenIdDict_MrGold/docker/certs/
set -euo pipefail

CERT_DIR="$(cd "$(dirname "$0")" && pwd)"
CERT_NAME="openiddict"
CERT_PASSWORD="${OPENIDDICT_CERT_PASSWORD:-Lab@OpenIddict2025}"

echo "=== Tạo OpenIddict certificate ==="
echo "Output: ${CERT_DIR}/${CERT_NAME}.pfx"
echo "Password: ${CERT_PASSWORD}"
echo ""

# Tạo private key + self-signed certificate (RSA 2048, hết hạn sau 5 năm)
openssl req -x509 \
  -newkey rsa:2048 \
  -keyout "${CERT_DIR}/${CERT_NAME}.key" \
  -out    "${CERT_DIR}/${CERT_NAME}.crt" \
  -days   1825 \
  -nodes \
  -subj "/CN=OpenIddict-AuthDemo/O=AuthDemo/C=VN"

# Export sang định dạng PKCS#12 (.pfx) mà .NET đọc được
openssl pkcs12 -export \
  -out    "${CERT_DIR}/${CERT_NAME}.pfx" \
  -inkey  "${CERT_DIR}/${CERT_NAME}.key" \
  -in     "${CERT_DIR}/${CERT_NAME}.crt" \
  -passout "pass:${CERT_PASSWORD}"

# Xóa file trung gian — chỉ giữ .pfx
rm -f "${CERT_DIR}/${CERT_NAME}.key" "${CERT_DIR}/${CERT_NAME}.crt"

# Cho phép container user (non-root) đọc file — openssl tạo ra 600 by default
chmod 644 "${CERT_DIR}/${CERT_NAME}.pfx"

echo ""
echo "=== Done ==="
echo "File: ${CERT_DIR}/${CERT_NAME}.pfx"
echo ""
echo "Tiếp theo — copy sang VM2 (thay IP/user cho đúng):"
echo "  scp ${CERT_DIR}/${CERT_NAME}.pfx bank@192.168.1.36:~/projects/OpenIdDict_MrGold/docker/certs/"
echo ""
echo "Đặt env vars trong docker-compose:"
echo "  OpenIddict__CertPath=/app/certs/openiddict.pfx"
echo "  OpenIddict__CertPassword=${CERT_PASSWORD}"
