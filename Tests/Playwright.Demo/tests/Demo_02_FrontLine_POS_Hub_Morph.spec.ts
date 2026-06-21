import { expect, test } from '@playwright/test';
import {
  ROUTES,
  USERS,
  assertAbsentFromDom,
  assertPresent,
  demoPause,
  highlightLocator,
  loginViaUi,
} from '../lib/demo-helpers';
import {
  ADMIN_TILE_CODES,
  FRONTLINE_TILE_CODES,
  hubTileSelector,
} from '../lib/hub-tiles';

/**
 * Live demo: showroom agent sees only frontline tiles; admin tiles are removed from DOM (v-if).
 * Cue card: guides/demo_02_frontline_hub.html
 */
test.describe('Demo 02 — Frontline POS Hub Morph @demo', () => {
  test('showroom persona morphs Hub to five frontline tiles only', async ({ page }) => {
    await loginViaUi(page, USERS.showroom);
    await demoPause(page);

    await page.goto(ROUTES.telecomHub, { waitUntil: 'networkidle' });
    await expect(page.locator('#telecomSearchInput')).toBeVisible();
    await demoPause(page);

    for (const code of FRONTLINE_TILE_CODES) {
      const tile = page.locator(hubTileSelector(code));
      await tile.scrollIntoViewIfNeeded();
      await assertPresent(page, hubTileSelector(code));
      await highlightLocator(tile);
      await demoPause(page);
    }

    for (const code of ADMIN_TILE_CODES) {
      await assertAbsentFromDom(page, hubTileSelector(code));
    }

    await expect(page.locator('.telecom-hub-tiles-section [data-testid^="hub-wizard-tile-"]')).toHaveCount(4);
    await expect(page.locator('.telecom-hub-tiles-section .tile-recharge')).toHaveCount(1);
    await demoPause(page);
  });
});
