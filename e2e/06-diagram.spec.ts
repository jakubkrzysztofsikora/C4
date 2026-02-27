import { test, expect } from '@playwright/test';
import { loginAsDemo, API_BASE_URL } from './helpers';

test.describe('Diagram Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDemo(page);
  });

  test('navigate to diagram page', async ({ page }) => {
    await page.click('a.nav-link:has-text("Diagram")');
    await expect(page).toHaveURL(/\/diagram/);
    await expect(page.locator('h2:has-text("Architecture Diagram")')).toBeVisible();
  });

  test('diagram sidebar controls are visible', async ({ page }) => {
    await page.goto('/diagram');

    await expect(page.locator('.diagram-sidebar')).toBeVisible();
    await expect(page.locator('.toolbox')).toBeVisible();
  });

  test('C4 level filter dropdown works', async ({ page }) => {
    await page.goto('/diagram');

    const levelSelect = page.locator('.toolbox select');
    await expect(levelSelect).toBeVisible();

    await levelSelect.selectOption('Container');
    await expect(levelSelect).toHaveValue('Container');

    await levelSelect.selectOption('Component');
    await expect(levelSelect).toHaveValue('Component');

    await levelSelect.selectOption('Context');
    await expect(levelSelect).toHaveValue('Context');
  });

  test('search filter input is functional', async ({ page }) => {
    await page.goto('/diagram');

    const searchInput = page.locator('input[placeholder="Search service"]');
    await expect(searchInput).toBeVisible();
    await searchInput.fill('test-service');
    await expect(searchInput).toHaveValue('test-service');
  });

  test('zoom and timeline sliders are visible', async ({ page }) => {
    await page.goto('/diagram');

    const sliders = page.locator('input[type="range"]');
    await expect(sliders.first()).toBeVisible();
    const count = await sliders.count();
    expect(count).toBeGreaterThanOrEqual(2);
  });

  test('export buttons are visible', async ({ page }) => {
    await page.goto('/diagram');

    await expect(page.locator('button:has-text("Export SVG")')).toBeVisible();
    await expect(page.locator('button:has-text("Export PDF")')).toBeVisible();
  });

  test('legend badges are visible', async ({ page }) => {
    await page.goto('/diagram');

    const legend = page.locator('.legend');
    await expect(legend).toBeVisible();
    await expect(legend.locator('.badge.green')).toBeVisible();
    await expect(legend.locator('.badge.yellow')).toBeVisible();
    await expect(legend.locator('.badge.red')).toBeVisible();
    await expect(legend.locator('.badge.drift')).toBeVisible();
  });

  test('diagram canvas area renders ReactFlow', async ({ page }) => {
    await page.goto('/diagram');

    const canvas = page.locator('.diagram-stage');
    await expect(canvas).toBeVisible({ timeout: 5000 });

    const reactFlow = page.locator('.react-flow');
    await expect(reactFlow).toBeVisible({ timeout: 5000 });
  });

  test('diagram with project ID in URL', async ({ page }) => {
    const apiBaseUrl = API_BASE_URL;
    const projectId = await page.evaluate(async (baseUrl) => {
      const token = localStorage.getItem('c4_token');
      const res = await fetch(`${baseUrl}/api/projects`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.ok) {
        const data = await res.json();
        return Array.isArray(data) && data.length > 0 ? data[0].id ?? data[0].projectId : null;
      }
      return null;
    }, apiBaseUrl);

    const id = projectId ?? '00000000-0000-0000-0000-000000000001';
    await page.goto(`/diagram/${id}`);
    await expect(page).toHaveURL(new RegExp(`/diagram/${id}`));
    await expect(page.locator('.diagram-sidebar')).toBeVisible();
  });
});
