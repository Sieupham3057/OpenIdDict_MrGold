# Grafana Dashboard — Cách đọc dashboard k6 + API với góc nhìn Technical Leader

Tài liệu này dùng cho lab hiện tại của bạn:

```text
API:        http://192.168.1.35:5000
Grafana:    http://192.168.1.35:3000
Prometheus: http://192.168.1.35:9090
InfluxDB:   http://192.168.1.35:8086
```

Luồng test bạn đang chạy:

```bash
k6 run \
  --out influxdb=http://localhost:8086/k6 \
  --env BASE_URL=http://localhost:5000 \
  k6/load-test.js
```

Script `load-test.js` đang chạy tối đa **1000 VUs** trong khoảng **13 phút**, theo các stage:

```javascript
{ duration: '1m', target: 50  }
{ duration: '3m', target: 200 }
{ duration: '5m', target: 500 }
{ duration: '2m', target: 1000 }
{ duration: '2m', target: 0   }
```

Nghĩa là tải không tăng đột ngột ngay từ đầu, mà tăng dần để mình quan sát hệ thống bắt đầu yếu ở ngưỡng nào.

---

## 1. Mục tiêu khi xem dashboard là gì?

Với vai trò **Technical Leader**, mình không chỉ xem “có biểu đồ hay không”. Mình cần trả lời 5 câu hỏi:

```text
1. Hệ thống có còn sống không?
2. Tải đang vào hệ thống bao nhiêu?
3. User đang cảm nhận nhanh hay chậm?
4. Có lỗi không? Lỗi bắt đầu từ ngưỡng tải nào?
5. Nếu chậm/lỗi thì bottleneck nằm ở API, DB, CPU, memory, thread pool hay logic code?
```

Dashboard giúp mình nhìn được **tương quan theo thời gian**:

```text
k6 tăng Virtual Users
        ↓
Request/sec tăng
        ↓
P95 latency tăng?
        ↓
Error rate tăng?
        ↓
API CPU / RAM / Thread / GC / DB có dấu hiệu bất thường?
```

Nếu chỉ nhìn một biểu đồ riêng lẻ thì dễ kết luận sai. Technical Leader phải đọc dashboard theo **chuỗi nguyên nhân - kết quả**.

---

## 2. Có 2 dashboard chính cần đọc

### Dashboard 1 — k6 Manual Load Testing Dashboard

Nguồn dữ liệu:

```text
k6 → InfluxDB → Grafana
```

Dashboard này trả lời câu hỏi:

```text
Người dùng giả lập đang gặp trải nghiệm như thế nào?
```

Các panel chính:

| Panel | Ý nghĩa |
|---|---|
| Virtual Users | Số user ảo đang chạy tại từng thời điểm |
| Requests Per Second | Số request k6 gửi mỗi giây |
| P95 Response Time | 95% request hoàn thành dưới bao nhiêu thời gian |
| Average Response Time | Thời gian phản hồi trung bình |
| Error Rate % | Tỷ lệ request fail |
| Checks Per Second | Số assertion/check k6 thực hiện mỗi giây |

---

### Dashboard 2 — API dashboard / api container

Nguồn dữ liệu:

```text
.NET API /metrics → Prometheus → Grafana
```

Dashboard này trả lời câu hỏi:

```text
Bên trong API đang khỏe hay đang chịu áp lực?
```

Các panel chính:

| Panel | Ý nghĩa |
|---|---|
| API Target UP | Prometheus có scrape được API không |
| API Request Rate | API nhận bao nhiêu request/giây |
| API P95 Latency | Latency từ góc nhìn API |
| API Process Memory | RAM process .NET đang dùng |
| API Process CPU % | CPU process .NET đang dùng |
| API Process Threads | Số thread của process API |

> Với lab hiện tại, không lấy CPU/RAM container từ cAdvisor vì cAdvisor trên server đang không expose được container label `api`. Thay vào đó dùng `.NET process metrics` và `docker stats`.

---

