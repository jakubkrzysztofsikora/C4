import { test, expect } from '@playwright/test';
import { loginAsDemo } from './helpers';

test.describe('Cross-Cutting Concerns', () => {
  test('dark/light mode toggle works', async ({ page }) => {
    await loginAsDemo(page);
    await page.goto('/');

    const themeToggle = page.locator('button:has-text("Dark"), button:has-text("Light")');
    await expect(themeToggle).toBeVisible();

    const initialText = await themeToggle.textContent();

    await themeToggle.click();
    await expect(page.locator('html')).toHaveAttribute('data-theme', newText?.toLowerCase() ?? '');

    const newText = await themeToggle.textContent();
    expect(newText).not.toBe(initialText);

    const storedTheme = await page.evaluate(() => localStorage.getItem('c4_theme'));
    expect(storedTheme).toBeTruthy();
  });

  test('navigation between all pages works', async ({ page }) => {
    await loginAsDemo(page);

    await page.click('a.nav-link:has-text("Organizations")');
    await expect(page).toHaveURL(/\/organizations/);

    await page.click('a.nav-link:has-text("Subscriptions")');
    await expect(page).toHaveURL(/\/subscriptions/);

    await page.click('a.nav-link:has-text("Diagram")');
    await expect(page).toHaveURL(/\/diagram/);

    await page.click('a.nav-link:has-text("Dashboard")');
    await expect(page).toHaveURL(/\/$/);
  });

  test('header shows user email after login', async ({ page }) => {
    await loginAsDemo(page);
    await page.goto('/');

    await expect(page.locator('.header-user:has-text("demo@c4.local")')).toBeVisible();
  });

  test('header contains sign out button', async ({ page }) => {
    await loginAsDemo(page);
    await page.goto('/');

    await expect(page.locator('button:has-text("Sign out")')).toBeVisible();
  });

  test('sign out clears token and redirects to login', async ({ page }) => {
    await loginAsDemo(page);
    await page.goto('/');

    await page.click('button:has-text("Sign out")');

    await expect(page).toHaveURL(/\/login/, { timeout: 5000 });

    const token = await page.evaluate(() => localStorage.getItem('c4_token'));
    expect(token).toBeFalsy();
  });

  test('invalid token causes redirect to login', async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => {
      localStorage.setItem('c4_token', 'invalid-expired-token');
    });

    await page.goto('/');
    await page.waitForTimeout(2000);

    await expect(page).toHaveURL(/\/login/);
  });

  test('active nav link is highlighted', async ({ page }) => {
    await loginAsDemo(page);

    await page.goto('/organizations');
    const orgLink = page.locator('a.nav-link:has-text("Organizations")');
    await expect(orgLink).toHaveClass(/active/);

    await page.goto('/diagram');
    const diagramLink = page.locator('a.nav-link:has-text("Diagram")');
    await expect(diagramLink).toHaveClass(/active/);
  });
});
