$docPath = 'e:\TECHLEAD_PROJECT\OpenIdDict\docker\Grafana-TechLead-Guide.docx'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Add()
$sel = $word.Selection

function H1($t) { $sel.Style = 'Heading 1'; $sel.TypeText($t); $sel.TypeParagraph() }
function H2($t) { $sel.Style = 'Heading 2'; $sel.TypeText($t); $sel.TypeParagraph() }
function H3($t) { $sel.Style = 'Heading 3'; $sel.TypeText($t); $sel.TypeParagraph() }
function P($t)  { $sel.Style = 'Normal'; $sel.TypeText($t); $sel.TypeParagraph() }
function NL     { $sel.Style = 'Normal'; $sel.TypeParagraph() }
function Bullet($t) { $sel.Style = 'List Bullet'; $sel.TypeText($t); $sel.TypeParagraph() }
function Warn($t) {
    $sel.Style = 'Normal'
    $sel.Font.Color = 255
    $sel.Font.Bold = $true
    $sel.TypeText('[CANH BAO] ' + $t)
    $sel.Font.Color = -16777216
    $sel.Font.Bold = $false
    $sel.TypeParagraph()
}
function Good($t) {
    $sel.Style = 'Normal'
    $sel.Font.Color = 32768
    $sel.Font.Bold = $true
    $sel.TypeText('[OK] ' + $t)
    $sel.Font.Color = -16777216
    $sel.Font.Bold = $false
    $sel.TypeParagraph()
}
function Code($t) {
    $sel.Style = 'Normal'
    $sel.Font.Name = 'Courier New'
    $sel.Font.Size = 9
    $sel.Font.Color = 8388608
    $sel.TypeText($t)
    $sel.Font.Name = 'Calibri'
    $sel.Font.Size = 11
    $sel.Font.Color = -16777216
    $sel.TypeParagraph()
}
function PB { $sel.InsertBreak(7) }

function MakeTable($headers, $rows, $caption) {
    if ($caption) {
        $sel.Style = 'Normal'
        $sel.Font.Bold = $true
        $sel.Font.Italic = $true
        $sel.TypeText($caption)
        $sel.Font.Bold = $false
        $sel.Font.Italic = $false
        $sel.TypeParagraph()
    }
    $nR = $rows.Count + 1
    $nC = $headers.Count
    $range = $sel.Range
    $tbl = $doc.Tables.Add($range, $nR, $nC)
    $tbl.Style = 'Table Grid'
    $tbl.Borders.Enable = $true
    for ($c = 0; $c -lt $nC; $c++) {
        $cell = $tbl.Cell(1, $c+1)
        $cell.Range.Text = $headers[$c]
        $cell.Range.Font.Bold = $true
        $cell.Shading.BackgroundPatternColor = 12632256
    }
    for ($r = 0; $r -lt $rows.Count; $r++) {
        for ($c = 0; $c -lt $nC; $c++) {
            $tbl.Cell($r+2, $c+1).Range.Text = $rows[$r][$c]
        }
    }
    $sel.MoveDown(5, $nR + 2)
    $sel.TypeParagraph()
}

# TITLE
$sel.Style = 'Title'
$sel.TypeText('Grafana Monitoring')
$sel.TypeParagraph()
$sel.Style = 'Subtitle'
$sel.TypeText('Technical Leader Guide - Load Testing & API Observability')
$sel.TypeParagraph()
P 'Phien ban: 1.0  |  Ngay: 04/06/2026  |  Stack: k6 + InfluxDB + Prometheus + Grafana + .NET 8'
NL
PB

# CHUONG 1
H1 'CHUONG 1: HIEU BAN CHAT CAC DASHBOARD'
P 'Phan nay giai thich tung panel trong 2 dashboard dang chay, y nghia thuc te va nguong can chu y voi tu cach Technical Leader.'

