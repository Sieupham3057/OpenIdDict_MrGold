/**
 * Crash Test — Tìm điểm chết của hệ thống
 *
 * Mục đích:
 *   1. Tăng VU liên tục cho đến khi hệ thống bắt đầu fail
 *   2. Ghi nhận ngưỡng VU mà p95 latency vượt 3s / error rate > 10%
 *   3. Sau đó dùng ngưỡng đó để test scale out (2 instance qua LB)
 *
 * Không có thresholds → k6 luôn chạy đến hết, không dừng sớm.
 * Mục tiêu là QUAN SÁT — không phải pass/fail.
 *
 * Chạy (1 instance trực tiếp):
 *   k6 run --out influxdb=http://192.168.1.35:8086/k6 \
 *          --env BASE_URL=http://192.168.1.35:5000 \
 *          k6/crash-test.js
 *
 * Chạy (qua Load Balancer sau khi scale):
 *   k6 run --out influxdb=http://192.168.1.35:8086/k6 \
 *          --env BASE_URL=http://192.168.1.35:80 \
 *          k6/crash-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { SharedArray } from 'k6/data';
import { Rate, Trend } from 'k6/metrics';
import papaparse from 'https://jslib.k6.io/papaparse/5.1.1/index.js';

// ─── Stages: tăng đều, không dừng lại ─────────────────────────────────────────
// Theo dõi Grafana dashboard 2587 — xem p95 latency và error rate thay đổi theo VU

export const options = {
  stages: [
    { duration: '30s', target: 50   },  // Baseline — hệ thống đang khỏe
    { duration: '1m',  target: 200  },  // Tải bình thường
    { duration: '1m',  target: 500  },  // Tải cao
    { duration: '1m',  target: 800  },  // Vượt ngưỡng thiết kế
    { duration: '1m',  target: 1200 },  // Stress
    { duration: '1m',  target: 2000 },  // Phá giới hạn
    { duration: '2m',  target: 2000 },  // Giữ tải cao — quan sát hệ thống "chết"
    { duration: '2m',  target: 0    },  // Hạ tải → xem hệ thống có self-recover không
  ],
  // KHÔNG có thresholds — mục tiêu là quan sát toàn bộ quá trình collapse + recover
};

const BASE_URL  = __ENV.BASE_URL  || 'http://localhost:5000';
const CLIENT_ID = __ENV.CLIENT_ID || 'angular-spa';

// ─── Custom metrics để theo dõi từng loại tín hiệu ────────────────────────────

const loginOkRate   = new Rate('crash_login_ok');      // Giảm về 0% khi server treo
const timeoutRate   = new Rate('crash_timeout');        // Tăng khi connection pool cạn
const error5xxRate  = new Rate('crash_5xx');            // Tăng khi crash / OOM
const loginLatency  = new Trend('crash_login_ms');      // Xem khi nào login bắt đầu chậm

// ─── Users data ────────────────────────────────────────────────────────────────

const users = new SharedArray('users', function () {
  return papaparse.parse(open('./users.csv'), { header: true }).data
    .filter(u => u.username);
});

// ─── Setup: kiểm tra API còn sống ─────────────────────────────────────────────

export function setup() {
  const res = http.get(`${BASE_URL}/health`, { timeout: '10s' });
  if (res.status !== 200) {
    throw new Error(`API không phản hồi tại ${BASE_URL}/health — status: ${res.status}`);
  }
  console.log(`[CRASH TEST] Bắt đầu tại ${BASE_URL}`);
  console.log(`[CRASH TEST] Mở Grafana http://192.168.1.35:3000 → dashboard 2587 để xem real-time`);
}

// ─── Main VU function ─────────────────────────────────────────────────────────

export default function () {
  const user = users[__VU % users.length];

  // ── Login ─────────────────────────────────────────────────────────────────────
  let token = null;

  group('1_login', () => {
    const start = Date.now();

    const res = http.post(
      `${BASE_URL}/connect/token`,
      {
        grant_type: 'password',
        client_id:  CLIENT_ID,
        username:   user.username,
        password:   user.password,
        scope:      'openid profile email roles',
      },
      {
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        timeout: '20s',   // Timeout dài để thấy rõ server treo thay vì fail ngay
      }
    );

    loginLatency.add(Date.now() - start);
    loginOkRate.add(res.status === 200);
    timeoutRate.add(res.error_code === 1050);   // 1050 = k6 timeout error code
    error5xxRate.add(res.status >= 500);

    check(res, { 'login ok': (r) => r.status === 200 });

    if (res.status === 200) token = res.json('access_token');
  });

  if (!token) {
    sleep(1);
    return;
  }

  const headers = {
    Authorization:  `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // ── Đọc sản phẩm ─────────────────────────────────────────────────────────────
  group('2_products', () => {
    const res = http.get(`${BASE_URL}/api/products?page=1&pageSize=20`, {
      headers,
      timeout: '20s',
    });
    check(res, { 'products ok': (r) => r.status === 200 });
    sleep(Math.random() * 1.5 + 0.5);
  });

  // ── Tạo đơn hàng (write — tạo DB write pressure) ─────────────────────────────
  group('3_create_order', () => {
    const res = http.post(
      `${BASE_URL}/api/orders`,
      JSON.stringify({
        items: [{ productId: Math.floor(Math.random() * 50) + 1, quantity: 1 }],
        note: `crash-test VU=${__VU}`,
      }),
      { headers, timeout: '20s' }
    );
    check(res, {
      'order ok':      (r) => r.status === 201,
      'order not 500': (r) => r.status !== 500,
    });
    sleep(1);
  });

  sleep(Math.random() * 2 + 1);
}
