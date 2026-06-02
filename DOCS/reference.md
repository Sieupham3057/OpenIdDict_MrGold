# LỘ TRÌNH KHOÁ HỌC BACKEND .NET
## Từ Junior → Mid/Senior trong 10–12 Tuần

- Lớp nhỏ 5–8 người
- Học tối 2 buổi/tuần
- Mentor 1-nhiều thực chiến

> **Noted:** Phần in đậm màu đỏ là optional nếu trong lớp có bạn học nhanh thì bổ túc thêm hoặc cả lớp học nhanh sẽ bổ túc thêm vì phần nâng cao sẽ khó tiếp cận. Khuyến khích các bạn cố gắng và nỗ lực học.

---

# TỔNG QUAN KHOÁ HỌC

| Nội dung | Chi tiết |
|---|---|
| Đối tượng | Developer backend 1–3 năm kinh nghiệm, muốn lên mid/senior nhanh |
| Thời gian | 2 buổi/tuần (tối) \| 2h – 2.5h / buổi |
| Thời lượng | 10 – 12 tuần (~20–24 buổi học) |
| Sĩ số | Tối đa 8 học viên / lớp (lớp nhỏ, review code cá nhân) |
| Công nghệ | .NET 8 (ASP.NET Core), EF Core, SQL Server, Index SQL, Docker Compose, Docker Swarm, CI/CD, Redis, RabbitMQ |
| Buổi học | Tối Thứ 3 & Thứ 5 (hoặc Thứ 2 & Thứ 4) – sẽ chốt theo vote lớp |
| Hình thức | Online qua Google Meet / Zoom |

---

# Bạn sẽ đạt được gì sau khoá học?

- Hiểu kiến trúc hệ thống thực tế: Layered Architecture, Clean Architecture, CQRS
- Deploy ứng dụng lên server/cloud bằng Docker + CI/CD (GitHub Actions hoặc Jenkins Pipeline)
- Debug & xử lý lỗi production tự tin: logging, tracing, profiling
- Nắm vững Performance Optimization: caching, async, database tuning, Index SQL Server
- Tự viết API chuẩn REST, bảo mật JWT/OAuth2, tích hợp Redis, Message Queue
- Trả lời phỏng vấn Senior .NET tự tin với mock interview thực chiến
- Có 1 project thực tế hoàn chỉnh đưa vào portfolio / CV

---

# LỘ TRÌNH TỔNG THỂ

Khoá học chia thành 4 giai đoạn, mỗi giai đoạn xây dựng trên nền của giai đoạn trước, dẫn đến project hoàn chỉnh cuối khoá.

---

# Giai đoạn 1 — Tuần 1–3
## NỀN TẢNG & KIẾN TRÚC

- Recap .NET 8 / C# hiện đại (records, pattern matching, minimal API)
- Layered Architecture vs Clean Architecture
- Dependency Injection thực chiến
- EF Core nâng cao: migrations, relationships, performance
- RESTful API design chuẩn, versioning, middleware pipeline

---

# Giai đoạn 2 — Tuần 4–6
## DEPLOY & DEVOPS THỰC CHIẾN

- Docker: containerize .NET app + SQL Server
- Docker Compose cho local dev environment
- GitHub Actions hoặc Jenkins Pipeline:
  - CI pipeline (build, test, lint)
- CD pipeline:
  - Tự động deploy lên VPS
- Environment config, secrets management, health checks

---

# Giai đoạn 3 — Tuần 7–9
## PERFORMANCE & DEBUG PRODUCTION

- Logging với Serilog + Seq / ELK stack
- Distributed Tracing (OpenTelemetry + Jaeger)
- Caching:
  - In-memory
  - Redis distributed cache
- Async/await, concurrency, rate limiting
- Query optimization, index strategy, EF Core profiling
- Message Queue với RabbitMQ / Azure Service Bus

---

# Giai đoạn 4 — Tuần 10–12
## PROJECT THỰC TẾ & PHỎNG VẤN

- Hoàn thiện project thực tế end-to-end
- Code review + refactor theo chuẩn senior
- Mock Interview:
  - Câu hỏi system design
  - Technical .NET