## 3. Cách đọc Dashboard k6

## 3.1 Virtual Users

Panel này cho biết tại thời điểm đó có bao nhiêu user ảo đang chạy.

Ví dụ trong test hiện tại:

```text
0 → 50 → 200 → 500 → 1000 → 0
```

Khi nhìn panel này, cần đối chiếu với các panel khác:

```text
VU tăng mà latency vẫn ổn  → hệ thống chịu tải tốt
VU tăng mà latency tăng mạnh → hệ thống bắt đầu bão hòa
VU tăng mà error rate tăng → hệ thống đã vượt ngưỡng chịu tải
```

### Technical Leader cần hỏi

```text
Hệ thống bắt đầu chậm từ mốc bao nhiêu VU?
Ở 200 VU ổn không?
Ở 500 VU ổn không?
Ở 1000 VU còn dùng được không?
```

Mục tiêu không phải chỉ là “đạt 1000 VU”, mà là biết **đạt 1000 VU với latency và error rate như thế nào**.

---

## 3.2 Requests Per Second

Panel này cho biết k6 đang gửi bao nhiêu request mỗi giây.

Ví dụ bạn đang thấy RPS tăng dần khi VU tăng. Đây là bình thường.

### Cách đọc

| Hiện tượng | Ý nghĩa |
|---|---|
| VU tăng, RPS tăng | Bình thường, hệ thống còn xử lý được |
| VU tăng, RPS đứng ngang | Hệ thống bắt đầu bão hòa |
| VU tăng, RPS giảm | Hệ thống quá tải, request bị chậm hoặc fail |
| RPS dao động mạnh | Có thể có bottleneck, DB lock, GC pause, timeout |

### Technical Leader cần quan tâm

```text
Throughput tối đa hệ thống đạt được là bao nhiêu req/s?
Sau khi vượt ngưỡng, RPS có tăng tiếp không hay bị chững lại?
```

Nếu RPS không tăng dù VU tăng, nghĩa là thêm user không giúp hệ thống xử lý nhiều hơn. Khi đó hệ thống đã đạt giới hạn.

---

## 3.3 P95 Response Time

Đây là panel quan trọng nhất.

P95 nghĩa là:

```text
95% request nhanh hơn giá trị này.
5% request còn lại chậm hơn giá trị này.
```

Ví dụ:

```text
P95 = 800ms
```

Nghĩa là 95% request hoàn thành dưới 800ms.

Nếu:

```text
P95 = 8s
```

Nghĩa là 5% user đang gặp request rất chậm, trải nghiệm xấu.

### Vì sao không chỉ xem Average?

Average dễ bị đánh lừa.

Ví dụ:

```text
9 request = 100ms
1 request = 10s
Average có thể vẫn nhìn không quá tệ
Nhưng user gặp request 10s sẽ thấy hệ thống đơ
```

Vì vậy khi đánh giá performance, ưu tiên:

```text
P95 > P99 > Average
```

### Ngưỡng tham khảo

| P95 | Đánh giá |
|---:|---|
| < 300ms | Rất tốt |
| 300ms - 800ms | Tốt / chấp nhận được |
| 800ms - 2s | Cảnh báo, cần theo dõi |
| 2s - 5s | Xấu, cần điều tra |
| > 5s | Rất xấu, user sẽ cảm thấy hệ thống đơ |

Với ảnh dashboard bạn gửi, P95 có lúc tăng lên nhiều giây khi VU tăng cao. Điều này cho thấy hệ thống bắt đầu chịu áp lực ở giai đoạn tải cao.

### Technical Leader cần hỏi

```text
P95 bắt đầu tăng mạnh tại thời điểm nào?
Lúc đó VU là bao nhiêu?
RPS là bao nhiêu?
Error rate có tăng theo không?
API CPU/Thread/Memory có tăng theo không?
SQL Server có wait/lock không?
```

---

## 3.4 Average Response Time

Average response time là thời gian phản hồi trung bình.

