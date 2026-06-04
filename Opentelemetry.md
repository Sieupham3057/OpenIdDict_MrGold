# OpenTelemetry — Giải thích & So sánh với Code Hiện Tại

---

## 1. OpenTelemetry là gì?

**OpenTelemetry (OTel)** là một **standard mở** (open standard) do CNCF (Cloud Native Computing Foundation) quản lý, cung cấp SDK, API và công cụ thống nhất để thu thập **dữ liệu observability** từ ứng dụng.

Mục tiêu: thay vì mỗi vendor (Datadog, Jaeger, Prometheus, Zipkin...) có SDK riêng, OTel cung cấp **một lớp trung gian duy nhất** — bạn instrument code một lần, export ra bất kỳ backend nào.

```
┌─────────────────────────────────────┐
│           Your Application          │
│   (instrument bằng OTel SDK)        │
└────────────────┬────────────────────┘
                 │ OTLP (OpenTelemetry Protocol)
                 ▼
┌─────────────────────────────────────┐
│        OTel Collector               │
│  (nhận, xử lý, route telemetry)     │
└──────┬──────────┬───────────────────┘
       │          │          │
       ▼          ▼          ▼
  Prometheus   Jaeger     Grafana
  (Metrics)   (Traces)    Loki(Logs)
```

---

## 2. Ba Trụ Cột Observability (The Three Pillars)

### Trụ cột 1 — Logs (Nhật ký)
- **Là gì**: Bản ghi sự kiện có timestamp. Ví dụ: `"User login failed for email X"`
- **Dùng để**: Debug lỗi cụ thể, xem chuỗi sự kiện
- **Giới hạn**: Không liên kết được các request qua nhiều service

### Trụ cột 2 — Metrics (Chỉ số)
- **Là gì**: Số đo tổng hợp theo thời gian. Ví dụ: `http_requests_total = 1500`
- **Dùng để**: Dashboard, alerting, trending
- **Giới hạn**: Không cho biết *request nào* bị chậm, chỉ biết *có bao nhiêu* request chậm

### Trụ cột 3 — Traces (Phân tán theo dõi)
- **Là gì**: Chuỗi span theo dõi một request đi qua nhiều service/layer
- **Dùng để**: Tìm bottleneck, debug latency trong microservices
- **Giới hạn**: Tốn storage nếu sample 100%

```
Trace: POST /api/auth/login  (total: 245ms)
  └── Span: Validate Request         (2ms)
  └── Span: Query User DB            (180ms)  ← bottleneck!
  └── Span: Verify Password Hash     (55ms)
  └── Span: Generate JWT             (8ms)
```

---

## 3. Khi nào và tại sao sử dụng OpenTelemetry?

### Nên dùng OTel khi:

| Tình huống | Lý do |
|---|---|
| **Microservices** (nhiều service giao tiếp nhau) | Distributed tracing là không thể thiếu — không có traces thì không biết request bị chậm ở service nào |
| **Production system** cần SLA/SLO | Correlate logs + metrics + traces để RCA (Root Cause Analysis) nhanh |
| **Cloud-native** (K8s, containers) | OTel là standard của CNCF ecosystem |
| **Multi-vendor** (muốn đổi backend) | Instrument một lần, export nhiều nơi |
| Cần **correlation** giữa log và trace | Gắn `trace_id` vào log để từ log jump thẳng vào trace |

### Không cần thiết khi:
- Monolith đơn giản, team nhỏ
- Môi trường development / học tập
- Đã có logging + metrics đủ dùng và không có microservices

---

## 4. Phân tích Code Hiện Tại — AuthDemo.Api

### Stack hiện tại:

```
Logging  → Serilog  (Console + File sink)
Metrics  → prometheus-net  (UseHttpMetrics + MapMetrics)
Tracing  → ❌ Không có
```

### Chi tiết từng thành phần:

