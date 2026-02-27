import { test, expect } from '@playwright/test';

test.describe('Authentication Flows', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.goto('/login');
  });

  test('login with seeded demo credentials succeeds', async ({ page }) => {
    const consoleMessages: string[] = [];
    page.on('console', msg => consoleMessages.push(`${msg.type()}: ${msg.text()}`));

    const networkErrors: string[] = [];
    page.on('requestfailed', req => networkErrors.push(`${req.method()} ${req.url()} - ${req.failure()?.errorText}`));

    await page.fill('#auth-email', 'demo@c4.local');
    await page.fill('#auth-password', 'Password123!');

    const [response] = await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/auth/login'), { timeout: 10000 }).catch(() => null),
      page.click('button.auth-submit'),
    ]);

    if (response) {
      const status = response.status();
      const body = await response.text().catch(() => 'could not read body');
      console.log(`Login API response: status=${status}, body=${body}`);

      if (status === 200) {
        await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
        await expect(page).not.toHaveURL(/\/login/);

        const token = await page.evaluate(() => localStorage.getItem('c4_token'));
        expect(token).toBeTruthy();
      } else {
        console.log('Login failed with non-200 status');
        console.log('Console messages:', consoleMessages);
        expect(status).toBe(200);
      }
    } else {
      console.log('No network response captured for login');
      console.log('Console messages:', consoleMessages);
      console.log('Network errors:', networkErrors);
      expect(response).not.toBeNull();
    }
  });

  test('login with invalid credentials shows error', async ({ page }) => {
    await page.fill('#auth-email', 'demo@c4.local');
    await page.fill('#auth-password', 'wrongpassword');

    const [response] = await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/auth/login'), { timeout: 10000 }).catch(() => null),
      page.click('button.auth-submit'),
    ]);

    if (response) {
      const status = response.status();
      console.log(`Invalid creds response: status=${status}`);
    }

    await expect(page).toHaveURL(/\/login/);

    const errorElement = page.locator('[role="alert"], .form-error');
    await expect(errorElement.first()).toBeVisible({ timeout: 5000 });
  });

  test('register new user succeeds', async ({ page }) => {
    await page.click('button.auth-tab:has-text("Create Account")');
    await expect(page.locator('#auth-display-name')).toBeVisible();

    const uniqueEmail = `e2e-test-${Date.now()}@c4.local`;
    await page.fill('#auth-display-name', 'Test User E2E');
    await page.fill('#auth-email', uniqueEmail);
    await page.fill('#auth-password', 'TestPassword123!');

    const [response] = await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/auth/register'), { timeout: 10000 }).catch(() => null),
      page.click('button.auth-submit'),
    ]);

    if (response) {
      const status = response.status();
      const body = await response.text().catch(() => 'could not read body');
      console.log(`Register API response: status=${status}, body=${body}`);
    }

    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
    await expect(page).not.toHaveURL(/\/login/);
  });

  test('logout redirects to login page', async ({ page }) => {
    await page.fill('#auth-email', 'demo@c4.local');
    await page.fill('#auth-password', 'Password123!');

    await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/auth/login'), { timeout: 10000 }),
      page.click('button.auth-submit'),
    ]);

    await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });

    const signOutButton = page.locator('button:has-text("Sign Out"), button:has-text("Logout"), button:has-text("Log Out"), button:has-text("Sign out")');
    await signOutButton.first().click({ timeout: 5000 });

    await expect(page).toHaveURL(/\/login/, { timeout: 5000 });

    const token = await page.evaluate(() => localStorage.getItem('c4_token'));
    expect(token).toBeFalsy();
  });

  test('protected routes redirect to login when not authenticated', async ({ page }) => {
    await page.goto('/organizations');
    await expect(page).toHaveURL(/\/login/);

    await page.goto('/subscriptions');
    await expect(page).toHaveURL(/\/login/);

    await page.goto('/diagram');
    await expect(page).toHaveURL(/\/login/);
  });

  test('registration validation rejects short password', async ({ page }) => {
    await page.click('button.auth-tab:has-text("Create Account")');
    await expect(page.locator('#auth-display-name')).toBeVisible();

    await page.fill('#auth-display-name', 'Test User');
    await page.fill('#auth-email', 'short@c4.local');
    await page.fill('#auth-password', 'short');

    await page.click('button.auth-submit');

    const errorElement = page.locator('[role="alert"], .form-error');
    await expect(errorElement.first()).toBeVisible({ timeout: 5000 });
    await expect(page).toHaveURL(/\/login/);
  });
});
