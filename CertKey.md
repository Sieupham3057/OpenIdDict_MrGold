# OpenIddict Certificate Configuration — Production Guide

## Tổng quan

Trong OpenIddict, có 3 loại key/certificate:

| Loại | Method (Dev) | Method (Prod) | Mục đích |
|------|-------------|---------------|----------|
| Ephemeral key | `AddEphemeralEncryptionKey()` | — | Chỉ sống trong RAM, mất sau restart |
| Self-signed (persistent) | `AddDevelopmentEncryptionCertificate()` | — | Lưu vào Data Protection store, sống qua restart nhưng tied vào 1 máy |
| Production certificate | — | `AddEncryptionCertificate(cert)` | Từ CA hoặc tự tạo, dùng được multi-instance |

> **Quy tắc**: Dùng 2 cert riêng biệt cho **signing** (ký token) và **encryption** (mã hóa token) để có thể rotate độc lập khi cần.

---

## Cách 1: File `.pfx` (Linux/Docker — phổ biến nhất)

### Bước 1 — Tạo certificate

```powershell
# Tạo private key + self-signed cert, valid 5 năm
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 1825 -nodes `
  -subj "/CN=OpenIddict Signing/O=YourCompany"

# Đóng gói thành file .pfx (PKCS#12)
openssl pkcs12 -export -out openiddict.pfx -inkey key.pem -in cert.pem -passout pass:YourStrongPassword
```

> **Lưu ý**: File `key.pem` chứa private key — KHÔNG commit lên git, KHÔNG để public.

### Bước 2 — Đặt file cert lên server

```
/etc/certs/
  openiddict-signing.pfx
  openiddict-encryption.pfx
```

Đặt permission chỉ cho app user đọc được:

```bash
chmod 600 /etc/certs/openiddict-signing.pfx
chown appuser:appuser /etc/certs/openiddict-signing.pfx
```

### Bước 3 — Cấu hình `appsettings.Production.json`

```json
{
  "OpenIddict": {
    "SigningCertPath": "/etc/certs/openiddict-signing.pfx",
    "EncryptionCertPath": "/etc/certs/openiddict-encryption.pfx",
    "SigningCertPassword": "",
    "EncryptionCertPassword": ""
  }
}
```

> Password để trống trong file, truyền qua environment variable để không lộ trong git.

### Bước 4 — Truyền password qua environment variable

```bash
# Linux / Docker
export OpenIddict__SigningCertPassword="YourStrongPassword"
export OpenIddict__EncryptionCertPassword="YourStrongPassword"
```

```yaml
# Docker Compose
services:
  api:
    environment:
      - OpenIddict__SigningCertPassword=YourStrongPassword
      - OpenIddict__EncryptionCertPassword=YourStrongPassword
```

### Bước 5 — Load cert trong `Program.cs`

```csharp
if (environment.IsDevelopment())
{
    options.AddEphemeralEncryptionKey()
           .AddEphemeralSigningKey();
}
else
{
    var config = builder.Configuration;

    // Load signing cert
    var signingCert = new X509Certificate2(
        config["OpenIddict:SigningCertPath"]!,
        config["OpenIddict:SigningCertPassword"],
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
    );

    // Load encryption cert
    var encryptionCert = new X509Certificate2(
        config["OpenIddict:EncryptionCertPath"]!,
        config["OpenIddict:EncryptionCertPassword"],
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
    );

    options.AddSigningCertificate(signingCert)
           .AddEncryptionCertificate(encryptionCert);
}
```

---

## Cách 2: Windows Certificate Store (Windows Server on-premise)

### Bước 1 — Import cert vào Windows Certificate Store

```powershell
Import-PfxCertificate `
    -FilePath .\openiddict-signing.pfx `
    -CertStoreLocation Cert:\LocalMachine\My `
    -Password (ConvertTo-SecureString "YourPassword" -AsPlainText -Force)
```

Sau khi import, lấy **Thumbprint** của cert:

```powershell
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*OpenIddict*" } | Select Thumbprint, Subject
```

### Bước 2 — Lưu Thumbprint vào config

```json
{
  "OpenIddict": {
    "SigningCertThumbprint": "A1B2C3D4E5F6...",
    "EncryptionCertThumbprint": "B2C3D4E5F6A1..."
  }
}
```

### Bước 3 — Load cert từ Windows Store trong `Program.cs`

