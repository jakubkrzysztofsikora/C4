import { Page, expect } from '@playwright/test';

export const API_BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:5000';

export async function loginAsDemo(page: Page): Promise<void> {
  await page.goto('/login');
  await page.evaluate(() => localStorage.clear());
  await page.goto('/login');
  await page.fill('#auth-email', 'demo@c4.local');
  await page.fill('#auth-password', 'Password123!');
  await Promise.all([
    page.waitForResponse(resp => resp.url().includes('/api/auth/login'), { timeout: 10000 }),
    page.click('button.auth-submit'),
  ]);
  await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });
  await expect(page).not.toHaveURL(/\/login/);
}
