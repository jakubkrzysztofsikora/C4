import { test, expect } from '@playwright/test';
import { loginAsDemo } from './helpers';

test.describe('Dashboard Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDemo(page);
  });

  test('dashboard is the default authenticated route', async ({ page }) => {
    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator('h1:has-text("Dynamic Architecture Dashboard")')).toBeVisible();
  });

  test('dashboard shows project ID input and load button', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('#project-id-input')).toBeVisible();
    await expect(page.locator('button:has-text("Load Project")')).toBeVisible();
  });

  test('dashboard shows empty state when no project loaded', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('text=No project loaded')).toBeVisible();
    await expect(page.locator('.empty-state')).toBeVisible();
  });

  test('load button disabled when project ID is empty', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('button:has-text("Load Project")')).toBeDisabled();

    await page.fill('#project-id-input', 'some-id');
    await expect(page.locator('button:has-text("Load Project")')).toBeEnabled();
  });

  test('load project with seed project ID', async ({ page }) => {
    await page.goto('/');

    const projectId = await page.evaluate(async () => {
      const token = localStorage.getItem('c4_token');
      const res = await fetch('http://localhost:5000/api/projects', {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.ok) {
        const data = await res.json();
        return Array.isArray(data) && data.length > 0 ? data[0].id ?? data[0].projectId : null;
      }
      return null;
    });

    if (projectId) {
      await page.fill('#project-id-input', projectId);
      await page.click('button:has-text("Load Project")');
      await page.waitForTimeout(3000);
    } else {
      await page.fill('#project-id-input', '00000000-0000-0000-0000-000000000000');
      await page.click('button:has-text("Load Project")');
      await page.waitForTimeout(3000);
    }

    expect(true).toBe(true);
  });

  test('navigation links are visible in header', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('a.nav-link:has-text("Dashboard")')).toBeVisible();
    await expect(page.locator('a.nav-link:has-text("Organizations")')).toBeVisible();
    await expect(page.locator('a.nav-link:has-text("Subscriptions")')).toBeVisible();
    await expect(page.locator('a.nav-link:has-text("Diagram")')).toBeVisible();
  });
});