H2 '1.1 Dashboard k6 - Load Testing'
P 'Dashboard nay doc du lieu tu InfluxDB do k6 ghi vao real-time khi chay load test. Day la goc nhin tu phia CLIENT (k6 gia lap user).'

H3 'Panel: Checks Per Second'
P 'Checks trong k6 la cac assertion ban dinh nghia trong script, vi du: response status == 200, response time nho hon 500ms. Panel dem so check pass+fail moi giay.'
Bullet 'Gia tri cao = nhieu request duoc xu ly va check dang pass'
Bullet 'Gia tri dot ngot giam = API bat dau fail checks hoac timeout'
Bullet 'Trong screenshot: tang tu 0 len ~400-500 checks/s theo so VU'
Warn 'Neu Checks Per Second giam trong khi Virtual Users van tang = he thong bat dau nghen.'

H3 'Panel: P95 Response Time'
P 'P95 (Percentile 95) la thoi gian response ma 95% request hoan thanh duoi gia tri do. 5% request con lai cham hon. Day la metric quan trong nhat cho UX.'
Bullet 'P95 duoi 500ms: Tot cho web API'
Bullet 'P95 tu 500ms den 2s: Chap nhan duoc, can theo doi'
Bullet 'P95 tu 2s den 5s: Xau, users nhan thay cham ro ret'
Bullet 'P95 tren 5s: Nghiem trong, can action ngay'
Warn 'SCREENSHOT: P95 tang len 30s - he thong dang collapse duoi tai cao (600 VU). Day la nguong CUC KY NGHIEM TRONG trong production.'

H3 'Panel: Error Rate %'
P 'Ti le phan tram request bi loi (HTTP 4xx/5xx hoac timeout). Tinh tu metric http_req_failed trong InfluxDB.'
Bullet 'Error Rate = 0%: Tot'
Bullet 'Error Rate duoi 0.1%: Chap nhan duoc cho hau het he thong'
Bullet 'Error Rate tren 1%: Can dieu tra'
Bullet 'Error Rate tren 5%: Alert ngay lap tuc'
Good 'Trong screenshot: Error Rate = 0% - API khong tra loi. Nhung response time rat cao cho thay timeout phia client se xay ra neu test tiep tuc.'

H3 'Panel: Requests Per Second (RPS / TPS)'
P 'Throughput thuc te - so request duoc gui va nhan moi giay. Day la thuoc do nang luc cua he thong.'
Bullet 'RPS tang tuyen tinh voi VU = he thong dang scale tot'
Bullet 'RPS plateau trong khi VU van tang = he thong da dat gioi han (bottleneck)'
Bullet 'RPS giam trong khi VU tang = he thong dang reject requests hoac timeout'
Bullet 'Trong screenshot: RPS ~200-300, co spike - khong on dinh'

H3 'Panel: Virtual Users (VU)'
P 'So luong user ao dang dong thoi gui request. k6 ramp VU theo cau hinh script. Panel cho biet muc do tai ban dang tao ra.'
Bullet 'Trong screenshot: VU tang tu 0 len ~600 trong ~10 phut - day la stress test'
Bullet 'VU 600 voi response time 30s cho thay system breaking point o ~200-300 VU'

H3 'Panel: Average Response Time'
P 'Mean response time cua tat ca request. It dai dien hon P95 vi bi anh huong boi outlier.'
Bullet 'Trong screenshot: Average ~20s - cung thoi diem P95 = 30s, he thong toan dien cham'

PB
H2 '1.2 Dashboard API Container - Prometheus Metrics'
P 'Dashboard nay doc tu Prometheus scrape /metrics cua .NET API. Day la goc nhin tu phia SERVER.'

H3 'Panel: API Target UP'
P 'Prometheus health check. UP=1 nghia la Prometheus scrape duoc /metrics tu API. DOWN=0 nghia la API khong reachable.'
Good 'Trong screenshot: = 1 (UP) - API dang song va expose metrics.'
Warn 'Luu y: UP chi nghia la /metrics accessible, KHONG phai API dang xu ly dung.'

