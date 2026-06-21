import { expect, test } from '@playwright/test';
import {
  ROUTES,
  USERS,
  demoPause,
  highlightLocator,
  loginViaUi,
} from '../lib/demo-helpers';

/**
 * Live demo: GM MIS Executive Command Center — scope bar, hero KPIs, region toggle without reload.
 * Cue card: guides/demo_01_command_center.html
 */
test.describe('Demo 01 — Sovereign Command Center Pulse @demo', () => {
  test('executive MIS scope bar, hero cards, and region filter', async ({ page }) => {
    await loginViaUi(page, USERS.executiveMis);
    await demoPause(page);

    const summaryResponse = page.waitForResponse(
      (r) => r.url().includes('/Telecom/GetExecutiveCommandCenterSummary') && r.ok(),
      { timeout: 90_000 },
    );

    await page.goto(ROUTES.commandCenter, { waitUntil: 'networkidle' });
    await summaryResponse;

    await expect(page.locator('#commandCenterGate')).toHaveClass(/d-none/);
    await expect(page.locator('#commandCenterApp')).toBeVisible();
    await demoPause(page);

    const scopeBar = page.locator('.executive-scope-bar');
    await scopeBar.scrollIntoViewIfNeeded();
    await expect(scopeBar).toBeVisible();
    await highlightLocator(scopeBar);
    await demoPause(page);

    const scorecardHost = page.locator('#executiveScorecardHost');
    await expect(scorecardHost.locator('.strategic-bento-card').first()).toBeVisible({ timeout: 90_000 });

    const heroCards = scorecardHost.locator('.scorecard-hero-card');
    await expect(heroCards).toHaveCount(4);

    for (let i = 0; i < 4; i++) {
      const card = heroCards.nth(i);
      await card.scrollIntoViewIfNeeded();
      await highlightLocator(card);
      await demoPause(page);
    }

    const regionSelect = scopeBar.locator('select.form-select').first();
    const optionCount = await regionSelect.locator('option').count();
    const urlBefore = page.url();

    if (optionCount > 2) {
      await regionSelect.selectOption({ index: 1 });
      await demoPause(page);

      const scopeRefresh = page.waitForResponse(
        (r) => r.url().includes('/Telecom/GetExecutiveCommandCenterSummary') && r.ok(),
      );
      await scopeBar.locator('.btn-strategic-gold').click();
      await scopeRefresh;

      expect(page.url()).toBe(urlBefore);
      await expect(page.locator('#commandCenterApp')).toBeVisible();
      await expect(scorecardHost.locator('.strategic-bento-card').first()).toBeVisible();
      await demoPause(page);

      if (optionCount > 3) {
        await regionSelect.selectOption({ index: 2 });
        await demoPause(page);
        const secondRefresh = page.waitForResponse(
          (r) => r.url().includes('/Telecom/GetExecutiveCommandCenterSummary') && r.ok(),
        );
        await scopeBar.locator('.btn-strategic-gold').click();
        await secondRefresh;
        expect(page.url()).toBe(urlBefore);
        await demoPause(page);
      }
    }
  });
});
