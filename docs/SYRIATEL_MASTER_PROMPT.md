# Syriatel / Macquires CRM — Imperial Master Prompt & Runbook

This file is the **canonical prompt and discipline guide** for evolving the solution toward a **unified OSS/BSS telecom CRM** narrative (TM Forum SID concepts, Huawei CBS bridge story, action-oriented UI). It does **not** replace product ownership or legal commitments.

**Arabic strategic prompt + project-alignment annex:** [CRM_MASTER_PROMPT_AR.md](./CRM_MASTER_PROMPT_AR.md) (same repo; do not duplicate SQL-level detail there — keep mapping in separate migration docs).

**Telecom UI terminology (mandatory for front-end copy):** [TELECOM_UI_GLOSSARY.md](./TELECOM_UI_GLOSSARY.md)

**Product name (Syriatel / contract):** **Syriatel Macquires CRM** — the white-label build is referred to internally as **Macquires Telecom** until a full nuclear cut (Track B) or solution rename completes.

---

## Code discipline (mandatory for humans & AI)

1. **Follow existing patterns:** Same folder layout, MediatR commands/queries, `BaseEntity`, repositories, `NumberSequenceService`, DI style as the rest of Indotalent/Macquires — no random layer rewrites.
2. **Clean architecture:** Domain stays pure; Application depends on abstractions; Infrastructure holds EF + integrations; Razor/Vue pages stay thin.
3. **Small, reviewable changes:** One clear goal per change-set; no mass reformat of unrelated files.
4. **Performance:** Prefer `AsNoTracking` on reads; avoid N+1; index searchable fields (e.g. MSISDN) when adding entities; bounded Polly retries (no infinite loops).
5. **Verify:** `dotnet build` on the solution (or at least Application + Infrastructure) after substantive edits.
6. **Demo honesty:** UX speed and query efficiency are goals — measure response times and keep builds green rather than empty slogans.

---

## Stack (as implemented)

- **Backend:** .NET 9, Clean Architecture, MediatR (CQRS).
- **Frontend:** Razor Pages + Vue 3 (`setup`) + Syncfusion EJ2 where lists are heavy; Telecom Hub uses Bootstrap + Vue for the “big four” dashboard.
- **Resilience:** `IBillingSystemIntegration` → `HuaweiCbsBillingIntegration` with **Polly** retries and **`BillingIntegrationLog`** rows per attempt (`appsettings.json` section `TelecomBilling`).
- **Optional hub integrations:** `IChargingSystemIntegration`, `ISmsGatewayIntegration` (mocks; demo endpoints on `TelecomController`).

---

## The Imperial Telecom Prompt (vision scope)

**Role:** Lead Software Architect & Telecom Industry Expert (TM Forum narrative).

**Mission:** Evolve **Macquires CRM** into a **Unified Telecom CRM & Operations Suite** aligned with a Syrian tier-1 operator demo (Syriatel-style).

**Strict technical architecture**

- **Backend:** .NET 9, Clean Architecture, MediatR (CQRS).
- **Frontend:** Razor Pages + Vue.js (setup) + Syncfusion EJ2.
- **Standards:** TM Forum **SID** concepts — expand gradually; full SID is a multi-year product, not one sprint.
- **Reliability:** **Polly** retry policies on external integrations (billing bridge).

### 1. Core domain — subscriber 360°

- Entities: `SubscriberProfile`, `TelecomSubscription`, `MsisdnAsset`, `TelecomOperationRequest`, `BillingIntegrationLog`.
- Attributes: MSISDN (093/099 style in demo data), ICCID, IMSI, PUK1/2, subscription type (Prepaid/Postpaid/Hybrid).
- Pool lifecycle: `Available` → `Reserved` → `Active` → `Suspended` → `Quarantined`.
- `DocumentStatus` on operations/subscriptions for compliance storytelling.

### 2. Telecom engine — “big four”

In `Application/Features/TelecomManager`:

- **New Activation**, **Migration (MGR)**, **Take Over (TKO)**, **SIM Swap** (and placeholders for VAS/MNP).
- **Numbering:** `NumberSequenceService` with prefixes `ACT-`, `MGR-`, `TKO-`, `SIM-`, … (`useDate: false`).
- **Billing bridge:** On **Confirm**, call `IBillingSystemIntegration` (Huawei CBS **mock**) with transactional save story: internal state saved, external call logged, bounded retries.

### 3. UI mandate — action-oriented

- **Big buttons:** Activate / Migrate / Take Over / Support entry points (`/Telecom/TelecomHub`).
- **Wizard shell:** 3 steps (Input → Document → Confirm) — extend with real forms as needed.
- **Clean grids:** Prefer subscriber name, MSISDN/plan/status; de-clutter retail-only columns on telecom paths (`SalesOrderList`, `ProductList` tuned for Arabic labels + `ServiceCode`).
- **Map:** Mock geographic badges for branch/network story (real GIS later).

### 4. MIS / KPIs

- Demo queries for **ARPU**, **Churn placeholder**, **branch heat** from existing sales data; see `docs/TELECOM_MIS_DEFINITIONS.md` for metric definitions.

### 5. Seeding — “Syriatel soul”

- Warehouses & products: Arabic Syriatel-style demo names + **service codes** (`YAHALA_SHABAB`, `SABA_10GB`, `BUSINESS_PRO_50`, …).
- **Hero persona:** **سعدون الشامي** — ~10-year customer; scenario: prepaid→postpaid + 5G router for new home in **مشروع دمر**; seeded `TelecomOperationRequest` (MGR) in **Draft** with **Uploaded** documents, ready for confirm + CBS log demo.
- **Scale:** ~100 subscribers worth of `Customer` + `SubscriberProfile` + `MsisdnAsset` + `TelecomSubscription` via `TelecomSyriatelSeeder` when `SubscriberProfile` is empty (after demo seed).

