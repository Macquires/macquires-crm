import { expect, type Locator, type Page } from '@playwright/test';

export const DEMO_PASSWORD = '123456';

export const USERS = {
  executiveMis: 'st-mis@syriatelecom-demo.local',
  showroom: 'st-showroom@syriatelecom-demo.local',
  backOffice: 'st-backoffice@syriatelecom-demo.local',
} as const;

export const ROUTES = {
  commandCenter: '/Executive/CommandCenter',
  telecomHub: '/Telecom/TelecomHub',
  backOfficeDashboard: '/Telecom/BackOfficeDashboard',
} as const;

/** Matches [DELAY - PAUSE FOR 2 SECONDS] markers in HTML cue cards. */
export const DEMO_PAUSE_MS = 2_000;

export async function demoPause(page: Page, ms: number = DEMO_PAUSE_MS): Promise<void> {
  await page.waitForTimeout(ms);
}

export async function loginViaUi(page: Page, email: string, password = DEMO_PASSWORD): Promise<void> {
  const baseUrl = process.env.BASE_URL ?? 'http://localhost:5000';
  await page.context().addCookies([
    {
      name: '.AspNetCore.Culture',
      value: 'c=en|uic=en',
      url: baseUrl,
    },
  ]);

  await page.goto('/Accounts/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill(email);
  await page.locator('#Password').fill(password);
  await page.locator("button[type='submit']").click();
  await page.waitForURL((url) => !url.pathname.includes('/Accounts/Login'), { timeout: 60_000 });
}

export async function highlightLocator(locator: Locator): Promise<void> {
  await locator.evaluate((el) => {
    el.style.outline = '3px solid #dc2626';
    el.style.outlineOffset = '4px';
    el.style.boxShadow = '0 0 24px rgba(220, 38, 38, 0.45)';
  });
}

export async function assertAbsentFromDom(page: Page, selector: string): Promise<void> {
  await expect(page.locator(selector)).toHaveCount(0);
}

export async function assertPresent(page: Page, selector: string): Promise<void> {
  await expect(page.locator(selector)).toHaveCount(1);
  await expect(page.locator(selector)).toBeVisible();
}