H3 'Panel: API Request Rate'
P 'So request/giay tinh tu phia server (metric http_requests_received_total). So sanh voi RPS cua k6:'
Bullet 'Neu k6 RPS lon hon API Request Rate = requests bi drop truoc khi vao API'
Bullet 'Neu k6 RPS bang API Request Rate = requests den duoc API, van de la xu ly ben trong'
Bullet 'Trong screenshot: API Request Rate ~60-80/s, k6 ghi ~200-300/s - chenh lech do buffering/queueing'

H3 'Panel: API Process Threads'
P '.NET Thread Pool thread count. So thread tang khi co nhieu I/O blocking (database, HTTP calls).'
Bullet 'Thread count tang = nhieu request dang bi block cho I/O (thuong la database)'
Bullet 'Thread count plateau roi khong tang them = ThreadPool exhausted, request queue up'
Warn 'SCREENSHOT: Thread tang tu ~20 len ~100+ va van dang tang - day la dau hieu thread pool pressure. Nguyen nhan: database query cham hoac sync blocking code.'

H3 'Panel: API P95 Latency'
P 'P95 latency do tu phia server (Prometheus histogram). So sanh voi P95 cua k6:'
Bullet 'Neu server P95 nho hon k6 P95 = co overhead o network/client side'
Bullet 'Neu server P95 bang k6 P95 = van de nam trong API processing'
Bullet 'Trong screenshot tooltip: 198ms luc 11:47:55 (tot), tang len ~25s sau (he thong nghen)'

PB
# CHUONG 2
H1 'CHUONG 2: DOC DASHBOARD VOI CON MAT TECHNICAL LEADER'

H2 '2.1 Chan Doan Screenshot Hien Tai'
Warn 'CHAN DOAN: He thong dang bi THREAD POOL EXHAUSTION + DATABASE BOTTLENECK'
P 'Chuoi su kien tu 2 screenshot (11:45 - 11:55):'
Bullet '11:40-11:47: VU tang 0 -> 300, P95 tot (~200ms), Thread = 20-40'
Bullet '11:47-11:50: VU vuot 300, Thread tang nhanh 40 -> 80, P95 tang 5-10s'
Bullet '11:50-11:55: VU dat 500-600, Thread ~100+, P95 = 20-30s, Average = 15-20s'
Bullet 'Error Rate van 0% - API khong crash, chi cuc ky cham'
P 'Giai thich ky thuat:'
Bullet 'Database (SQL Server) la bottleneck: moi request can query DB, khi 600 VU cung query thi DB queue day'
Bullet '.NET ThreadPool cap them thread cho cho DB (I/O-bound), nhung thread co gioi han'
Bullet 'Thread pool pressure lam request moi phai cho thread tu do -> P95 tang vot'
Bullet 'Day la pattern CLASSIC cua N+1 query hoac thieu DB connection pooling'

H2 '2.2 Nguong Technical Leader Can Quan Tam'
$thH = @('Metric', 'Tot (Green)', 'Canh bao (Yellow)', 'Nguy hiem (Red)', 'Action')
$thR = @(
    @('P95 Response Time', 'duoi 500ms', '500ms - 2s', 'tren 2s', 'Scale / Optimize query'),
    @('Error Rate', 'duoi 0.1%', '0.1% - 1%', 'tren 1%', 'Alert + investigate logs'),
    @('Thread Count (.NET)', 'duoi 50', '50 - 150', 'tren 150 tang lien tuc', 'Check DB blocking'),
    @('API Target UP', '= 1', 'N/A', '= 0', 'Restart + alert on-call'),
    @('RPS trend', 'Tang theo VU', 'Plateau khi VU tang', 'Giam khi VU tang', 'Scale horizontal'),
    @('Avg Response Time', 'duoi 200ms', '200ms - 1s', 'tren 1s', 'Profile + optimize')
)
MakeTable $thH $thR 'Bang 1: Nguong quyet dinh cho Technical Leader'