```csharp
else
{
    X509Certificate2 FindCert(string thumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        return store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
            .OfType<X509Certificate2>()
            .Single(); // Ném exception nếu không tìm thấy — fail fast
    }

    var signingThumbprint    = builder.Configuration["OpenIddict:SigningCertThumbprint"]!;
    var encryptionThumbprint = builder.Configuration["OpenIddict:EncryptionCertThumbprint"]!;

    options.AddSigningCertificate(FindCert(signingThumbprint))
           .AddEncryptionCertificate(FindCert(encryptionThumbprint));
}
```

---

## Cách 3: Azure Key Vault (Cloud — khuyến nghị nhất)

### Bước 1 — Tạo cert và upload lên Key Vault

```powershell
# Tạo .pfx rồi convert sang base64
$certBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("openiddict-signing.pfx"))

# Upload lên Azure Key Vault dưới dạng Secret
az keyvault secret set `
    --vault-name "your-keyvault-name" `
    --name "openiddict-signing-cert" `
    --value $certBase64
```

> Tại sao dùng Secret thay vì Certificate object của Key Vault? Vì Key Vault Certificate không export được private key qua SDK dễ dàng. Lưu dưới dạng Secret (base64) thì load trực tiếp vào `X509Certificate2` đơn giản hơn.

### Bước 2 — Cài package

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

### Bước 3 — Kết nối Key Vault trong `Program.cs`

```csharp
// Đặt TRƯỚC builder.Build()
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault-name.vault.azure.net/"),
    new DefaultAzureCredential() // Tự động dùng Managed Identity trên Azure, hoặc az login khi dev
);
```

### Bước 4 — Load cert từ Key Vault

```csharp
else
{
    X509Certificate2 LoadCertFromKeyVault(string secretName)
    {
        var certBase64 = builder.Configuration[secretName]
            ?? throw new InvalidOperationException($"Secret '{secretName}' not found in Key Vault.");

        var certBytes = Convert.FromBase64String(certBase64);
        return new X509Certificate2(certBytes, (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
    }

    options.AddSigningCertificate(LoadCertFromKeyVault("openiddict-signing-cert"))
           .AddEncryptionCertificate(LoadCertFromKeyVault("openiddict-encryption-cert"));
}
```

### Bước 5 — Cấp quyền Managed Identity đọc Key Vault

```bash
# Cấp quyền "Get" Secret cho Managed Identity của App Service / AKS
az keyvault set-policy `
    --name "your-keyvault-name" `
    --object-id "<managed-identity-object-id>" `
    --secret-permissions get list
```

---

## Certificate Rotation (Thay cert mới không làm gián đoạn)

Khi cert sắp hết hạn, cần rotate mà không invalidate token đang active:

```csharp
// Thêm cert mới, GIỮ cert cũ lại để validate token cũ còn sống
options.AddSigningCertificate(newSigningCert)      // cert mới — dùng để ký token mới
       .AddSigningCertificate(oldSigningCert)      // cert cũ — vẫn dùng để validate token cũ
       .AddEncryptionCertificate(newEncryptionCert)
       .AddEncryptionCertificate(oldEncryptionCert);
```

Sau khi toàn bộ token cũ đã hết hạn (ví dụ 1 giờ nếu access token lifetime là 1 giờ), xóa cert cũ đi.

---

## Checklist trước khi lên production

- [ ] Cert được lưu ngoài source code (không commit vào git)
- [ ] Password/secret truyền qua environment variable hoặc Key Vault, không hardcode
- [ ] File `.pfx` có permission hạn chế (chỉ app user đọc được)
- [ ] Dùng 2 cert riêng cho signing và encryption
- [ ] Có kế hoạch rotate cert trước khi hết hạn
- [ ] Ephemeral/Development key đã bị tắt hoàn toàn ở môi trường production
- [ ] Test load cert thành công trước khi deploy

---

## Tóm tắt chọn cách nào

| Infrastructure | Khuyến nghị |
|----------------|-------------|
| Azure App Service / AKS | **Cách 3** — Azure Key Vault + Managed Identity |
| AWS EC2 / ECS | AWS Secrets Manager (tương tự Cách 3, dùng `AddSecretsManager`) |
| Windows Server on-premise | **Cách 2** — Windows Certificate Store |
| Linux Server / Docker tự quản | **Cách 1** — File `.pfx` + env var password |
| Staging / internal | Cách 1 đơn giản, hoặc `AddDevelopmentEncryptionCertificate()` tạm |
