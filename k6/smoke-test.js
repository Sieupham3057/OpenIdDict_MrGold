/**
 * Smoke Test — chạy trước load-test để xác nhận script và flow hoạt động đúng.
 *
 * Chỉ 2 VU trong 1 phút — mục tiêu: KHÔNG có lỗi, không kiểm tra performance.
 *
 * Chạy:
 *   k6 run k6/smoke-test.js
 *   k6 run --env BASE_URL=http://localhost:5000 k6/smoke-test.js
 *
 * Docker:
 *   docker run --rm -i \
 *     -v ${PWD}/k6:/scripts \
 *     --add-host=host.docker.internal:host-gateway \
 *     grafana/k6 run --env BASE_URL=http://host.docker.internal:5000 /scripts/smoke-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { SharedArray } from 'k6/data';
import papaparse from 'https://jslib.k6.io/papaparse/5.1.1/index.js';

export const options = {
  vus:      2,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.01'],   // Không chấp nhận lỗi khi smoke test
    http_req_duration: ['p(95)<5000'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = __ENV.CLIENT_ID || 'angular-spa';

const users = new SharedArray('users', function () {
  // Strip UTF-8 BOM (﻿) if present — common when file is saved from Excel/Windows
  const content = open('./users.csv').replace(/^﻿/, '');
  const rows = papaparse.parse(content, { header: true, skipEmptyLines: true }).data
    .filter(u => u.username);
  if (rows.length === 0) {
    throw new Error('users.csv is empty or has no valid rows — check file encoding (should be UTF-8 without BOM)');
  }
  return rows;
});

export function setup() {
  const res = http.get(`${BASE_URL}/health`);
  if (res.status !== 200) {
    throw new Error(`API không phản hồi tại ${BASE_URL}/health — status: ${res.status}`);
  }
  console.log(`[SMOKE] API online: ${BASE_URL}`);
}

export default function () {
  const user = users[__VU % users.length];

  // ── Login ─────────────────────────────────────────────────────────────────────
  let token;
  group('1_login', () => {
    const res = http.post(
      `${BASE_URL}/connect/token`,
      {
        grant_type: 'password',
        client_id:  CLIENT_ID,
        username:   user.username,
        password:   user.password,
        scope:      'openid profile email roles',
      },
      { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
    );

    const ok = check(res, {
      '[smoke] login 200':           (r) => r.status === 200,
      '[smoke] has access_token':    (r) => r.json('access_token') !== null,
    });

    if (ok) token = res.json('access_token');
  });

  if (!token) {
    console.error(`Login thất bại cho ${user.username}`);
    sleep(2);
    return;
  }

  const headers = {
    Authorization:  `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // ── Danh sách sản phẩm ────────────────────────────────────────────────────────
  group('2_products', () => {
    const res = http.get(`${BASE_URL}/api/products?page=1&pageSize=20`, { headers });
    check(res, {
      '[smoke] products 200':     (r) => r.status === 200,
      '[smoke] has data field':   (r) => r.json('data') !== null,
    });
    sleep(1);
  });

  // ── Chi tiết sản phẩm ─────────────────────────────────────────────────────────
  group('3_product_detail', () => {
    const res = http.get(`${BASE_URL}/api/products/1`, { headers });
    check(res, {
      '[smoke] product detail 200 or 404': (r) => r.status === 200 || r.status === 404,
    });
    sleep(1);
  });

  // ── Tạo đơn hàng ──────────────────────────────────────────────────────────────
  group('4_create_order', () => {
    const res = http.post(
      `${BASE_URL}/api/orders`,
      JSON.stringify({ items: [{ productId: 1, quantity: 1 }], note: 'smoke test' }),
      { headers }
    );
    check(res, {
      '[smoke] order 201 or 400': (r) => r.status === 201 || r.status === 400,
      '[smoke] order not 500':    (r) => r.status !== 500,
    });
    sleep(1);
  });

  // ── Đơn hàng của mình ────────────────────────────────────────────────────────
  group('5_my_orders', () => {
    const res = http.get(`${BASE_URL}/api/orders/my`, { headers });
    check(res, {
      '[smoke] my orders 200': (r) => r.status === 200,
    });
  });

  sleep(2);
}