H2 '2.3 Nhan Biet Pattern Van De'

H3 'Pattern 1: Gradual Degradation'
P 'Dau hieu: P95 tang dan theo thoi gian du VU khong doi. Nguyen nhan: memory leak, connection leak, cache miss tich luy.'
Bullet 'Xem process_resident_memory_bytes - neu tang lien tuc khong giam = memory leak'
Code 'PromQL: rate(process_resident_memory_bytes{job="dotnet-api"}[5m]) > 0'

H3 'Pattern 2: Cliff Drop (Sup Do Dot Ngot)'
P 'Dau hieu: RPS dot ngot giam 80-100%, Error Rate tang vot, API Target DOWN. Nguyen nhan: OOM Kill, crash, DB lost.'
Code 'docker logs api --tail 200 | grep -E "ERROR|FATAL|Unhandled|OutOfMemory"'

H3 'Pattern 3: Thread Exhaustion (nhu screenshot cua ban)'
P 'Dau hieu: Thread tang lien tuc, P95 tang theo bac thang, RPS plateau. Nguyen nhan: DB query cham blocking threads.'
Code 'SQL: SELECT wait_type, COUNT(*) FROM sys.dm_exec_requests GROUP BY wait_type ORDER BY 2 DESC'

H3 'Pattern 4: Bimodal Distribution'
P 'Dau hieu: P95 cao bat thuong trong khi Average van thap. Mot so request dac biet cham (cold start, cache miss).'
Code 'PromQL: histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m])) > 5'

PB
# CHUONG 3
H1 'CHUONG 3: NGOAI DASHBOARD - CAC THUC HANH QUAN TRONG HON'

H2 '3.1 Alerting - Tu Dong Phat Hien Van De'
P 'Grafana Alert rules tu dong notify khi metric vuot nguong. KHONG nen ngoi nhin dashboard thu cong. Day la yeu cau bat buoc trong production.'
$alH = @('Alert Name', 'PromQL Condition', 'Nguong', 'Severity')
$alR = @(
    @('API Down', 'up{job="dotnet-api"} == 0', 'For: 1m', 'Critical'),
    @('High P95 Latency', 'histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 2', 'For: 2m', 'Warning'),
    @('High Error Rate', 'rate(http_requests_received_total{status=~"5.."}[1m]) > 0.01', 'For: 1m', 'Critical'),
    @('Thread Pool Pressure', 'process_num_threads{job="dotnet-api"} > 100', 'For: 5m', 'Warning'),
    @('Memory Growth', 'rate(process_resident_memory_bytes[5m]) > 1000000', 'For: 10m', 'Warning')
)
MakeTable $alH $alR 'Bang 2: Alert rules can thiet lap ngay trong Grafana Alerting'

H2 '3.2 Log Correlation - Tim Root Cause'
P 'Khi dashboard bao loi, buoc tiep theo la xem log de biet chinh xac exception nao xay ra.'
Bullet 'Buoc 1: Dashboard bao P95 tang luc 11:52'
Bullet 'Buoc 2: docker logs api --since 2026-06-04T11:50:00 --until 2026-06-04T11:55:00'
Bullet 'Buoc 3: Tim pattern ERROR, SqlException, Timeout, Thread'
Bullet 'Buoc 4: Correlate voi Prometheus metric cung timestamp'
Code 'docker logs api --since "2026-06-04T11:50:00" | grep -E "ERROR|Exception|Timeout" | head -50'

H2 '3.3 Distributed Tracing - Tim Endpoint Cham'
P 'Tich hop OpenTelemetry de trace tung request end-to-end, biet chinh xac line code nao cham.'
Bullet 'Tool: Jaeger (open source) hoac Zipkin - chay them container'
Bullet 'SDK: OpenTelemetry cho .NET 8 - add package OpenTelemetry.Extensions.Hosting'
Bullet 'Khi co trace: moi request hien waterfall diagram, thay DB query mat bao ms'
Bullet 'Correlation: Trace ID co the link sang Grafana Explore de xem log cung luc'

