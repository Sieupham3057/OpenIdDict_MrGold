import http from 'k6/http';
import { check, sleep } from 'k6';

// Stress test: tăng liên tục đến khi hệ thống fail
export const options = {
  stages: [
    { duration: '2m',  target: 50   },  // khởi động nhẹ
    { duration: '3m',  target: 100  },  // tải nhẹ — baseline
    { duration: '3m',  target: 200  },  // tải vừa
    { duration: '3m',  target: 400  },  // bắt đầu stress
    { duration: '3m',  target: 600  },  // stress nặng
    { duration: '3m',  target: 800  },  // ngưỡng nguy hiểm
    { duration: '3m',  target: 1000 },  // thường die ở đây với 2 core
    { duration: '2m',  target: 0    },  // cool down — xem có recover không
  ],
  thresholds: {
    // Không fail test tự động, để quan sát đến tận cùng
    http_req_duration: ['p(95)<10000'],
    http_req_failed: ['rate<0.5'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://192.168.1.35:5000';

// Danh sách user seed sẵn (smoke test trước để xác nhận user tồn tại)
function getUser(vu) {
  const idx = String(vu % 100 + 1).padStart(3, '0');
  return { username: `loadtest_${idx}@test.com`, password: 'TestPass@123' };
}

export default function () {
  const user = getUser(__VU);

  // Step 1: Login
  const loginRes = http.post(`${BASE_URL}/connect/token`,
    `grant_type=password&client_id=angular-spa&username=${user.username}&password=${user.password}&scope=openid profile email roles`,
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
  );

  const loginOk = check(loginRes, {
    'login 200': (r) => r.status === 200,
    'has token': (r) => r.json('access_token') !== undefined,
  });

  if (!loginOk) {
    sleep(1);
    return;
  }

  const token = loginRes.json('access_token');
  const headers = { Authorization: `Bearer ${token}` };

  // Step 2: Gọi API (thay bằng endpoint thực của bạn)
  const meRes = http.get(`${BASE_URL}/api/user/info`, { headers });
  check(meRes, { 'user info 200': (r) => r.status === 200 });

  sleep(1);
}