import { test, expect } from '@playwright/test';

test.describe('Full Stack Connectivity', () => {
  test('frontend serves the app on port 3000', async ({ page }) => {
    const response = await page.goto('/');
    expect(response?.status()).toBe(200);
  });

  test('app redirects unauthenticated users to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login/);
  });

  test('login page renders with sign in form', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('input[type="email"], input[placeholder*="email" i], input[name="email"]').first()).toBeVisible();
    await expect(page.locator('input[type="password"]').first()).toBeVisible();
  });

  test('backend health endpoint responds', async ({ request }) => {
    const response = await request.get('http://localhost:5000/health');
    expect(response.status()).toBe(200);
  });

  test('all module health endpoints respond', async ({ request }) => {
    const modules = ['identity', 'discovery', 'graph', 'telemetry', 'visualization', 'feedback'];
    for (const mod of modules) {
      const response = await request.get(`http://localhost:5000/api/${mod}/health`);
      expect(response.status()).toBe(200);
      const body = await response.json();
      expect(body.status).toBe('ok');
    }
  });
});
