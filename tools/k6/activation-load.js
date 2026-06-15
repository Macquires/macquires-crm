import http from 'k6/http';
import { check, sleep } from 'k6';

// Usage:
//   k6 run --vus 50 --duration 2m -e BASE_URL=http://localhost:8080 -e JWT_TOKEN=... tools/k6/activation-load.js
// Target: 200 concurrent activations across VUs (adjust --vus).

export const options = {
  scenarios: {
    activation_burst: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS || 50),
      duration: __ENV.DURATION || '2m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<3000'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const token = __ENV.JWT_TOKEN || '';

export default function () {
  if (!token) {
    console.warn('Set JWT_TOKEN env var');
    return;
  }

  const headers = {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
    'Idempotency-Key': `k6-${__VU}-${__ITER}-${Date.now()}`,
  };

  // Health gate
  const health = http.get(`${baseUrl}/health`);
  check(health, { 'health ok': (r) => r.status === 200 });

  // List pending operations (read-heavy under load)
  const list = http.get(`${baseUrl}/api/Telecom/Operations?status=PendingDocuments&take=5`, { headers });
  check(list, { 'list ops': (r) => r.status === 200 || r.status === 404 });

  sleep(0.5);
}
