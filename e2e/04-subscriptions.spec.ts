import { test, expect } from '@playwright/test';
import { loginAsDemo } from './helpers';

test.describe('Subscription Wizard', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDemo(page);
  });

  test('navigate to Subscriptions page', async ({ page }) => {
    await page.click('a.nav-link:has-text("Subscriptions")');
    await expect(page).toHaveURL(/\/subscriptions/);
    await expect(page.locator('h1:has-text("Azure Subscription Wizard")')).toBeVisible();
  });

  test('subscription form elements are visible', async ({ page }) => {
    await page.goto('/subscriptions');

    await expect(page.locator('#subscription-id-input')).toBeVisible();
    await expect(page.locator('#display-name-input')).toBeVisible();
    await expect(page.locator('button:has-text("Connect")')).toBeVisible();
    await expect(page.locator('text=Connect Azure Subscription')).toBeVisible();
  });

  test('connect Azure subscription succeeds', async ({ page }) => {
    await page.goto('/subscriptions');

    const subId = crypto.randomUUID();
    await page.fill('#subscription-id-input', subId);
    await page.fill('#display-name-input', 'E2E Test Subscription');

    const [response] = await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/discovery/subscriptions') && resp.request().method() === 'POST', { timeout: 10000 }),
      page.click('button:has-text("Connect")'),
    ]);

    const status = response.status();
    console.log(`Subscription connect: status=${status}`);

    if (status === 201 || status === 200) {
      await expect(page.locator('text=Connected Subscription')).toBeVisible({ timeout: 5000 });
      await expect(page.locator('strong:has-text("E2E Test Subscription")')).toBeVisible();
    } else {
      const body = await response.text();
      console.log(`Subscription body: ${body}`);
      expect(status).toBe(201);
    }
  });

  test('connect button disabled when inputs empty', async ({ page }) => {
    await page.goto('/subscriptions');

    const connectBtn = page.locator('button:has-text("Connect")');
    await expect(connectBtn).toBeDisabled();

    await page.fill('#subscription-id-input', 'some-id');
    await expect(connectBtn).toBeDisabled();

    await page.fill('#display-name-input', 'Some Name');
    await expect(connectBtn).toBeEnabled();
  });
});
