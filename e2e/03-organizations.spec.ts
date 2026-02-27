import { test, expect } from '@playwright/test';
import { loginAsDemo } from './helpers';

test.describe('Organization & Project Management', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDemo(page);
  });

  test('organizations page loads with setup prompt', async ({ page }) => {
    await page.click('a.nav-link:has-text("Organizations")');
    await page.waitForURL('**/organizations');

    await expect(page.locator('h1:has-text("Organization")')).toBeVisible();
    await expect(page.locator('text=Set up your organization')).toBeVisible();
    await expect(page.locator('#org-name-input')).toBeVisible();
    await expect(page.locator('button:has-text("Register")')).toBeVisible();
  });

  test('register organization succeeds', async ({ page }) => {
    await page.goto('/organizations');

    const orgName = `E2E Org ${Date.now()}`;
    await page.fill('#org-name-input', orgName);

    const [response] = await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/organizations') && resp.request().method() === 'POST', { timeout: 10000 }),
      page.click('button:has-text("Register")'),
    ]);

    expect(response.status()).toBe(201);

    await expect(page.locator(`strong:has-text("${orgName}")`)).toBeVisible({ timeout: 5000 });
    await expect(page.locator('#project-name-input')).toBeVisible();
    await expect(page.locator('button:has-text("Create Project")')).toBeVisible();
  });

  test('create project after registering organization', async ({ page }) => {
    await page.goto('/organizations');

    const orgName = `Org Proj ${Date.now()}`;
    await page.fill('#org-name-input', orgName);
    await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/organizations') && resp.request().method() === 'POST'),
      page.click('button:has-text("Register")'),
    ]);

    await expect(page.locator('#project-name-input')).toBeVisible({ timeout: 5000 });

    const projName = `Project ${Date.now()}`;
    await page.fill('#project-name-input', projName);

    const [projResponse] = await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/projects') && resp.request().method() === 'POST', { timeout: 10000 }),
      page.click('button:has-text("Create Project")'),
    ]);

    expect(projResponse.status()).toBe(201);

    await expect(page.locator(`li span:has-text("${projName}")`)).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=Projects (1)')).toBeVisible();
  });

  test('empty project state shows message', async ({ page }) => {
    await page.goto('/organizations');

    const orgName = `Empty Org ${Date.now()}`;
    await page.fill('#org-name-input', orgName);
    await Promise.all([
      page.waitForResponse(resp => resp.url().includes('/api/organizations') && resp.request().method() === 'POST'),
      page.click('button:has-text("Register")'),
    ]);

    await expect(page.locator('text=No projects yet')).toBeVisible({ timeout: 5000 });
  });
});