Panel này vẫn hữu ích, nhưng không nên dùng nó làm tiêu chí chính.

### Cách đọc

| Hiện tượng | Ý nghĩa |
|---|---|
| Average thấp, P95 cao | Có một nhóm request bị chậm bất thường |
| Average và P95 cùng tăng | Toàn hệ thống đang chậm |
| Average ổn, Error Rate tăng | Request fail nhanh, cần xem status code/log |

### Technical Leader cần quan tâm

Nếu Average thấp nhưng P95 cao, nghĩa là đa số user ổn, nhưng một nhóm user bị rất chậm. Đây thường là dấu hiệu:

```text
DB lock
GC pause
Thread pool starvation
Một endpoint cụ thể chậm
Một số request write bị nghẽn
```

---

## 3.5 Error Rate %

Panel này cho biết tỷ lệ request fail.

Trong k6, request fail thường đến từ:

```text
HTTP 4xx / 5xx
Timeout
Check fail
Network error
```

### Ngưỡng tham khảo

| Error Rate | Đánh giá |
|---:|---|
| 0% | Tốt nhất |
| < 0.1% | Chấp nhận được với tải cao |
| 0.1% - 1% | Cảnh báo |
| 1% - 5% | Xấu |
| > 5% | Fail test |

Trong script của bạn đang đặt threshold:

```javascript
http_req_failed: ['rate<0.05']
```

Tức là error rate phải nhỏ hơn 5%.

Với Technical Leader, 5% chỉ nên là ngưỡng stress test. Với production thực tế, nên đặt mục tiêu thấp hơn:

```text
Smoke test: 0%
Load test bình thường: < 0.1% hoặc < 1%
Stress test: có thể chấp nhận lỗi để tìm giới hạn
```

### Cần cảnh giác khi nào?

```text
Error rate bắt đầu > 0 khi VU tăng
Error rate tăng cùng lúc P95 tăng
Error rate tăng nhưng API CPU thấp
```

Trường hợp thứ ba thường gợi ý vấn đề DB, connection pool, timeout hoặc logic nghiệp vụ.

---

## 3.6 Checks Per Second

Checks là các assertion trong k6 script, ví dụ:

```javascript
check(res, { 'login 200': (r) => r.status === 200 });
check(res, { 'products 200': (r) => r.status === 200 });
check(res, { 'create order 201': (r) => r.status === 201 });
```

Panel Checks Per Second cho biết mỗi giây có bao nhiêu check được thực hiện.

### Cách đọc

| Hiện tượng | Ý nghĩa |
|---|---|
| Checks tăng theo RPS | Bình thường |
| Checks giảm khi VU tăng | Request đang chậm/fail khiến flow không chạy tiếp |
| Checks vẫn nhiều nhưng Error Rate tăng | API trả response nhưng không đúng kỳ vọng |

---

# 4. Cách đọc API dashboard

## 4.1 API Target UP

Query thường dùng:

```promql
up{job="dotnet-api"}
```

Giá trị:

```text
1 = API đang được Prometheus scrape thành công
0 = API down hoặc Prometheus không scrape được
```

Panel này phải luôn là `1`.

### Nếu API Target UP = 0

Kiểm tra:

```bash
docker ps
curl http://localhost:5000/health
curl http://localhost:5000/metrics | head
docker logs api --tail 100
```

Nếu target down thì các panel Prometheus khác có thể mất data.

---

## 4.2 API Request Rate

Query thường dùng:

```promql
sum(rate(http_requests_received_total{job="dotnet-api"}[1m]))
```

Panel này đo số request/giây mà API thực sự nhận.

### So sánh với k6 Requests Per Second

| Trường hợp | Ý nghĩa |
|---|---|
| k6 RPS ≈ API RPS | Bình thường |
| k6 RPS cao hơn API RPS nhiều | Có request fail trước khi tới API, network/proxy issue |
| API RPS tăng nhưng latency vẫn thấp | Hệ thống chịu tải tốt |
| API RPS đứng ngang trong khi VU tăng | API/DB đã đạt ngưỡng xử lý |