H2 '3.4 Capacity Planning - Dung Data De Quyet Dinh Scale'
P 'Technical Leader dung load test data de tra loi: "He thong nay chiu duoc bao nhieu user thuc su?"'
Bullet 'Chay stress test: ramp tu 0 -> 1000 VU trong 20 phut'
Bullet 'Xac dinh breaking point: VU tai do P95 vuot 2s lan dau tien'
Bullet 'Ap dung safety factor 60-70%: breaking point = 300 VU -> production limit = 180-210 VU'
Bullet 'Estimate concurrent users thuc te: 10% peak traffic = simultaneous requests'
Bullet 'Tinh so replicas: neu 1 instance chiu 200 VU, can 3 instances cho 600 peak users'

H2 '3.5 SLO Monitoring'
P 'SLO (Service Level Objective) la cam ket ve chat luong dich vu. Vi du: 99.9% request hoan thanh duoi 500ms.'
Code 'groups:'
Code '  - name: api_slo'
Code '    rules:'
Code '      - alert: SLOBreach'
Code '        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 0.5'
Code '        for: 5m'
Code '        labels:'
Code '          severity: critical'

PB
# CHUONG 4
H1 'CHUONG 4: USE CASE THUC TE - CHAN DOAN VA XU LY'

H2 '4.1 Use Case: API Cham Dan Sau 30 Phut Chay'
P 'Tinh huong: Dashboard binh thuong luc deploy, nhung sau 30 phut P95 tang tu 200ms len 2s ma khong co them traffic.'

H3 'Buoc 1 - Nhin dashboard, dat cau hoi:'
Bullet 'Memory co tang khong? -> process_resident_memory_bytes'
Bullet 'Thread co tang khong? -> process_num_threads'
Bullet 'Error rate co thay doi khong?'
Bullet 'GC co chay nhieu khong? -> dotnet_gc_collections_total'

H3 'Buoc 2 - Kiem tra memory leak:'
Code 'PromQL: process_resident_memory_bytes{job="dotnet-api"}'
Code '# Neu line di len lien tuc khong xuong = memory leak'

H3 'Buoc 3 - Xem GC hoat dong:'
Code 'PromQL: rate(dotnet_gc_collections_total{generation="2"}[5m])'
Code '# GC Gen2 rate cao nhung memory van tang = GC khong collect duoc = leak'

H3 'Nguyen nhan va Fix:'
Bullet 'Static collection khong duoc clear: List(T) static tich luy data qua tung request'
Bullet 'HttpClient khong duoc dispose: dung IHttpClientFactory thay vi new HttpClient()'
Bullet 'Event handler subscription khong unsubscribe: memory leak classic trong .NET'
Bullet 'Tool dieu tra: dotnet-dump, dotnet-gcdump de snapshot heap'

H2 '4.2 Use Case: Error Rate Dot Ngot Tang Len 5%'
P 'Tinh huong: Alert do xuong, Error Rate tu 0% len 5%, P95 tang vot.'

H3 'Xac dinh loai loi:'
Code 'PromQL: sum by (status) (rate(http_requests_received_total{job="dotnet-api"}[1m]))'
Code '# 500 = server error, 503 = service unavailable, 429 = rate limit'

H3 'Check database:'
Code 'docker logs api --tail 200 | grep -i "sql|connection|timeout|deadlock"'

H3 'Emergency mitigation:'
Bullet 'Neu DB down: failover sang replica, enable read-only mode'
Bullet 'Neu OOM Kill: docker restart api (sau do dieu tra goc rech)'
Bullet 'Neu deployment bad: rollback sang version truoc'
Bullet 'Neu external service down: circuit breaker activate, return cached response'

H2 '4.3 Use Case: Thread Count Tang Vot (Nhu Screenshot Cua Ban)'
P 'Tinh huong: Thread tang tu 20 len 100+ theo VU, P95 tang vot.'

