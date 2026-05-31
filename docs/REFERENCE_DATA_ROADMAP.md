# Reference data roadmap (dynamic lookups)

This complements the MVP delivered for **telecom subscription line types** (`TelecomSubscriptionTypes` + admin UI).

## Done (MVP)

- `TelecomSubscriptionTypeLookup` / table `TelecomSubscriptionTypes`: Code, NameAr, NameEn, DisplayColor, SortOrder, IsActive, IsDefault.
- `TelecomSubscription.SubscriptionTypeId` FK; startup SQL patch migrates legacy int enum column.
- API: `TelecomSubscriptionTypeController` (list + create + update).
- UI: `/TelecomSubscriptionTypes/TelecomSubscriptionTypeList` (Settings menu).
- Customer grid + Telecom hub use API-driven types; integration **Code** unchanged for external systems.

## Candidate next lookups (inventory)

| Area | Current shape | Suggested approach |
|------|---------------|-------------------|
| Telecom | `MsisdnPoolStatus`, `TelecomDocumentStatus`, `TelecomOperationKind`, `TelecomOperationStatus` in `Domain/Enums/TelecomEnums.cs` | Promote to reference tables only if operators must rename/disable values without deploy. |
| Inventory / sales | Other `enum` types under `Core/Domain/Enums` | Same rule: code-stable column + localized labels when UX requires admin control. |
| UI | Hard-coded `<option>` lists outside telecom types | Replace with `Get*List` endpoints as each screen is touched. |

## Principles

1. **Code vs label**: `Code` is the integration contract; labels are user-editable.
2. **No hard delete** for rows in use: prefer `IsActive = false` (and optional soft-delete on `BaseEntity`).
3. **One default** where the domain requires a fallback for null user input.
