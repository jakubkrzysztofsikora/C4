import { test, expect } from '@playwright/test';
import { loginAsDemo, API_BASE_URL } from './helpers';

test.describe('Feedback System', () => {
  let token: string;
  let projectId: string;

  test.beforeAll(async ({ request }) => {
    const loginResponse = await request.post(`${API_BASE_URL}/api/auth/login`, {
      data: { email: 'demo@c4.local', password: 'Password123!' },
    });
    const loginData = await loginResponse.json();
    token = loginData.token;

    const orgResponse = await request.post(`${API_BASE_URL}/api/organizations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `Feedback Org ${Date.now()}` },
    });
    const org = await orgResponse.json();

    const projectResponse = await request.post(
      `${API_BASE_URL}/api/organizations/${org.organizationId}/projects`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: { name: `Feedback Project ${Date.now()}` },
      }
    );
    const project = await projectResponse.json();
    projectId = project.projectId;
  });

  test('feedback list endpoint returns OK', async ({ request }) => {
    const response = await request.get(
      `${API_BASE_URL}/api/projects/${projectId}/feedback`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(response.ok()).toBe(true);
  });

  test('feedback summary endpoint returns initial state', async ({ request }) => {
    const response = await request.get(
      `${API_BASE_URL}/api/projects/${projectId}/feedback/summary`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(response.ok()).toBe(true);
    const summary = await response.json();
    expect(summary.totalCount).toBe(0);
  });

  test('submit feedback via API succeeds', async ({ request }) => {
    const response = await request.post(
      `${API_BASE_URL}/api/projects/${projectId}/feedback`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          targetType: 0,
          targetId: '00000000-0000-0000-0000-000000000001',
          category: 0,
          rating: 4,
          comment: 'E2E test feedback submission',
        },
      }
    );

    const status = response.status();
    const body = await response.text();
    console.log(`Submit feedback: status=${status}, body=${body}`);
    expect(status).toBe(201);
  });

  test('feedback learnings endpoint is accessible', async ({ request }) => {
    const response = await request.get(
      `${API_BASE_URL}/api/projects/${projectId}/feedback/learnings`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(response.ok()).toBe(true);
  });

  test('diagram page renders with feedback-related legend', async ({ page }) => {
    await loginAsDemo(page);
    await page.goto('/diagram');

    await expect(page.locator('.diagram-sidebar')).toBeVisible();
    await expect(page.locator('.legend')).toBeVisible();
  });
});