- Sửa CV, tối ưu LinkedIn / GitHub profile
- Q&A tổng kết, định hướng tiếp theo

---

# PROJECT THỰC TẾ CUỐI KHOÁ

Học viên sẽ xây dựng hệ thống **JobBoard API** (tương tự TopCV / LinkedIn Jobs) – một project thực tế đủ phức tạp để đưa vào CV senior.

---

# JobBoard API – Hệ thống Tuyển Dụng

## Auth & User
- JWT + Refresh Token
- Role:
  - Employer
  - Candidate
  - Admin
- OAuth2 Google

## Job Management
- CRUD Jobs
- Filter/search full-text
- Phân trang
- Soft delete

## Application
- Nộp đơn
- Upload CV
- Trạng thái đơn theo pipeline

## Notification
- Email thông báo (MailKit)
- In-app notification qua RabbitMQ

## Performance
- Redis cache danh sách job
- Output cache
- EF Core optimization

## Deploy
- Docker Compose local
- GitHub Actions CI/CD
- Deploy VPS/Azure

## Observability
- Serilog structured log
- OpenTelemetry tracing
- Health check endpoint

## Testing
- Unit test (xUnit + Moq)
- Integration test (WebApplicationFactory)

---

# Stack công nghệ sử dụng trong project

## Backend
- ASP.NET Core 8 Web API
- Clean Architecture

## ORM
- Entity Framework Core 8
- SQL Server

## Cache
- Redis (StackExchange.Redis)

## Message Queue
- RabbitMQ

## Auth
- JWT Bearer + Refresh
- Keycloak
- SSO IDP Server

## Storage
- Azure Blob Storage
- Hoặc MinIO self-hosted

## Containerisation
- Docker
- Docker Compose

## CI/CD
- GitHub Actions
- Jenkins Pipeline
- Docker
- VPS deploy

## Observability
- Serilog
- OpenTelemetry
- Jaeger

---

# PHƯƠNG PHÁP GIẢNG DẠY

## Live Coding
Mentor code trực tiếp, không slide lý thuyết khô khan. Học viên code theo và đặt câu hỏi ngay.

## Review Code Cá Nhân
Mỗi bài tập được review trực tiếp trên GitHub PR. Feedback cụ thể từng dòng code.

## Project Thực Tế
Xây dựng 1 project hoàn chỉnh từ đầu đến cuối, có thể đưa ngay vào CV.

## Mock Interview
Phỏng vấn giả lập với câu hỏi thực tế, feedback sau mỗi buổi.

## Group Discussion
Thảo luận nhóm 5–8 người – học từ cách tiếp cận của nhau.

## Recording
Tất cả buổi học được ghi hình, xem lại không giới hạn trong khoá học.

---

# CÂU HỎI THƯỜNG GẶP (FAQ)

## Q: Tôi chưa dùng Docker bao giờ, có theo kịp không?
**A:** Hoàn toàn ổn. Khoá học bắt đầu từ Docker cơ bản với mục đích học thực hành luôn, mentor sẽ hỗ trợ cài đặt và setup ngay buổi đầu.

---

## Q: Tôi đang dùng .NET Framework, có cần học .NET Core trước không?
**A:** Không cần. Khoá học sẽ bắt đầu với recap .NET 8, tuy nhiên bạn nên biết C# cơ bản (OOP, LINQ).

---

## Q: Lịch học có linh hoạt không nếu tôi bận 1 buổi?
**A:** Mỗi buổi đều được ghi hình. Bạn có thể xem lại và đặt câu hỏi qua group chat.

---

## Q: Sau khoá học tôi có được hỗ trợ tiếp không?
**A:** Tất cả học viên được vào group để hỗ trợ nhau lâu dài.

---

## Q: Tôi cần chuẩn bị gì trước khoá học?
**A:** Máy tính RAM ≥ 8GB, cài Visual Studio, Docker Desktop.

---

# SẴN SÀNG LÊN SENIOR?

Lớp chỉ 5–8 người. Đăng ký sớm để giữ chỗ.

**Liên hệ:** [Zalo / Facebook / Email]