---

## 4.3 API P95 Latency

Query thường dùng:

```promql
histogram_quantile(
  0.95,
  sum(rate(http_request_duration_seconds_bucket{job="dotnet-api"}[5m])) by (le)
)
```

Panel này đo latency từ metrics của API.

### So sánh API P95 với k6 P95

| So sánh | Ý nghĩa |
|---|---|
| API P95 ≈ k6 P95 | Thời gian chậm nằm chủ yếu trong API/server |
| k6 P95 cao hơn API P95 nhiều | Có overhead network, client wait, DNS, connection, hoặc k6 side |
| API P95 cao nhưng CPU thấp | Có thể API đang chờ DB/I/O |
| API P95 cao và CPU cao | CPU bottleneck hoặc code xử lý nặng |

Trong ảnh bạn gửi, k6 P95 và API P95 đều tăng khi tải tăng. Đây là dấu hiệu chậm nằm ở phía server/API hoặc phụ thuộc phía sau API như SQL Server.

---

## 4.4 API Process Threads

Metric thường dùng:

```promql
process_num_threads{job="dotnet-api"}
```

Panel này cho biết process API đang có bao nhiêu thread.

### Cách đọc

| Hiện tượng | Ý nghĩa |
|---|---|
| Thread tăng nhẹ khi load tăng | Bình thường |
| Thread tăng liên tục không giảm | Cần kiểm tra thread leak hoặc blocking code |
| Thread tăng mạnh cùng latency | Có thể app đang bị blocking I/O hoặc thread pool pressure |

Với .NET, thread tăng khi tải tăng là bình thường. Nhưng nếu thread cứ tăng không hạ sau khi test kết thúc, cần điều tra.

### Technical Leader cần hỏi

```text
Sau khi k6 ramp down về 0, số thread có giảm/ổn định lại không?
Nếu không giảm, có khả năng blocking operation hoặc resource leak không?
```

---

## 4.5 API Process Memory

Query:

```promql
process_resident_memory_bytes{job="dotnet-api"}
```

Metric này đo RAM thực tế process .NET đang chiếm.

### Unit trong Grafana

```text
Standard options → Unit → bytes
```

### Cách đọc

| Hiện tượng | Ý nghĩa |
|---|---|
| Memory tăng khi load tăng rồi ổn định | Bình thường |
| Memory tăng rồi giảm sau GC/ramp down | Bình thường |
| Memory tăng liên tục không giảm | Cảnh báo memory leak |
| Memory tăng mạnh cùng Error Rate | Có thể app bị áp lực RAM/GC |

### Sau khi test xong cần xem gì?

Khi VU đã ramp down về 0, quan sát thêm 5-10 phút:

```text
Memory có quay về gần baseline không?
Hay tiếp tục nằm ở mức cao?
```

Nếu không giảm, cần xem thêm GC metrics hoặc dùng profiler/dotnet-dump.

---

## 4.6 API Process CPU %

Query:

```promql
rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100
```

### Cách hiểu

`process_cpu_seconds_total` là counter tổng số giây CPU đã dùng từ khi process start.

Dùng `rate(...[1m])` để tính CPU dùng mỗi giây.

Ví dụ:

```text
0.5  = 50% của 1 core
1.0  = 100% của 1 core
2.0  = 200%, tức dùng hết 2 cores
```

Vì server của bạn có 2 cores, nếu query đã nhân `* 100`:

```text
100% = tương đương 1 core
200% = tương đương 2 cores
```

### Ngưỡng tham khảo với server 2 cores

| CPU API | Đánh giá |
|---:|---|
| < 50% | Nhẹ |
| 50% - 120% | Bình thường khi load test |
| 120% - 180% | Cảnh báo, gần hết CPU |
| ~200% | API dùng gần hết 2 cores |

Nếu CPU cao và P95 tăng, khả năng lớn là CPU bottleneck hoặc code xử lý nặng.

