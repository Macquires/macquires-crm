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
  BACKOFFICE_TILE_CODES,
  FRONTLINE_TILE_CODES,
  SUPERVISOR_TILE_CODES,
  hubTileSelector,
} from '../lib/hub-tiles';

/**
 * Live demo: back-office agent sees compliance tiles on Hub; Approve/Reject live on BackOfficeDashboard only.
 * Cue card: guides/demo_03_backoffice.html
 */
test.describe('Demo 03 — BackOffice Single Source of Truth @demo', () => {
  test('back-office persona morphs Hub and isolates approval console', async ({ page }) => {
    await loginViaUi(page, USERS.backOffice);
    await demoPause(page);

    await page.goto(ROUTES.telecomHub, { waitUntil: 'networkidle' });
    await expect(page.locator('#telecomSearchInput')).toBeVisible();
    await demoPause(page);

    for (const code of BACKOFFICE_TILE_CODES) {
      const tile = page.locator(hubTileSelector(code));
      await tile.scrollIntoViewIfNeeded();
      await assertPresent(page, hubTileSelector(code));
      await highlightLocator(tile);
      await demoPause(page);
    }

    for (const code of FRONTLINE_TILE_CODES) {
      await assertAbsentFromDom(page, hubTileSelector(code));
    }

    for (const code of SUPERVISOR_TILE_CODES) {
      await assertAbsentFromDom(page, hubTileSelector(code));
    }

    await page.locator('.telecom-table').scrollIntoViewIfNeeded();
    await demoPause(page);
    await assertAbsentFromDom(page, '.telecom-table .bo-approve-op');
    await assertAbsentFromDom(page, '.telecom-table .bo-reject-op');
    await demoPause(page);

    const queueLink = page.locator('a[href="/Telecom/BackOfficeDashboard"]');
    if (await queueLink.count()) {
      await queueLink.first().scrollIntoViewIfNeeded();
      await demoPause(page);
      await queueLink.first().click();
    } else {
      await page.goto(ROUTES.backOfficeDashboard, { waitUntil: 'networkidle' });
    }

    await expect(page.locator('#boTelecomQueuePanel')).toBeVisible();
    await demoPause(page);

    const approveButtons = page.locator('.bo-approve-op');
    const rejectButtons = page.locator('.bo-reject-op');
    const approvalCount = await approveButtons.count();

    if (approvalCount > 0) {
      await approveButtons.first().scrollIntoViewIfNeeded();
      await highlightLocator(approveButtons.first());
      await demoPause(page);
      await highlightLocator(rejectButtons.first());
      await demoPause(page);
    } else {
      await expect(page.locator('#boTelecomQueueBody')).toBeVisible();
      await demoPause(page);
    }
  });
});