---

## UI extension (mandate summary)

- Universal search (MSISDN, name, **National ID**) via `GetTelecomUniversalSearch`.
- Color-coded statuses in Hub tables (badges).
- Reduce technical noise (IDs, GUID columns) on retail-facing telecom screens.

---

## Demo playbook (before a live meeting)

1. **Hero story:** Open Telecom Hub → mention سعدون الشامي + MGR draft + دمر + router narrative.
2. **Resilience:** Click **تأكيد CBS (أول مسودة جاهزة)** on Sadoun’s row — watch `BillingIntegrationLog`: failed attempts then success after Polly (configure `TelecomBilling:FailAttemptsBeforeSuccess`).
3. **Contrast script:** “Minutes, not twenty minutes” for retail completion vs legacy 2010-era tools.
4. **Safety net:** After seeding, take a **database backup** before the live demo (operational step, not automated here).

---

## API surface (Telecom)

Base route: `/api/Telecom/…` (JWT required except health checks elsewhere).

- `POST CreateTelecomOperation`, `POST UploadTelecomOperationDocument`, `POST ConfirmTelecomOperation`
- `POST ImportSimInventoryBatch` (batch SIM/ICCID/PUK → `MsisdnAsset` pool)
- `GET GetTelecomOperationList`, `GET GetBillingIntegrationLogList`, `GET GetTelecomDashboardKpis`, `GET GetTelecomUniversalSearch`
- `POST TouchChargingDemo`, `POST SendSmsDemo` (mocks)
- `CreateTelecomOperation` accepts optional **`targetOfferName`** for **Migration** (e.g. «سيريتل ميكس») — stored on `TelecomOperationRequest`.

---

## Thursday demo logins (strict telecom roles)

Password for all accounts below: **`123456`**. Each user has **one** Identity role for RBAC demos (open in separate browsers if needed).

| Persona | Email | Role |
|--------|------|------|
| Showroom | `st-showroom@syriatelecom-demo.local` | `TelecomShowroom` |
| Back office | `st-backoffice@syriatelecom-demo.local` | `TelecomBackOffice` |
| Call center | `st-callcenter@syriatelecom-demo.local` | `TelecomCallCenter` |
| GM / MIS | `st-mis@syriatelecom-demo.local` | `TelecomManagement` |
| Regional director (North) | `st-regional-north@syriatelecom-demo.local` | `TelecomManagement` |
| Regional director (Central) | `st-regional-central@syriatelecom-demo.local` | `TelecomManagement` |
| Regional director (Coast) | `st-regional-coast@syriatelecom-demo.local` | `TelecomManagement` |
| Branch manager (Mezzeh) | `st-branch-mezzeh@syriatelecom-demo.local` | `TelecomManagement` |
| Branch manager (Aleppo) | `st-branch-aleppo@syriatelecom-demo.local` | `TelecomManagement` |
| Branch manager (Tartus) | `st-branch-tartus@syriatelecom-demo.local` | `TelecomManagement` |

Strategic analytics: `/Telecom/StrategicAnalytics` — GM sees national drill-down; regional/branch accounts are scoped via `OrgUnit` (seeded in `OrgUnitSeeder` + `StrategicMisDemoSeeder`).

Seeder: `Infrastructure/SeedManager/Demos/TelecomDemoIdentitySeeder.cs` (runs after catalog roles are assigned). **JWT** issued on login/refresh includes **`ClaimTypes.Role`** entries so `[Authorize(Roles = …)]` on `TelecomController` is enforced.

---

## Track A vs Track B (reference)

- **Track A (implemented here):** RBAC, server + client menu filtering for strict telecom users, Telecom Hub tiles/wizards gated by role, Customer list shows **MSISDN + Prepaid/Postpaid** when linked to `SubscriberProfile`, Migration **target package** field on create.
- **Track B (nuclear telecom-only):** Requires a **signed allow-delete inventory** before mass removal. Recommended: dedicated **git branch** or **new repository** that reuses only security/identity patterns — not executed wholesale on `main` in one step.

### Track B — starter inventory (sign before delete)

High-noise areas to retire in a nuclear pass (Indotalent-style navigation still present in repo): **Pipeline** (Campaigns, Budgets, Expenses, Leads, Sales teams), **Third Party** vendor/supplier modules, **Inventory** (Products, Warehouses, Goods receipts, Stock), **Purchase** / **Sales** document chains unrelated to OSS/BSS, **Accounting** (GL, Cash, Bank, Tax), **HR** / **Payroll**, **Assets**, **Projects**, **POS**, **Shipments** — keep anything still referenced from `TelecomOperationRequest`, `SubscriberProfile`, billing logs, or Identity.

### Track B — rename map (product doc only)

| Current | Target (nuclear narrative) |
|--------|---------------------------|
| `Customer` (party row) | Keep table or rename in UI to **Subscriber** / party |
| `Warehouse` | **Service center** / branch |
| `Product` (non-SIM) | **TelecomService** / catalog simplification |

### Track B — safe cleanup (no mass delete on `main`)

Track A left retail modules in the repo on purpose. **Dead-code stripping** on shared branches should stay limited to proven orphans (unused files, unused package refs in a single project) with a green `dotnet build` after each batch — see plan todo **cleanup-dead-code-safe**. A **signed inventory** (**cleanup-inventory-audit**) is the gate before any nuclear delete.

---

Breadth (SIM swap, loyalty, identity path) + **simple** retail UI + international framing (TM Forum, Huawei CBS story) while staying honest about what is **demo code** vs production hardening.