H3 'Root cause analysis:'
Bullet 'Voi .NET async/await dung: thread khong nen tang nhieu du nhieu concurrent request'
Bullet 'Thread tang = code dang block thread (sync wait, Task.Result, .Wait(), blocking DB call)'
Bullet 'SQL Server voi 600 concurrent queries = connection pool max ~100 connections mac dinh'
Bullet '100 threads block cho DB + 500 request trong queue = P95 = 30s (dung voi screenshot)'

H3 'Fix code:'
Code '// BAD - blocking EF Core sync:'
Code 'var result = dbContext.Users.ToList();'
Code ''
Code '// GOOD - async EF Core:'
Code 'var result = await dbContext.Users.ToListAsync();'
Code ''
Code '// Trong connection string - tang pool size:'
Code 'Server=...;Database=...;Max Pool Size=300;Min Pool Size=10'

H3 'Xac nhan fix:'
P 'Chay lai k6 voi cung VU. Thread count phai on dinh o muc thap du VU cao. P95 phai giam xuong duoi 500ms.'

H2 '4.4 Use Case: k6 Dashboard Trang Khong Co Data'
P 'Tinh huong: Chay k6 xong, vao Grafana nhung panel trang het.'
Bullet '1. InfluxDB co data khong? curl -G http://localhost:8086/query --data-urlencode "db=k6" --data-urlencode "q=SHOW MEASUREMENTS"'
Bullet '2. Grafana co dung datasource khong? Admin -> Data sources -> influxdb -> Test'
Bullet '3. Time range co dung khong? Chon "Last 1 hour" hoac "Last 6 hours"'
Bullet '4. k6 co dung --out influxdb=... khong? k6 khong tu dong ghi vao InfluxDB'
Bullet '5. Query co dung measurement name khong? Vao Explore -> influxdb, test query trc'

H2 '4.5 Use Case: Prometheus Target DOWN'
P 'Tinh huong: Dashboard API Target UP hien thi 0.'
Bullet '1. API container co chay khong? docker ps | grep api'
Bullet '2. /metrics co accessible khong? curl http://localhost:5000/metrics | head'
Bullet '3. Prometheus config co dung target khong? cat prometheus/prometheus.yml'
Bullet '4. Prometheus co resolve duoc hostname khong? docker exec prometheus wget -qO- http://api:8080/metrics'
Bullet '5. Network co thong khong? docker network inspect bridge'

PB
# CHUONG 5
H1 'CHUONG 5: CHECKLIST VA THUC HANH HANG NGAY'

H2 '5.1 Pre-Deploy Checklist'
Bullet '[ ] Chay smoke test k6 (10 VU, 1 phut) -> P95 duoi 500ms, Error = 0%'
Bullet '[ ] Chay load test k6 (100 VU, 5 phut) -> P95 duoi 2s, Error duoi 0.1%'
Bullet '[ ] Kiem tra memory khong tang sau 5 phut chiu tai'
Bullet '[ ] Kiem tra Thread count on dinh'
Bullet '[ ] Prometheus target UP'
Bullet '[ ] Alert rules da configured'

H2 '5.2 Post-Deploy Monitoring'
Bullet '[ ] Xem Grafana 15 phut sau deploy'
Bullet '[ ] Verify P95 khong tang so voi truoc deploy'
Bullet '[ ] Verify Error Rate khong tang'
Bullet '[ ] Verify Memory trend khong bat thuong'

H2 '5.3 Danh Muc k6 Scripts Can Co'
$scrH = @('Script', 'Muc dich', 'VU', 'Duration')
$scrR = @(
    @('smoke-test.js', 'Verify API hoat dong sau deploy', '1-5 VU', '1-2 phut'),
    @('load-test.js', 'Test normal production load', '50-100 VU', '10-15 phut'),
    @('stress-test.js', 'Tim breaking point, capacity planning', 'Ramp 0->1000', '20-30 phut'),
    @('spike-test.js', 'Test hanh vi khi traffic dot bien', '0->500->0', '5 phut'),
    @('soak-test.js', 'Test memory leak qua thoi gian dai', '50 VU constant', '1-2 gio')
)
MakeTable $scrH $scrR 'Bang 3: Danh muc k6 scripts can co trong project'

