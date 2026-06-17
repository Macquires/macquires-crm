# BSS Wizard Green Matrix

Tracking matrix for telecom BSS wizards across Hub, Customer 360, Customer List, backend, and tests.

**Scope note:** External HTTP adapters (VAS billing, billing posting, TKO obligation settlement) are intentionally mock-only until vendor endpoints exist — excluded from green closure.

| Wizard | Code | Hub | C360 | List | Create API | Confirm | E2E API | E2E UI |
|--------|------|-----|------|------|------------|---------|---------|--------|
| Activate | ACT | ✅ | ✅ offer card + effective | ✅ offer card + effective | ✅ | ✅ | ✅ scheduled | ✅ Hub + C360 + List ACT |
| Reconnect | RCN | ✅ | ✅ | ✅ effective + BDR gateway | ✅ | ✅ | ✅ BDR→RCN + scheduled | ✅ Hub + C360 + List payment |
| Refund | RFD | ✅ | ✅ snapshots | ✅ effective | ✅ | ✅ | ✅ happy-path + BO | ✅ Hub + C360 + List effective |
| Bad debt | BDR | ✅ | ✅ supervisor/plan | ✅ effective + supervisor | ✅ | ✅ BO | ✅ write-off BO | ✅ Hub + C360 + List write-off |
| Device sale | DEV | ✅ inventory + down-pay | ✅ inventory + sale type | ✅ inventory + installment | ✅ | ✅ | ✅ happy-path | ✅ Hub + C360 + List inventory |
| Suspension | SUS | ✅ fraud | ✅ fraud | ✅ fraud + identity | ✅ | ✅ | ✅ fraud BO API | ✅ Hub + C360 + List fraud |
| VAS | VAS | ✅ wizard | ✅ wizard | ✅ wizard + toggle | ✅ | ✅ | ✅ deactivate | ✅ Hub + C360 + List wizard |
| Change number / MNP | CNR | ✅ | ✅ | ✅ Port-In UI | ✅ gateway | ✅ BO | ✅ correlation + happy-path | ✅ Hub + C360 + List MNP |
| Migration | MGR | ✅ | ✅ | ✅ offer + effective | ✅ | ✅ | ✅ scheduled | ✅ Hub + C360 + List offering |
| Takeover | TKO | ✅ | ✅ | ✅ modal | ✅ | ✅ | ✅ happy-path + BO | ✅ Hub + C360 + List deposit |
| Termination | TRM | ✅ | ✅ | ✅ effective | ✅ | ✅ | ✅ happy-path + BO | ✅ Hub + C360 + List fraud ticket |
| SIM swap | SIM | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ happy-path | ✅ Hub + C360 + List replacement |
| Support ticket | SUP | ✅ wizard | ✅ wizard | ✅ modal | ✅ `CreateTechnicalTicket` | — | — | ✅ Hub + List + C360 smoke |
| Change GSM type | CGT | ✅ path parity | ✅ path parity | ✅ path parity | ✅ | ✅ | ✅ happy-path | ✅ Hub + C360 + List target/path |

## Legend

- **Hub** — `TelecomHub.cshtml` + `telecom-bss-wizard-clearance.js` (`data-testid="hub-*"`)
- **C360** — `Customer360Profile.cshtml.js` inline wizard (`data-testid="c360-*"`)
- **List** — `CustomerList.cshtml.js` modals (`data-testid="list-*"`)
- **E2E API** — `Category=Integration` (`TelecomOperationHappyPathTests` covers all 13 kinds + specialized suites)
- **E2E UI** — `Category=UI` Playwright smoke (`TelecomBssGreenClosureUiTests`, `TelecomChangeNumberWizardUiTests`)

## Deferred (external integration only)

| Area | Status |
|------|--------|
| `IVasBillingIntegration` HTTP | mock only |
| `IBillingPostingIntegration` HTTP | mock only |
| `ITakeOverObligationSettlementIntegration` HTTP | mock only |

## Feature flags (`.env.example`)

```
TelecomIntegrations__Mnp__Mode=Mock
TelecomIntegrations__IntelligentNetwork__Mode=Mock
TelecomIntegrations__Cashier__Mode=Mock
TelecomIntegrations__PaymentGateway__Mode=Mock
TelecomIntegrations__DeviceInventory__Mode=Mock
TelecomIntegrations__ESim__Mode=Mock
TelecomIntegrations__Sms__Mode=Mock
TelecomIntegrations__VasBillingEnabled=false
```

## CI

- Unit: `dotnet test --filter "Category!=Integration&Category!=Smoke"`
- Integration: `Category=Integration`
- UI: `Category=UI`

## Playwright smoke inventory (`TelecomBssGreenClosureUiTests`)

All 14 telecom line operations covered across Hub / C360 / List where applicable. Hub tests use `hub-wizard-tile-*` tiles; C360 uses deep-link `?wizard=&lineKey=`; List uses line-actions dropdown.

| Surface | Wizards with UI smoke |
|---------|----------------------|
| Hub | ACT, RCN, RFD, BDR, DEV, SUS, VAS, CNR, MGR, TKO, TRM, SIM, SUP, CGT |
| C360 | ACT, RCN, RFD, BDR, DEV, SUS, VAS, CNR, MGR, TKO, TRM, SIM, SUP, CGT |
| List | ACT, RCN, RFD, BDR, DEV, SUS, VAS, CNR, MGR, TKO, TRM, SIM, SUP, CGT |
