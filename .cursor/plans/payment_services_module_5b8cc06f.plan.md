---
name: Payment Services Module
overview: "§12 Recharge, Voucher & Payment — **منفّذ Full-Stack ديمو (~82%)** عبر `TelecomPaymentTransaction` + orchestrator + 360/List/Hub + BO KPIs/عكس."
todos:
  - id: p1-fullstack-foundation
    content: "P1: PAY- ledger + PaymentServicesOrchestrator + Create/Confirm API + 360/List شحن (gateway ref + receipt)"
    status: completed
  - id: p2-fullstack-voucher
    content: "P2: ValidateVoucher + Redeem + SMS (PaymentServicesNotificationHandler) + voucher في List/Hub/360"
    status: completed
  - id: p3-fullstack-reversal
    content: "P3: ReversePayment + CBS ReverseRecharge + audit + fraud velocity (VAL-12-04) + BO عكس"
    status: completed
  - id: p4-fullstack-kpis
    content: "P4: GetPaymentServicesKpis + BO panel + tests + RechargeCustomer360Line → orchestrator"
    status: completed
isProject: false
---

# Payment Services (Blueprint §12 + §70)

**الحالة:** ✅ **مكتمل ديمو** (~82%) — الخطة كانت `pending` بينما الكود منفّذ؛ تمت مزامنة التوثيق والاختبارات.

## المعمارية

| طبقة | مسار |
|------|------|
| Ledger | `TelecomPaymentTransaction`, `TelecomPaymentAuditLog` |
| Orchestrator | `PaymentServicesOrchestrator` — Draft → Gateway → CBS Recharge → balance |
| VAL-12 | `PaymentServicesEligibilityChecker` + `PaymentServicesFraudTicketService` |
| Reversal | `PaymentServicesReversalService` + `ReversePaymentTransaction` |
| SMS | `PaymentServicesNotificationHandler` |
| Legacy bridge | `RechargeCustomer360Line` يستدعي orchestrator (Create + Confirm) |

## API (`TelecomController`)

- `POST ValidateVoucher`
- `POST CreatePaymentTransaction` / `ConfirmPaymentTransaction`
- `POST ReversePaymentTransaction` (Management / BackOffice / Admin)
- `GET GetPaymentTransactionList` / `Detail` / `GetPaymentServicesKpis`

## UI

- **Customer 360** + **Customer List:** `executeListPaymentFlow` — محفظة أو قسيمة
- **Telecom Hub:** نفس مسار Create/Confirm
- **Back Office:** `#boPaymentServicesPanel` — KPIs + جدول + عكس

## SQL يدوي

- `Infrastructure/.../Migrations/TelecomPaymentServices_Manual.sql`
- `TelecomPaymentServices_P3_Manual.sql`
- `TelecomPaymentRechargePermission_Manual.sql` (صلاحية `telecom.line.recharge`)

## اختبارات

- `PaymentServicesEligibilityTests.cs`
- `GetPaymentServicesKpisHandlerTests.cs`

## متبقّي (~18%)

- بوابة دفع HTTP إنتاجية (اليوم `PaymentGatewayMockIntegration`)
- فواتير / disputes / settlement batch
- Playwright E2E

## تحقق يدوي

راجع `docs/TELECOM_E2E_SMOKE_AR.md` — قسم Payment Services.