H2 '5.4 Commands Thuong Dung Khi Incident'
Code '# Xem container resource ngay lap tuc:'
Code 'docker stats --no-stream'
Code ''
Code '# Xem log API real-time:'
Code 'docker logs api --tail 100 -f'
Code ''
Code '# Xem log theo time range:'
Code 'docker logs api --since "2026-06-04T11:50:00" --until "2026-06-04T11:55:00"'
Code ''
Code '# Restart API nhanh:'
Code 'docker restart api'
Code ''
Code '# Force Prometheus reload config:'
Code 'curl -X POST http://localhost:9090/-/reload'
Code ''
Code '# Check InfluxDB co data khong:'
Code 'curl -G http://localhost:8086/query --data-urlencode "db=k6" --data-urlencode "q=SHOW MEASUREMENTS"'

PB
# CHUONG 6
H1 'CHUONG 6: PANELS NANG CAO NEN THEM VAO DASHBOARD'

H2 '6.1 .NET GC Collections Rate'
P 'Panel nay giup phat hien memory pressure va GC storm - khi GC chay qua nhieu lam cham ung dung.'
Code 'PromQL: rate(dotnet_gc_collections_total{job="dotnet-api"}[5m])'
Code 'Legend: {{generation}}'
P 'gen0 thuong xuyen = OK (thu gom object ngan han). gen2 nhieu = memory pressure nghiem trong.'

H2 '6.2 HTTP Status Code Distribution'
P 'Phan biet loai loi: 4xx la loi phia client, 5xx la loi phia server.'
Code 'PromQL: sum by (status) (rate(http_requests_received_total{job="dotnet-api"}[1m]))'

H2 '6.3 .NET ThreadPool Queue Length'
P 'Khi queue > 0 keo dai nghia la ThreadPool da bao hoa, request dang xep hang cho thread.'
Code 'PromQL: dotnet_threadpool_queue_length{job="dotnet-api"}'

H2 '6.4 k6 Iteration Duration (chinh xac hon Average)'
Code 'InfluxQL: SELECT percentile("value", 50) FROM "iteration_duration"'
Code 'WHERE time > now() - 15m GROUP BY time(10s)'

H2 '6.5 GC Pause Duration (Advanced)'
P 'Thoi gian GC dung ung dung de collect garbage. Gen2 GC pause > 100ms = user cam nhan gian doan.'
Code 'PromQL: rate(dotnet_gc_pause_ratio{job="dotnet-api"}[1m])'

PB
# CHUONG 7
H1 'CHUONG 7: QUICK REFERENCE'

H2 '7.1 Flow Doc Dashboard Khi Incident (30 giay)'
Bullet 'STEP 1: API Target UP = 1? -> Neu = 0: API down, restart ngay'
Bullet 'STEP 2: Error Rate? -> Neu tren 1%: co 5xx errors, xem log ngay'
Bullet 'STEP 3: P95 Response Time? -> Neu tren 2s: performance degradation'
Bullet 'STEP 4: Thread Count trend? -> Neu tang lien tuc: blocking I/O'
Bullet 'STEP 5: Memory trend? -> Neu tang khong giam: memory leak'
Bullet 'STEP 6: RPS vs VU ratio? -> Neu RPS plateau khi VU tang: bottleneck'

H2 '7.2 Khi Nao Call On-Call Engineer'
Warn 'CALL NGAY: API Target DOWN tren 1 phut'
Warn 'CALL NGAY: Error Rate tren 5% keo dai tren 2 phut'
Warn 'CALL NGAY: P95 tren 10s keo dai tren 3 phut trong production'
Warn 'CALL NGAY: Memory tang tren 80% container memory limit'
P 'INVESTIGATE (khong can call ngay): P95 tu 2-5s, Thread tang dan, GC rate cao.'

