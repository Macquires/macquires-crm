# Telecom MIS metrics — operational definitions (Macquires CRM)

Use these definitions consistently in reports, prompts, and demo talking points. All monetary values are in the **company base currency** unless stated otherwise. All periods are **UTC** unless the business specifies a local timezone.

---

## 1. ARPU — Average Revenue Per User

**Purpose:** Revenue intensity per active retail/B2B customer (Syriatel-style subscriber account mapped to `Customer`).

**Time window:** Calendar month *M* (or rolling 30 days *T*..*T+30d* — pick one per report and label it).

**Numerator (revenue in window):**

- **Preferred (cash-oriented):** Sum of `PaymentReceive.Amount` (or line totals, depending on schema) where payment date ∈ window and payment is allocated to **confirmed** sales documents (e.g. linked `Invoice` in `Confirmed` / paid states).
- **Alternative (accrual-oriented):** Sum of `Invoice` line totals (or header totals) for invoices with `InvoiceDate` ∈ window and status in **billed / confirmed** set.

**Denominator (active users):**

- Count of **distinct `CustomerId`** that had at least one **confirmed** `SalesOrder` or **confirmed** `Invoice` line in the **preceding** window of the same length (e.g. previous month) *or* that had any revenue event in the current window (define “active” once and reuse).
- Exclude `Customer.IsDeleted == true`.

**Formula:**

`ARPU = RevenueInWindow / ActiveCustomersInWindow`

**Edge cases:**

- If `ActiveCustomersInWindow == 0`, ARPU is **null / not applicable**, not zero.
- Refunds/credit notes in the window should **reduce** revenue per your accounting policy (document whether `CreditNote` reduces ARPU numerator).

**Future telecom fields:** When `TelecomSubscription` (or similar) exists, optionally compute **ARPU per active MSISDN** instead of per `Customer`, using distinct `MSISDN` as denominator.

---

## 2. Churn rate

**Purpose:** Share of subscribers/customers who stopped generating billable activity.

**Time window:** Month *M* vs prior month *M-1* (same length).

**Cohort at risk (start of *M*):**

- Customers classified **active** at end of *M-1*: e.g. had ≥1 confirmed invoice or confirmed sales order line in *M-1*, and not deleted.

**Churned in *M*:**

- Customers in the cohort who, during *M*, had **no** confirmed invoice and **no** confirmed sales activity **and** (optional) no open telecom subscription status = Active — until subscriptions exist, omit this clause.

**Formula:**

`ChurnRate = ChurnedCustomersInM / CohortAtRiskStartOfM`

**Optional revenue churn:** `RevenueChurn = (RevenueM-1 from churned subset) / RevenueM-1 total`.

**Edge cases:**

- New customers acquired in *M* are **excluded** from the cohort-at-risk unless you explicitly want “logo churn” (then document a different definition).

**Future telecom:** Replace “sales activity” with **subscription state** (`Active` → `Terminated` / `Suspended` with no return) per MSISDN.

---

## 3. Migration success rate

**Purpose:** Quality of prepaid/postpaid (or package) migration operations once `TelecomMigration` documents exist.

**Time window:** Same as migration report (e.g. month *M*).

**Numerator:**

- Count of `TelecomMigration` rows with:

  - `Status == Confirmed` **and**
  - `HuaweiSyncStatus == Succeeded` (or last billing call outcome = success — align with `IBillingSystemIntegration` mock/log table).

**Denominator:**

- Count of migrations **submitted for completion** in the window: `Status` in (`Confirmed`, `Cancelled`, `Archived`) **or** all non-`Draft` if you treat Draft as not yet “attempted”.

**Formula:**

`MigrationSuccessRate = SuccessfulHuaweiSyncCount / CompletedAttemptCount`

**Alternative (stricter):**

`SuccessfulMigrations / (SuccessfulMigrations + FailedAfterBillingCall)` where failures are explicit business states, not user-cancelled drafts.

**Until entities exist:** Report as **not available**; do not approximate from `SalesOrder` alone.

---

## 4. Implementation notes (Macquires codebase)

- Current **Sales Report** UI is line-based (`SalesOrderItem` via existing API). ARPU/churn require **aggregating queries** or new read models — see `SalesReportList.cshtml.js` as a grid shell only.
- Persisted **audit** fields on `BaseEntity` support “who confirmed migration” but not MIS numerators by themselves.
- When adding telecom tables, add **indexes** on `(Status, CreatedAtUtc)` and foreign keys to `Customer` for performant MIS.

---

## 5. One-line cheat sheet (for demos)

| Metric | Numerator | Denominator | Window |
|--------|-----------|-------------|--------|
| ARPU | Confirmed payments or invoices | Distinct active customers | Month / 30d |
| Churn | Cohort customers with no billable activity | Active customers at period start | Month vs prior |
| Migration success | Confirmed + billing sync OK | Completed migration attempts | Same as report |

---

## 6. RBAC catalog (telecom demo)

These **ASP.NET Identity** role names are seeded (see `RoleHelper.TelecomOperationalRoles`) and merged into `GetAdminRoles()`:

| Role | Intended use |
|------|----------------|
| `TelecomShowroom` | Create draft migration / takeover requests; no confirm/billing. |
| `TelecomBackOffice` | Confirm workflows; triggers post-save billing integration in future handlers. |

Navigation adds a **Telecom** module with URL segment `Telecom` (see `NavigationTreeStructure`). The default admin receives **all** catalog roles on each startup via `UserAdminSeeder.AssignAllCatalogRolesToDefaultAdminAsync`.

Controllers/commands should enforce `[Authorize(Roles = "TelecomBackOffice")]` (or combined policies) on **Confirm** endpoints in addition to any UI hiding.