Nếu CPU thấp mà P95 tăng, khả năng lớn là API đang chờ DB/I/O.

---

# 5. Cách đọc dashboard theo timeline

Khi chạy load test, không đọc từng panel riêng lẻ. Hãy đọc theo timeline:

```text
Bước 1: Nhìn Virtual Users
        ↓
Bước 2: Nhìn Requests Per Second
        ↓
Bước 3: Nhìn P95 Response Time
        ↓
Bước 4: Nhìn Error Rate
        ↓
Bước 5: Nhìn API Request Rate / API P95
        ↓
Bước 6: Nhìn API CPU / Memory / Threads
        ↓
Bước 7: Nếu API không rõ nguyên nhân, kiểm tra SQL Server
```

---

## 5.1 Kịch bản tốt

```text
VU tăng
RPS tăng
P95 vẫn thấp
Error Rate = 0
API CPU tăng vừa phải
Memory ổn định
Threads ổn định
```

Kết luận:

```text
Hệ thống chịu tải tốt ở mức hiện tại.
Có thể tăng tiếp VU hoặc tăng duration để test độ bền.
```

---

## 5.2 Kịch bản CPU bottleneck

```text
VU tăng
RPS tăng rồi đứng ngang
P95 tăng mạnh
Error Rate có thể tăng
API CPU gần 100%-200% trên máy 2 cores
Thread tăng
```

Kết luận:

```text
API đang bị nghẽn CPU hoặc có code xử lý đồng bộ/nặng.
```

Hướng xử lý:

```text
1. Tối ưu code hot path
2. Giảm blocking call
3. Kiểm tra serialization/json mapping
4. Cache response đọc nhiều
5. Scale out thêm API instance
6. Tăng CPU nếu cần
```

---

## 5.3 Kịch bản DB bottleneck

```text
VU tăng
P95 tăng mạnh
API CPU không cao
Memory không bất thường
RPS bị chững
SQL Server CPU/IO/wait tăng
```

Kết luận:

```text
API không bận CPU, mà đang chờ SQL Server.
```

Hướng xử lý:

```text
1. Kiểm tra slow query
2. Thêm index
3. Kiểm tra N+1 query
4. Tối ưu transaction
5. Kiểm tra lock/deadlock
6. Tăng connection pool hợp lý
7. Tách SQL Server sang máy riêng nếu cần
```

Query SQL cần chạy khi đang test:

```sql
SELECT TOP 10
    r.session_id,
    r.status,
    r.wait_type,
    r.wait_time / 1000.0 AS wait_sec,
    r.total_elapsed_time / 1000.0 AS elapsed_sec,
    r.logical_reads,
    SUBSTRING(t.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
          ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) AS query_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50
ORDER BY r.total_elapsed_time DESC;
```

---

## 5.4 Kịch bản memory leak / GC pressure

```text
VU tăng
Memory tăng
P95 tăng theo từng đợt
Sau khi VU về 0, memory không giảm
Gen2 GC tăng liên tục nếu có GC metrics
```

Kết luận:

```text
Có thể có memory leak hoặc cấp phát object quá nhiều.
```

Hướng xử lý:

```text
1. Kiểm tra object allocation
2. Kiểm tra cache không giới hạn
3. Kiểm tra static list/dictionary
4. Kiểm tra DbContext bị giữ lâu
5. Dùng dotnet-counters / dotnet-dump / profiler
```

---

## 5.5 Kịch bản lỗi nghiệp vụ / HTTP 500

```text
Error Rate tăng
P95 có thể tăng hoặc không
API CPU không nhất thiết cao
Log API có exception
```

Hướng xử lý:

```bash
docker logs api --tail 200 -f
```

Cần phân loại lỗi:

```text
401/403 → token/auth/permission
404 → route/data không tồn tại
409 → conflict/concurrency
500 → bug hoặc exception server
timeout → server/DB quá tải
```

---

# 6. Cách dùng docker stats song song với Grafana