H2 '7.3 PromQL Cheat Sheet'
$pqH = @('Muc dich', 'PromQL')
$pqR = @(
    @('API co UP khong', 'up{job="dotnet-api"}'),
    @('Request rate 1 phut', 'sum(rate(http_requests_received_total{job="dotnet-api"}[1m]))'),
    @('P95 latency 5 phut', 'histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job="dotnet-api"}[5m])) by (le))'),
    @('Error rate 5xx', 'rate(http_requests_received_total{job="dotnet-api",status=~"5.."}[1m])'),
    @('Memory MB', 'process_resident_memory_bytes{job="dotnet-api"} / 1024 / 1024'),
    @('CPU %', 'rate(process_cpu_seconds_total{job="dotnet-api"}[1m]) * 100'),
    @('Thread count', 'process_num_threads{job="dotnet-api"}'),
    @('GC Gen2 rate', 'rate(dotnet_gc_collections_total{generation="2"}[5m])')
)
MakeTable $pqH $pqR 'Bang 4: PromQL Cheat Sheet'

H2 '7.4 InfluxQL Cheat Sheet (k6)'
$iqH = @('Muc dich', 'InfluxQL')
$iqR = @(
    @('P95 response time', 'SELECT percentile("value",95) FROM "http_req_duration" WHERE time>now()-15m GROUP BY time(10s)'),
    @('Average response', 'SELECT mean("value") FROM "http_req_duration" WHERE time>now()-15m GROUP BY time(10s)'),
    @('RPS', 'SELECT sum("value") FROM "http_reqs" WHERE time>now()-15m GROUP BY time(1s)'),
    @('Virtual users', 'SELECT mean("value") FROM "vus" WHERE time>now()-15m GROUP BY time(10s)'),
    @('Error rate %', 'SELECT mean("value")*100 FROM "http_req_failed" WHERE time>now()-15m GROUP BY time(10s)'),
    @('Checks/s', 'SELECT sum("value") FROM "checks" WHERE time>now()-15m GROUP BY time(1s)')
)
MakeTable $iqH $iqR 'Bang 5: InfluxQL Cheat Sheet cho k6'

H2 '7.5 So Sanh 2 Nguon Data'
$cmpH = @('Metric', 'k6/InfluxDB', 'Prometheus/.NET', 'Dung khi nao')
$cmpR = @(
    @('P95 Latency', 'Goc nhin client (network included)', 'Goc nhin server (processing only)', 'So sanh hai gia tri de biet overhead mang'),
    @('Request Rate', 'Actual sent by k6', 'Actual received by API', 'Phat hien drop truoc khi vao API'),
    @('Error Rate', 'Client-side failed check', 'Server-side HTTP status', 'Cross-validate nguon loi'),
    @('Memory/CPU', 'Khong co trong k6', '.NET process metrics', 'Correlation voi lat khi load cao'),
    @('Thread', 'Khong co trong k6', 'process_num_threads', 'Phat hien blocking I/O pattern')
)
MakeTable $cmpH $cmpR 'Bang 6: So sanh 2 nguon data - khi nao dung cai nao'

# FOOTER
NL
NL
$sel.Style = 'Normal'
$sel.Font.Italic = $true
$sel.Font.Size = 9
$sel.TypeText('Tai lieu nay duoc tao tu thuc te lab: k6 + InfluxDB + Prometheus + Grafana 11 + .NET 8 + Docker')
$sel.TypeParagraph()
$sel.TypeText('Stack: AuthDemo.Api | SQL Server 2022 | Grafana 11.6.1 | Prometheus v2.55.1 | InfluxDB 1.8 | k6')
$sel.Font.Italic = $false
$sel.Font.Size = 11

$doc.SaveAs2($docPath)
$doc.Close()
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
Write-Host "DONE: $docPath"
