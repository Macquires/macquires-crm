# BSS Wizard Green Matrix

Tracking matrix for telecom BSS wizards across Hub, Customer 360, Customer List, backend, adapters, and E2E.

| Wizard | Code | Hub | C360 | List | Create API | Confirm | Adapter | E2E |
|--------|------|-----|------|------|------------|---------|---------|-----|
| Activate | ACT | ✅ | ✅ | ✅ offer detail + effective date | ✅ | ✅ | CBS/HLR | ✅ scheduled |
| Reconnect | RCN | ✅ | ✅ | ✅ effective date + BDR gateway | ✅ | ✅ | CBS/HLR | ✅ BDR→RCN |
| Refund | RFD | ✅ | ✅ | ✅ effective date | ✅ | ✅ | CBS + posting | ✅ |
| Bad debt | BDR | ✅ | ✅ | ✅ effective date + supervisor | ✅ | ✅ BO approve | CBS + posting | ✅ write-off BO |
| Device sale | DEV | ✅ | ✅ | ✅ effective date | ✅ | ✅ | CBS + posting | ✅ |
| Suspension | SUS | ✅ | ✅ | ✅ fraud identity | ✅ | ✅ | HLR | ✅ fraud list |
| VAS | VAS | ✅ wizard | ✅ wizard | ✅ toggle (`TelecomVasToggle`) | ✅ | ✅ | HLR + VAS billing mock | ✅ deactivate |
| Change number CNR | CNR | ✅ | ✅ | ✅ | ✅ | ✅ | HLR | ✅ |
| MNP Port-In | MNP | ✅ | ✅ | ✅ | ✅ gateway | ✅ BO | MNP mock/HTTP | ✅ correlation id |
| Migration | MGR | ✅ | ✅ | ✅ offer detail + effective date | ✅ | ✅ | CBS | ✅ scheduled |
| Takeover | TKO | ✅ | ✅ | ✅ | ✅ | ✅ | HLR + obligation mock | ✅ |
| Termination | TRM | ✅ | ✅ | ✅ | ✅ | ✅ | CBS/HLR | ✅ |
| SIM swap | SIM | ✅ | ✅ | ✅ | ✅ | ✅ | HLR | ✅ |
| Support ticket | SUP | ✅ wizard | ✅ wizard | ✅ modal | ✅ `CreateTechnicalTicket` | — | — | ✅ UI smoke |

## Legend

- **Hub** — `TelecomHub.cshtml` wizard + `telecom-bss-wizard-clearance.js`
- **C360** — `Customer360Profile.cshtml.js` inline wizard
- **List** — `CustomerList.cshtml.js` modals
- **Adapter** — `Infrastructure/TelecomIntegrations` mock/HTTP via `TelecomIntegrations:*:Mode`
- **E2E** — `Tests/ASPNET.E2E.Tests` Integration + UI (`Category=UI`)

## Feature flags (`.env.example`)

```
TelecomIntegrations__Mnp__Mode=Mock
TelecomIntegrations__IntelligentNetwork__Mode=Mock
TelecomIntegrations__Cashier__Mode=Mock
TelecomIntegrations__PaymentGateway__Mode=Mock
TelecomIntegrations__DeviceInventory__Mode=Mock
TelecomIntegrations__ESim__Mode=Mock
TelecomIntegrations__Sms__Mode=Mock
```

## CI

- Unit: `dotnet test --filter "Category!=Integration&Category!=Smoke"`
- Integration: `Category=Integration` (includes `TelecomBssGreenClosureE2ETests`)
- UI: `Category=UI` (includes `TelecomBssGreenClosureUiTests` — Hub + List + C360 smoke)
