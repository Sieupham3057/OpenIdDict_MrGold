/**
 * k6 Load Test Script — AuthDemo API
 *
 * Yêu cầu trước khi chạy:
 *   1. Khởi động API để WorkerService seed 100 test users vào DB
 *   2. Đảm bảo file users.csv nằm cùng thư mục với script này
 *
 * Chạy local:
 *   k6 run load-test.js
 *
 * Chạy với output Grafana/InfluxDB:
 *   k6 run --out influxdb=http://localhost:8086/k6 load-test.js
 *
 * Chạy Docker:
 *   docker run --rm -i -v ${PWD}:/scripts grafana/k6 run /scripts/load-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { SharedArray } from 'k6/data';
import { Rate, Trend } from 'k6/metrics';
import papaparse from 'https://jslib.k6.io/papaparse/5.1.1/index.js';

// ─── Cấu hình load test ────────────────────────────────────────────────────────

export const options = {
  stages: [
    { duration: '1m', target: 50  },  // Khởi động nhẹ
    { duration: '3m', target: 200 },  // Tăng dần
    { duration: '5m', target: 500 },  // Giữ tải cao
    { duration: '2m', target: 1000 }, // Stress test
    { duration: '2m', target: 0   },  // Hạ tải
  ],
  thresholds: {
    http_req_duration:        ['p(95)<3000'],  // 95% request dưới 3 giây
    http_req_failed:          ['rate<0.05'],   // Tỷ lệ lỗi dưới 5%
    'login_duration':         ['p(95)<2000'],  // Login dưới 2 giây
    'product_list_duration':  ['p(95)<1500'],  // Danh sách sản phẩm dưới 1.5 giây
    'create_order_duration':  ['p(95)<3000'],  // Tạo đơn hàng dưới 3 giây
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const CLIENT_ID = __ENV.CLIENT_ID || 'angular-spa';

// ─── Custom metrics ────────────────────────────────────────────────────────────

const loginDuration       = new Trend('login_duration');
const productListDuration = new Trend('product_list_duration');
const createOrderDuration = new Trend('create_order_duration');
const loginFailRate       = new Rate('login_fail_rate');

// ─── Đọc danh sách user từ CSV (chỉ parse 1 lần, chia sẻ giữa các VU) ─────────

const users = new SharedArray('users', function () {
  return papaparse.parse(open('./users.csv'), { header: true }).data
    .filter(u => u.username); // lọc dòng trống cuối file
});

// ─── Hàm login, trả về access_token hoặc null nếu lỗi ────────────────────────

function login(username, password) {
  const startTime = Date.now();

  const res = http.post(
    `${BASE_URL}/connect/token`,
    {
      grant_type:    'password',
      client_id:     CLIENT_ID,
      username:      username,
      password:      password,
      scope:         'openid profile email roles',
    },
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
  );

  loginDuration.add(Date.now() - startTime);

  const ok = check(res, { 'login 200': (r) => r.status === 200 });
  loginFailRate.add(!ok);

  if (!ok) return null;
  return res.json('access_token');
}

// ─── Main VU function ─────────────────────────────────────────────────────────

export default function () {
  // Mỗi VU chọn user theo index (mod để không vượt quá mảng)
  const user = users[__VU % users.length];

  // ── Bước 1: Login ────────────────────────────────────────────────────────────
  let token;
  group('1_login', () => {
    token = login(user.username, user.password);
  });

  if (!token) {
    sleep(2);
    return; // Bỏ qua iteration này nếu login thất bại
  }

  const headers = {
    Authorization:  `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // ── Bước 2: Lấy danh sách sản phẩm ─────────────────────────────────────────
  group('2_browse_products', () => {
    const startTime = Date.now();
    const page = Math.floor(Math.random() * 5) + 1;

    const res = http.get(`${BASE_URL}/api/products?page=${page}&pageSize=20`, { headers });
    productListDuration.add(Date.now() - startTime);

    check(res, { 'products 200': (r) => r.status === 200 });

    sleep(Math.random() * 2 + 1); // Giả lập user đọc trang (1–3 giây)
  });

  // ── Bước 3: Xem chi tiết 1 sản phẩm ngẫu nhiên ─────────────────────────────
  group('3_product_detail', () => {
    const productId = Math.floor(Math.random() * 50) + 1; // Giả sử có 50 sản phẩm
    const res = http.get(`${BASE_URL}/api/products/${productId}`, { headers });
    check(res, { 'product detail 200 or 404': (r) => r.status === 200 || r.status === 404 });

    sleep(Math.random() * 1.5 + 0.5);
  });

  // ── Bước 4: Tạo đơn hàng ────────────────────────────────────────────────────
  group('4_create_order', () => {
    const startTime = Date.now();
    const productId = Math.floor(Math.random() * 50) + 1;

    const payload = JSON.stringify({
      items: [{ productId: productId, quantity: 1 }],
      note: `k6 test order - VU ${__VU}`,
    });

    const res = http.post(`${BASE_URL}/api/orders`, payload, { headers });
    createOrderDuration.add(Date.now() - startTime);

    check(res, {
      'create order 201': (r) => r.status === 201,
      'create order not 500': (r) => r.status !== 500,
    });

    sleep(1);
  });

  // ── Bước 5: Xem đơn hàng của mình ──────────────────────────────────────────
  group('5_my_orders', () => {
    const res = http.get(`${BASE_URL}/api/orders/my`, { headers });
    check(res, { 'my orders 200': (r) => r.status === 200 });
  });

  // Nghỉ ngẫu nhiên 2–5 giây trước khi lặp lại (giả lập think time của user)
  sleep(Math.random() * 3 + 2);
}

// ─── Hàm chạy 1 lần khi bắt đầu (setup) ─────────────────────────────────────

export function setup() {
  // Kiểm tra API có online không trước khi bắt đầu test
  const res = http.get(`${BASE_URL}/health`);
  if (res.status !== 200) {
    throw new Error(`API không phản hồi tại ${BASE_URL}/health — status: ${res.status}`);
  }
  console.log(`API OK tại ${BASE_URL}`);
}