Vì lab hiện tại không dùng cAdvisor làm nguồn chính cho CPU/RAM container, hãy mở terminal riêng:

```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

Khi chạy k6, quan sát:

```text
api          CPU/RAM của API container
sqlserver    SQL Server có đang ăn CPU/RAM không
influxdb     Có bị quá tải khi k6 ghi metrics không
grafana      Thường nhẹ
prometheus   Thường nhẹ
```

### Cách đọc docker stats

| Container | Cần quan tâm gì |
|---|---|
| api | CPU cao hay memory tăng bất thường |
| sqlserver | CPU/RAM cao khi order/write nhiều |
| influxdb | Có tăng CPU mạnh khi k6 ghi nhiều data không |
| prometheus | Có scrape ổn không |
| grafana | Thường không phải bottleneck |

Nếu dashboard P95 tăng mà `sqlserver` trong docker stats cũng tăng mạnh, rất có thể DB là bottleneck.

---

# 7. Đọc kết quả test hiện tại của bạn

Dựa trên ảnh bạn gửi:

```text
Virtual Users đang tăng dần.
Requests Per Second tăng theo VU.
Error Rate gần như 0.
Average Response Time tăng nhưng chưa phải tệ nhất.
P95 Response Time có lúc tăng lên nhiều giây.
API Request Rate tăng theo tải.
API Target UP = 1.
API Process Threads tăng khi tải tăng.
API P95 Latency cũng tăng theo.
```

Diễn giải ban đầu:

```text
Hệ thống vẫn sống và vẫn nhận request.
Load test đang tạo được tải thật.
Chưa thấy lỗi nhiều.
Nhưng latency P95 bắt đầu tăng mạnh khi VU/RPS tăng cao.
```

Nhận định Technical Leader:

```text
Đây là dấu hiệu hệ thống bắt đầu tiến gần hoặc đi qua ngưỡng tải ổn định.
Cần xác định mốc VU/RPS mà P95 bắt đầu vượt ngưỡng.
Sau đó kiểm tra API CPU, memory, thread và SQL Server để tìm bottleneck.
```

---

# 8. Ngưỡng quyết định pass / warning / fail

## 8.1 Cho Smoke Test

| Metric | Pass |
|---|---|
| Error Rate | 0% |
| Checks | 100% hoặc gần 100% |
| P95 | Không quá quan trọng, nhưng nên < 1s |
| API Target UP | 1 |

Smoke test không dùng để đánh giá performance. Nó chỉ dùng để xác nhận flow đúng.

---

## 8.2 Cho Load Test bình thường

| Metric | Pass | Warning | Fail |
|---|---:|---:|---:|
| Error Rate | 0 - 0.1% | 0.1 - 1% | > 1% |
| P95 Response Time | < 800ms | 800ms - 2s | > 2s |
| P99 Response Time | < 2s | 2s - 5s | > 5s |
| API Target UP | 1 | - | 0 |
| API CPU | < 120% | 120 - 180% | ~200% trên máy 2 cores |
| Memory | Ổn định | Tăng chậm | Tăng liên tục không giảm |

---

## 8.3 Cho Stress Test

Stress test có thể fail, vì mục tiêu là tìm giới hạn.

Cần ghi lại:

```text
Tại bao nhiêu VU thì P95 vượt 2s?
Tại bao nhiêu RPS thì Error Rate bắt đầu > 1%?
Tại thời điểm fail, API CPU/RAM/Thread/SQL như thế nào?
Sau khi ramp down, hệ thống có tự hồi phục không?
```

---

# 9. Quy trình xử lý khi thấy cảnh báo

## Case 1 — P95 tăng nhưng Error Rate = 0

Ý nghĩa:

```text
Hệ thống còn xử lý được, nhưng đang chậm.
```

Làm tiếp:

```text
1. Xác định mốc VU/RPS bắt đầu chậm
2. Xem API CPU
3. Nếu CPU cao → CPU/code bottleneck
4. Nếu CPU thấp → kiểm tra SQL Server
5. Xem endpoint nào chậm nhất nếu có route metrics
```

---

## Case 2 — P95 tăng và Error Rate tăng

Ý nghĩa:

```text
Hệ thống đã quá tải hoặc có lỗi server.
```

Làm tiếp:

```bash
docker logs api --tail 200
```

Kiểm tra:

```text
Có exception 500 không?
Có timeout DB không?
Có connection pool exhausted không?
Có deadlock không?
```

---

## Case 3 — API CPU cao

Làm tiếp:

```text
1. Tìm endpoint nào gọi nhiều/chậm
2. Kiểm tra code sync/blocking
3. Kiểm tra mapping/serialization
4. Cache dữ liệu đọc nhiều
5. Scale out API
```

---

## Case 4 — API CPU thấp nhưng P95 cao

Khả năng cao:

```text
SQL Server bottleneck
External I/O
Lock/transaction
Connection pool
```

Làm tiếp:

```text
1. Chạy SQL DMV query
2. Xem wait_type
3. Xem logical_reads
4. Kiểm tra index
5. Kiểm tra transaction write order
```

---

## Case 5 — Memory tăng liên tục

Làm tiếp:

```text
1. Quan sát sau ramp down 5-10 phút
2. Nếu không giảm, nghi memory leak
3. Kiểm tra cache/static collection
4. Dùng dotnet-counters hoặc dotnet-dump
```

---

# 10. Checklist cho Technical Leader sau mỗi lần test

Sau mỗi lần chạy k6, ghi lại các thông tin sau:

```text
Ngày giờ test:
Git commit / image version:
Server specs:
Kịch bản test: smoke/load/stress/spike
Max VU:
Max RPS:
P95 cao nhất:
P99 cao nhất nếu có:
Error Rate cao nhất:
API CPU cao nhất:
API Memory cao nhất:
SQL Server CPU/RAM nếu có:
Endpoint chậm nhất:
Lỗi chính nếu có:
Kết luận pass/warning/fail:
Action items:
```

Mẫu kết luận:

```text
Load test 1000 VU hoàn thành.
Hệ thống không lỗi nghiêm trọng, Error Rate ~0%.
Tuy nhiên P95 tăng mạnh khi VU vượt khoảng 500-700.
API vẫn UP, request rate tăng theo tải.
Cần kiểm tra thêm SQL Server wait stats và API CPU để xác định bottleneck.
Khuyến nghị chưa dùng mức 1000 VU làm capacity an toàn cho production.
```

---

# 11. Dashboard nào cần để trong màn hình monitoring chính?

Nên có 2 dashboard:

## Dashboard A — k6 Load Test Overview

Các panel:

```text
Virtual Users
Requests Per Second
P95 Response Time
Average Response Time
Error Rate %
Checks Per Second
```

Dùng để trả lời:

```text
User giả lập đang thấy hệ thống như thế nào?
```

## Dashboard B — API Health Overview

Các panel:

```text
API Target UP
API Request Rate
API P95 Latency
API Process CPU %
API Process Memory
API Process Threads
```

Dùng để trả lời:

```text
API đang chịu tải như thế nào?
```

Terminal phụ:

```bash
watch -n 2 'docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"'
```

Dùng để trả lời:

```text
Container nào đang ăn CPU/RAM thật trên Docker host?
```

---

# 12. Ghi nhớ ngắn gọn

```text
P95 là metric chính để đánh giá trải nghiệm user.
Error Rate cho biết hệ thống còn đúng hay đã fail.
RPS cho biết throughput thực tế.
VU chỉ là áp lực đầu vào, không phải năng lực xử lý.
API CPU cao + latency cao = CPU/code bottleneck.
API CPU thấp + latency cao = thường là DB/I/O bottleneck.
Memory tăng không giảm sau test = nghi memory leak.
Target UP = 0 thì các metric Prometheus phía API không đáng tin.
```