#### Metrics — `prometheus-net` (dòng 62–64 trong Program.cs)
```csharp
app.UseHttpMetrics();  // tự động đo request count, duration, in-flight
app.MapMetrics();      // expose endpoint GET /metrics cho Prometheus scrape
```
**Đo được:**
- `http_requests_total` — tổng số request theo method/route/status
- `http_request_duration_seconds` — histogram latency
- `http_requests_in_progress` — số request đang xử lý

**Không đo được:**
- Thời gian query DB cụ thể
- Thời gian call service ngoài
- Correlation với log

#### Logging — Serilog
```csharp
app.UseSerilogRequestLogging();  // log mỗi HTTP request: method, path, status, ms
```
**Ghi được:**
- Structured logs ra Console + File
- HTTP request log tự động

**Không có:**
- `trace_id` trong log (không correlate với trace)
- Centralized log backend (Seq, Loki, Elasticsearch)

#### Tracing — Không có
Không có distributed tracing. Nghĩa là nếu một request vào `/api/auth/login` chậm, bạn không biết chậm ở bước nào (validate → query DB → hash → JWT).

---

## 5. So sánh: Code Hiện Tại vs OpenTelemetry

| Khả năng | Code Hiện Tại | OpenTelemetry |
|---|---|---|
| HTTP metrics | ✅ prometheus-net | ✅ OTel Metrics |
| Custom metrics | Cần `prometheus-net` API riêng | ✅ OTel API thống nhất |
| Structured logging | ✅ Serilog | ✅ OTel Logs |
| Log to file | ✅ Serilog File Sink | Qua OTel Collector |
| **Distributed tracing** | ❌ Không có | ✅ Traces + Spans |
| **Trace-Log correlation** | ❌ Không có | ✅ trace_id trong mọi log |
| **DB query timing** | ❌ Không rõ | ✅ EF Core instrumentation |
| Đổi backend dễ dàng | ❌ Khó (phụ thuộc prometheus-net) | ✅ Chỉ đổi exporter |
| Độ phức tạp setup | ✅ Đơn giản | ⚠️ Phức tạp hơn |

---

## 6. Nếu Migrate sang OpenTelemetry — Trông như thế nào?

### Packages cần thêm:
```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
```

### Cấu hình trong Program.cs:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()       // auto trace HTTP requests
        .AddEntityFrameworkCoreInstrumentation() // auto trace EF queries
        .AddOtlpExporter())                   // gửi đến Jaeger/Tempo
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()       // HTTP metrics (thay prometheus-net)
        .AddPrometheusExporter())             // vẫn expose /metrics cho Prometheus
    .WithLogging(logging => logging
        .AddOtlpExporter());                  // gửi log đến Loki/backend
```

### Kết quả: Trace tự động gắn vào log
```
[INF] HTTP POST /api/auth/login responded 200 in 245ms
      trace_id=abc123def456  span_id=789xyz
```
→ Từ log click vào Jaeger/Tempo xem chi tiết từng span.

---

## 7. Kết luận — Nên làm gì với AuthDemo.Api?

### Giữ nguyên nếu:
- Đây là monolith học tập / demo
- prometheus-net + Serilog đủ cho nhu cầu hiện tại
- Không cần distributed tracing

### Nên migrate sang OTel nếu:
- Tách thành microservices
- Deploy production cần RCA nhanh
- Muốn thêm Jaeger/Tempo để xem traces
- Muốn correlate log + trace (trace_id trong Serilog)

### Bước nhỏ có thể làm ngay (không cần full OTel):
Thêm `trace_id` vào Serilog log bằng cách enricher với `Activity.Current`:
```csharp
// Trong Serilog enricher — gắn TraceId vào mọi log entry
.Enrich.WithProperty("TraceId", Activity.Current?.TraceId.ToString() ?? "none")
```
Điều này cho bạn correlation log cơ bản mà không cần refactor toàn bộ stack.

---

**Tóm lại**: Code hiện tại (prometheus-net + Serilog) là **đủ tốt cho monolith đơn giản**. OpenTelemetry trở nên **cần thiết** khi bạn có microservices hoặc cần distributed tracing để debug latency production.
