$docPath = 'e:\TECHLEAD_PROJECT\OpenIdDict\docker\Grafana-TechLead-Guide.docx'
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Open($docPath)
$sel = $word.Selection

# Move to end of document
$sel.EndKey(6) | Out-Null  # 6 = wdStory

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
function Info($t) {
    $sel.Style = 'Normal'
    $sel.Font.Color = 16711680  # blue (BGR)
    $sel.Font.Bold = $true
    $sel.TypeText('[INFO] ' + $t)
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

# ═══════════════════════════════════════════════════════════════════════
PB
H1 'CHUONG 8: PHAN TICH THUC TE - KET QUA LOAD TEST 1000 VU'
P 'Phan nay phan tich ket qua thuc te cua bai chay load-test.js tren he thong AuthDemo voi 1000 Virtual Users (ramp 5 giai doan trong 13 phut). Day la case study thuc te de hieu cach doc va xu ly ket qua k6.'

# Raw output section
H2 '8.1 Cau Hinh Bai Test'
$cfgH = @('Tham so', 'Gia tri')
$cfgR = @(
    @('Script', 'k6/load-test.js'),
    @('Max VUs', '1000'),
    @('Duration', '13 phut (5 stages)'),
    @('Output', 'InfluxDB http://localhost:8086/k6'),
    @('BASE_URL', 'http://localhost:5000'),
    @('Graceful ramp down', '30s'),
    @('Total requests', '45,502'),
    @('Total iterations', '8,940 complete + 296 interrupted')
)
MakeTable $cfgH $cfgR 'Bang 7: Cau hinh bai load test thuc te'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.2 Ket Qua Threshold - SLO Da Vi Pham'
P 'Threshold la cac SLO (Service Level Objective) ban tu dinh nghia trong k6 script. Day la thang do quan trong nhat.'

$thrH = @('Metric', 'SLO dat ra', 'Ket qua thuc te P(95)', 'Trang thai', 'Vi pham bao nhieu lan')
$thrR = @(
    @('login_duration', 'p(95) < 2000ms', '38,998ms (39 giay)', 'FAIL', '19.5x'),
    @('product_list_duration', 'p(95) < 1500ms', '14,946ms (15 giay)', 'FAIL', '10x'),
    @('create_order_duration', 'p(95) < 3000ms', '7,832ms (7.8 giay)', 'FAIL', '2.6x'),
    @('http_req_duration', 'p(95) < 3000ms', '23,130ms (23 giay)', 'FAIL', '7.7x'),
    @('http_req_failed', 'rate < 5%', '0.00%', 'PASS', 'N/A')
)
MakeTable $thrH $thrR 'Bang 8: Ket qua threshold - 4/5 SLO bi vi pham'

Warn 'API DUNG VE CHUC NANG (0% error) NHUNG SAI VE HIEU NANG (4/5 threshold fail)'
P 'Dieu nay co nghia: moi request deu tra ve dung status code (200/201/404), khong co 5xx, nhung thoi gian cho qua lon. Trong production, user se abandon request truoc khi nhan duoc response.'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.3 Phan Tich Chi Tiet Tung Endpoint'

H3 '8.3.1 Login - Bottleneck Nghiem Trong Nhat'
P 'Day la endpoint cham nhat trong toan bo flow, va cung la diem nghen chinh cua toan he thong.'
$lgH = @('Stat', 'Gia tri', 'Y nghia')
$lgR = @(
    @('avg', '14,948ms (15 giay)', 'Trung binh 15 giay cho moi login - khong phai outlier'),
    @('min', '98ms', 'Login nhanh nhat (it VU, chua co contention)'),
    @('med', '14,003ms (14 giay)', 'Median = 14s: hon 50% user phai cho 14 giay'),
    @('max', '57,533ms (57.5 giay)', 'Mot so user cho gan 1 phut'),
    @('p(90)', '32,373ms (32 giay)', '10% user cham nhat phai cho 32 giay'),
    @('p(95)', '38,998ms (39 giay)', 'SLO la 2000ms, thuc te gap 19.5 lan')
)
MakeTable $lgH $lgR 'Bang 9: login_duration chi tiet'

P 'Nguyen nhan login cham voi OpenIdDict:'
Bullet 'Moi login can: validate credential (DB read) + create access token + create refresh token + persist tokens (2 DB writes)'
Bullet '1000 VU cung login = 2000 concurrent DB writes vao bang OpenIddictTokens'
Bullet 'DB write contention -> SQL Server lock wait -> thread block -> pool exhaustion'
Bullet 'Crypto operation (JWT signing) tuy khong nang nhung cong huong voi DB bottleneck'
Bullet 'Bang OpenIddictTokens co the da phong to voi hang trieu rows expired chua duoc cleanup'

H3 '8.3.2 Product List - Queue Waiting Pattern'
$plH = @('Stat', 'Gia tri', 'Y nghia')
$plR = @(
    @('avg', '3,836ms', 'Trung binh ~4 giay cho list products'),
    @('min', '5ms', 'Co the la cache hit hoac test khi con it VU'),
    @('med', '1,680ms', 'Median 1.7s - on khi tai vua'),
    @('p(95)', '14,946ms (15 giay)', 'SLO la 1500ms, thuc te gap 10 lan')
)
MakeTable $plH $plR 'Bang 10: product_list_duration chi tiet'

P 'Khoang cach lon giua min (5ms) va p(95) (15s) cho thay request duoc xu ly nhanh khi server ranh, nhung phai doi rat lau khi server bi nghen. Day la queue waiting pattern - van de la thread/connection pool, khong phai ban than query.'

H3 '8.3.3 Create Order - Bimodal Distribution'
$coH = @('Stat', 'Gia tri', 'Y nghia')
$coR = @(
    @('avg', '1,590ms', 'Trung binh 1.6 giay'),
    @('min', '5ms', 'Nhanh nhat'),
    @('med', '190ms', 'MEDIAN 190ms - phan lon order tao nhanh'),
    @('p(90)', '5,381ms', 'Nhay dot ngot tu median'),
    @('p(95)', '7,832ms', 'SLO la 3000ms, gap 2.6 lan')
)
MakeTable $coH $coR 'Bang 11: create_order_duration - chu y khoang cach lon giua med va p(95)'

P 'Bimodal distribution: median = 190ms nhung p(95) = 7832ms la 2 nhom rieng biet:'
Bullet 'Nhom 1 (phần lớn): Order tao nhanh khi co thread va DB connection san sang'
Bullet 'Nhom 2 (5-10%): Order phai cho lau khi DB dang bi lock hoac connection pool day'
Bullet 'Nguyen nhan: concurrent writes vao orders table + co the co row-level lock khi check inventory'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.4 HTTP Metrics Tong Hop'
$httpH = @('Stat', 'Gia tri', 'Nhan xet')
$httpR = @(
    @('avg', '4.93s', 'Trung binh gan 5 giay/request - rat cao'),
    @('min', '2.09ms', 'Request nhanh nhat (health check hoac cache)'),
    @('med', '857ms', 'Median duoi 1s - kha tot khi con it VU'),
    @('p(90)', '16.3s', '10% request cham nhat mat 16+ giay'),
    @('p(95)', '23.13s', 'SLO 3000ms, thuc te gap 7.7 lan'),
    @('max', '57.53s', 'Request cham nhat mat 57 giay'),
    @('total', '45,502 requests', '57.8 req/s throughput'),
    @('error rate', '0.00%', 'Khong co loi - API tiep tuc xu ly du cham')
)
MakeTable $httpH $httpR 'Bang 12: HTTP metrics tong hop'

Info 'THROUGHPUT THUC TE: 57.8 req/s voi 1000 VU. Neu tinh theo breaking point (200-300 VU), he thong chi co the xu ly ~200 req/s mot cach on dinh.'

H2 '8.5 Execution Metrics - Hanh Vi User Journey'
$execH = @('Stat', 'Gia tri', 'Y nghia')
$execR = @(
    @('iteration_duration avg', '31.51s', 'Mot user journey (login->browse->order) mat 31 giay'),
    @('iteration_duration p(95)', '1 phut 16 giay', '5% user journey mat hon 1 phut 16 giay'),
    @('iterations complete', '8,940', 'So user journey hoan thanh trong 13 phut'),
    @('iterations interrupted', '296', 'User dang giua flow khi test ket thuc'),
    @('vus_max', '1000', 'Dinh cao VU dat duoc'),
    @('data_received', '155 MB (197 kB/s)', 'Tong data nhan tu server'),
    @('data_sent', '60 MB (77 kB/s)', 'Tong data gui len server')
)
MakeTable $execH $execR 'Bang 13: Execution metrics'

P '296 interrupted iterations: day khong phai loi - la 296 VU dang o giua flow (vi du dang cho login response) khi test timeout. Neu muon giam con so nay, tang gracefulStop len 60s.'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.6 Chan Doan Root Cause'
Warn 'CHAN DOAN: SQL Server la single point of failure - DB write contention o tang OpenIdDict tokens'

P 'Chuoi su kien da xay ra trong bai test:'
Bullet 'Stage 1 (0-300 VU): He thong con xu ly duoc, login mat ~200-500ms'
Bullet 'Stage 2 (300-600 VU): DB connection pool bat dau sat nguong, login tang len 5-10s'
Bullet 'Stage 3 (600-1000 VU): Pool exhausted, request xep hang, login = 14-39s'
Bullet 'Stage 4 (1000 VU maintain): He thong nghen hoan toan, iteration = 31s trung binh'
Bullet 'Stage 5 (ramp down): P95 giam dan khi VU giam'

P 'Flow ky thuat chi tiet:'
Code '1000 VU login dong thoi'
Code '    |'
Code '    v'
Code 'OpenIdDict: validate + create token + persist token (2 DB writes/login)'
Code '    |'
Code '    v'
Code 'SQL Server: 2000 concurrent writes -> bang OpenIddictTokens lock contention'
Code '    |'
Code '    v'
Code 'Connection pool (max 100 mac dinh) -> 900 threads block-wait'
Code '    |'
Code '    v'
Code '.NET ThreadPool cap thread de block-wait -> pool pressure'
Code '    |'
Code '    v'
Code 'Request moi xep hang -> P95 login = 39s, P95 total = 23s'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.7 Ke Hoach Xu Ly Theo Thu Tu Uu Tien'

H3 'Uu Tien 1: Tang Connection Pool Size (quick win, 5 phut)'
P 'Hien tai connection pool mac dinh = 100 connections. 1000 VU can it nhat 200-300 connections.'
Code '// appsettings.json hoac environment variable:'
Code '"DefaultConnection": "Server=...;Database=...;'
Code '  Max Pool Size=300;Min Pool Size=20;'
Code '  Connection Timeout=30;Command Timeout=60"'
P 'Ket qua ky vong: login p(95) giam tu 39s xuong 5-10s. Khong can code change, chi config.'

H3 'Uu Tien 2: OpenIdDict Token Cleanup (1-2 gio)'
P 'Bang OpenIddictTokens co the da chua hang trieu rows expired, lam query/insert cham.'
Code '-- Kiem tra so luong token expired:'
Code 'SELECT COUNT(*) FROM OpenIddictTokens WHERE ExpirationDate < GETUTCDATE()'
Code ''
Code '-- Kiem tra size bang:'
Code 'SELECT'
Code '  t.name AS TableName,'
Code '  p.rows AS RowCounts,'
Code '  SUM(a.total_pages) * 8 AS TotalSpaceKB'
Code 'FROM sys.tables t'
Code 'INNER JOIN sys.indexes i ON t.object_id = i.object_id'
Code 'INNER JOIN sys.partitions p ON i.object_id = p.object_id'
Code 'INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id'
Code 'WHERE t.name LIKE ''OpenIddict%'''
Code 'GROUP BY t.name, p.rows'
Code 'ORDER BY TotalSpaceKB DESC'
Code ''
Code '-- Schedule cleanup job (SQL Server Agent):'
Code 'DELETE TOP(10000) FROM OpenIddictTokens'
Code 'WHERE ExpirationDate < DATEADD(day, -1, GETUTCDATE())'
P 'Hoac bat OpenIdDict background cleanup trong Program.cs:'
Code 'builder.Services.AddOpenIddict()'
Code '    .AddCore(options => {'
Code '        options.UseEntityFrameworkCore()...'
Code '        options.SetTokenLifetimes(new TokenLifetimes {'
Code '            AuthorizationCode = TimeSpan.FromMinutes(5),'
Code '            AccessToken = TimeSpan.FromHours(1),'
Code '            RefreshToken = TimeSpan.FromDays(14)'
Code '        });'
Code '    })'
Code '    .AddServer(options => {'
Code '        options.UseAspNetCore();'
Code '    })'
Code '    .AddValidation();'
Code ''
Code '// Them hosted service de tu dong cleanup:'
Code 'builder.Services.AddHostedService<OpenIddictTokenCleanupService>();'
Code '// OpenIddictTokenCleanupService la built-in trong OpenIdDict >= 3.0'

H3 'Uu Tien 3: Kiem Tra va Them Index (2-4 gio)'
P 'Sau khi fix connection pool, chay lai test va xem query nao van cham, them index neu can.'
Code '-- Xem missing index suggestions trong SQL Server:'
Code 'SELECT'
Code '  migs.user_seeks, migs.user_scans,'
Code '  mid.statement AS TableName,'
Code '  mid.equality_columns, mid.inequality_columns,'
Code '  migs.avg_user_impact'
Code 'FROM sys.dm_db_missing_index_group_stats migs'
Code 'INNER JOIN sys.dm_db_missing_index_groups mig ON migs.group_handle = mig.index_group_handle'
Code 'INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle'
Code 'ORDER BY migs.avg_user_impact DESC'

H3 'Uu Tien 4: Async Code Review (4-8 gio)'
P 'Kiem tra toan bo code EF Core co dung async khong:'
Code '// BAD - blocking thread:'
Code 'var orders = _context.Orders.Where(o => o.UserId == userId).ToList();'
Code 'var user = _context.Users.FirstOrDefault(u => u.Id == id);'
Code ''
Code '// GOOD - async, khong block thread:'
Code 'var orders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();'
Code 'var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);'
Code ''
Code '// Tim nhanh trong codebase:'
Code '// grep -rn "\.ToList()\|\.FirstOrDefault()\|\.Single()\|\.Count()" --include="*.cs"'

H3 'Uu Tien 5: Load Test Lai De Xac Nhan Fix'
P 'Sau moi fix, chay lai load test de verify:'
Bullet 'login p(95) phai xuong duoi 2000ms voi 300-400 VU'
Bullet 'Thread count trong Grafana phai on dinh (khong tang theo VU)'
Bullet 'Breaking point phai di len it nhat 2x so voi truoc'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.8 So Sanh Truoc Va Sau Fix (Ky Vong)'
$cmpH = @('Metric', 'Hien Tai (1000 VU)', 'Ky Vong Sau Fix (400 VU on dinh)')
$cmpR = @(
    @('login p(95)', '39,000ms', 'duoi 2,000ms'),
    @('product_list p(95)', '14,946ms', 'duoi 1,500ms'),
    @('create_order p(95)', '7,832ms', 'duoi 3,000ms'),
    @('http_req_duration p(95)', '23,130ms', 'duoi 3,000ms'),
    @('Throughput on dinh', '~57 req/s (bi nghen)', '~200-400 req/s'),
    @('Breaking point', '~200-300 VU', '~500-700 VU'),
    @('Thread count max', '100+ tang lien tuc', 'on dinh duoi 50')
)
MakeTable $cmpH $cmpR 'Bang 14: So sanh ket qua truoc va sau fix (du kien)'

# ═══════════════════════════════════════════════════════════════════════
H2 '8.9 Bai Hoc Tong Ket Tu Case Study Nay'
P 'Day la nhung bai hoc quan trong Technical Leader can nho:'
Bullet 'Checks pass 100% + Threshold fail = API dung nhung chua du toc do - can phan biet 2 loai nay'
Bullet 'P(95) quan trong hon Average vi no phan anh user thuc te cham nhat van duoc phuc vu'
Bullet 'Bimodal distribution (med thap, p95 cao) = queue waiting pattern, khong phai query cham'
Bullet 'Login la critical path: neu login cham, toan bo user journey cham'
Bullet 'OpenIdDict can duoc cau hinh cleanup de tranh bang tokens phong to'
Bullet 'Connection pool size phai duoc tang truoc khi go production voi nhieu concurrent user'
Bullet 'Breaking point phai duoc xac dinh truoc khi deploy: chay stress test ngay tu dau sprint'
Bullet 'Luon chay load test voi BASE_URL tro vao staging, khong chay tren production'

Good 'NEXT STEP: Fix connection pool (5 phut), chay lai smoke test va load test 300 VU de xac nhan cai thien.'

# ─────────────────────────────────────────────────────────────────────
$doc.Save()
$doc.Close()
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
Write-Host "DONE - appended Chapter 8 to: $docPath"
