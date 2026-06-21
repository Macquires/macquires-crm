/** Hub wizard tile selectors — maps demo codes to production data-testid / class hooks. */
export const HUB_TILE = {
  ACT: '[data-testid="hub-wizard-tile-activate"]',
  SIM: '[data-testid="hub-wizard-tile-simswap"]',
  CNR: '[data-testid="hub-wizard-tile-changeNumber"]',
  MGR: '[data-testid="hub-wizard-tile-migrate"]',
  PAY: '.telecom-hub-tiles-section .tile-recharge',
  CGT: '[data-testid="hub-wizard-tile-changeGsm"]',
  TKO: '[data-testid="hub-wizard-tile-takeover"]',
  TRM: '[data-testid="hub-wizard-tile-termination"]',
  SUS: '[data-testid="hub-wizard-tile-suspension"]',
  BDR: '[data-testid="hub-wizard-tile-badDebt"]',
  RCN: '[data-testid="hub-wizard-tile-reconnect"]',
  RFD: '[data-testid="hub-wizard-tile-refund"]',
  DEV: '[data-testid="hub-wizard-tile-deviceSale"]',
  VAS: '[data-testid="hub-wizard-tile-addpackage"]',
  SUP: '[data-testid="hub-wizard-tile-support"]',
} as const;

/** Frontline POS persona — five operational tiles (ACT, SIM, CNR, MGR, PAY). */
export const FRONTLINE_TILE_CODES = ['ACT', 'SIM', 'CNR', 'MGR', 'PAY'] as const;

/** Administrative tiles erased from DOM for showroom via v-if permission gates. */
export const ADMIN_TILE_CODES = ['CGT', 'TKO', 'TRM', 'SUS', 'BDR', 'RCN', 'RFD', 'DEV', 'VAS', 'SUP'] as const;

/** Supervisor-only tiles — also absent for back-office Hub morph. */
export const SUPERVISOR_TILE_CODES = ['RCN', 'RFD', 'DEV', 'VAS', 'SUP'] as const;

/** Back-office compliance morph — five tiles on Hub for BO persona. */
export const BACKOFFICE_TILE_CODES = ['SUS', 'CGT', 'BDR', 'TKO', 'TRM'] as const;

export function hubTileSelector(code: keyof typeof HUB_TILE): string {
  return HUB_TILE[code];
}
