# تقرير المطابقة السيادي الكامل
## Blueprint Coverage & Gap Analysis — Macquires CRM vs Syriatel MoM & Executive Design Edition

**الإصدار:** 1.0  
**التاريخ:** 2026-05-20  
**المُعد:** فريق Macquires — Enterprise Architecture  
**الجمهور:** لجنة سيريتل (حسام رضوان، عمار، أحمد الشوا، PMO، التشغيل، الهندسة)  
**المستودع:** `macquires-crm`  
**مراجع:** [`Telecom_CRM_Macquires_Executive_Design_Edition.docx`](../Telecom_CRM_Macquires_Executive_Design_Edition.docx) · [`bss_gap_analysis_roadmap.md`](../bss_gap_analysis_roadmap.md)

---

## منهجية القياس

| المستوى | التعريف | نطاق النسبة |
|---------|---------|-------------|
| **مكتمل** | Domain + Application + UI + مسار تشغيلي end-to-end | 85–100% |
| **جزئي قوي** | مسار أساسي يعمل؛ تكاملات Mock أو فجوات تشغيلية | 55–84% |
| **جزئي ضعيف** | كيانات/enums/دوال domain فقط دون orchestration | 25–54% |
| **حد أدنى** | seed/demo/mention | 5–24% |
| **غائب** | غير موجود في الكود | 0–4% |

**قاعدة الشفافية:** النسب تُقاس على **عمق التنفيذ الفعلي** وليس على وجود اسم مشابه في الوثائق. التكاملات CBS/HLR/SMS/Charging مُعلَنة صراحةً كـ **Demo/Mock** ما لم يُذكر خلاف ذلك.

---

# القسم A — الملخص التنفيذي والرؤية السيادية

## A.1 الخلاصة الرقمية

| المقياس | النسبة | المعنى للجنة |
|---------|--------|--------------|
| **المطابقة الإجمالية المرجّحة (BSS إنتاجي)** | **~43%** | نصف scope الـ Blueprint التشغيلي مغطّى بعمق حقيقي |
| **16 موديول MoM سيريتل (متوسط)** | **~48%** | متطلبات محضر الاجتماع مغطاة جزئياً إلى قوياً |
| **Part 3 — 22 نطاق Blueprint** | **~49%** | الوظائف التفصيلية في مستند Macquires Executive |
| **النضج المعماري (Architectural Maturity)** | **~85%** | Clean Architecture, CQRS, RBAC, Audit — جاهز للتوسع |
| **جاهزية POC/Demo** | **~70%** | Customer 360, Hub, تفعيل خط, Mock CBS/HLR قابل للعرض |
| **تكاملات إنتاج (CBS/HLR/Payment HTTP)** | **~35%** | العقود والأوركسترا موجودة؛ الموصلات الحقيقية لم تُربط بعد |

## A.2 ثلاثة محاور يجب فصلها في العرض

### المحور الأول: النضج المعماري (~85%)

المشروع **ليس CRUD عادي**. البنية تتبع:

- **Clean Architecture:** Domain → Application → Infrastructure → Presentation
- **CQRS عبر MediatR** مع FluentValidation وسلوكيات Authorization
- **RBAC متعدد الشخصيات:** Showroom, BackOffice, CallCenter, Management, SysAdmin
- **Audit متعدد الطبقات:** UserAudit, OperationAudit, Integration logs
- **Orchestration Engine:** `TelecomActivationWorkflow` — DB bind → CBS → HLR (async)

هذا الأساس يسمح بإضافة الموديولات الـ 16 المتبقية **دون هدم** ما بُني، وفق نفس نمط `TelecomOperationKind` + workflow.

### المحور الثاني: التغطية الوظيفية (~48% لـ MoM)

مسارات **البيع والتفعيل ونقل الملكية وتبديل الشريحة والـ 360** موجودة ومربوطة بالواجهة.  
مسارات **الإيقاف، إعادة التفعيل، الإنهاء، الدفع الكامل، الأجهزة، التحصيل، الالتزامات** غائبة أو على مستوى domain methods فقط.

### المحور الثالث: نجاح مرحلة POC

للجنة: **~43% BSS إجمالي ليس فشلاً** — بل يعني أن **المحور الاستراتيجي صحيح** (Customer 360 → عمليات → CBS/HLR) وأن **الفجوات محددة وقابلة للإغلاق** على نفس الكود. مقارنةً بمشاريع greenfield بدون orchestration، Macquires CRM **متقدم في الطبقة الحرجة** (تفعيل الخط + تعويض Ghost Profile).

## A.3 رسالة موجزة للإدارة العليا

> «المنصة الحالية هي **نواة BSS/CRM واعية بالاتصالات** وليست ERP محوّلاً. نسبة ~85% في المعمارية تعني قدرة على التوسع الوطني؛ نسبة ~48% في موديولات MoM تعني خارطة طريق واضحة للسبرنتات القادمة — وليس إعادة بناء من الصفر.»

---

# القسم B — التفكيك التفصيلي لـ 16 موديول MoM سيريتل

---

## B.1 — بيع خط جديد (Selling Line / New Activation)

**نسبة التغطية: 72%** | **الحالة: جزئي قوي**

### 1) الوضع الحالي بالكود

| Artifact | المسار |
|----------|--------|
| Orchestrator | `Core/Application/Common/Telecom/TelecomActivationWorkflow.cs` |
| Triple bind | `SubscriptionBindingExecutor.cs`, `SubscriptionBindingService.cs` |
| Command | `CreateTelecomOperationRequest` (`Kind = NewActivation`) |
| Confirm | `ConfirmTelecomOperationRequest` → workflow |
| UI | `Customer360Profile.cshtml(.js)`, `TelecomHub.cshtml(.js)`, `CustomerList.cshtml.js` |
| Demo path | `docs/NEW_ACTIVATION_DEMO_PATH_AR.md` |

**المراحل المُنفَّذة:** حجز MSISDN → إنشاء عملية ACT- → رفع وثيقة → Confirm → ربط DB → CBS → HLR (MediatR) → SMS ترحيب.

**التحققات:** ICCID (19–20 رقم + Luhn), حد خطوط فردية (`Telecom.MaxActiveLinesPerIndividual`), توافق الباقة مع نوع الخط.

### 2) الربط الفني CBS / HLR

```
[UI Confirm] → TelecomActivationWorkflow
    → Phase A: DB transaction (reserve + triple-bind) — متزامن
    → Phase B: IBillingSystemIntegration.ProvisionAsync — متزامن (Mock CBS)
    → Phase C: MediatR → TelecomOperationHlrHandler — غير متزامن (Mock HLR)
    → Phase D: TelecomOperationSmsHandler — welcome SMS
```

- **CorrelationId** على كل عملية (GUID v7)
- **Idempotency CBS:** replay آمن عبر `BillingIntegrationLog`
- **Ghost Profile mitigation:** `ITelecomHlrFailureCompensator` عند فشل HLR الصريح

### 3) الفجوات التشغيلية

- CBS/HLR **Mock** — لا HTTP Huawei/Ericsson إنتاجي
- لا مسار **Dealer Agent** منفصل (persona Showroom فقط)
- الدفع عند التفعيل (initial deposit) يُشتق من `Product.UnitPrice` — لا payment gateway
- IMSI يُعرض من المخزون (`PairedImsi`) — لا capture يدوي إن فُقد من المخزون

### 4) خارطة الطريق

| الأولوية | البند |
|----------|-------|
| **Quick Win** | ربط payment mock في خطوة Confirm؛ عرض حالة `PendingExternal` polling (موجود جزئياً في 360) |
| **Medium** | HTTP adapters لـ CBS/HLR حسب `IntegrationTarget` |
| **Long-Term** | Dealer quota + OTP regulatory + production chaos testing |

---

## B.2 — خدمات الدفع وتسوية الفواتير (Payment Services)

**نسبة التغطية: 32%** | **الحالة: جزئي ضعيف**

### 1) الوضع الحالي بالكود

- `RechargeCustomer360Line.cs` — شحن من Customer 360
- `HuaweiCbsBillingIntegration.cs` — `GetOutstandingBalanceAsync` (mock)
- `HuaweiCbsMockController.cs` — QueryBalance/Recharge demo API
- `Customer360WalletBuilder.cs` — محفظة demo
- `BillingIntegrationLog` — سجل محاولات فقط

### 2) الربط الفني CBS / HLR

- **Recharge:** لمس CBS mock؛ **لا HLR**
- **Balance inquiry:** mock deterministic (MSISDN ينتهي بـ 9 = دين)
- **لا Payment Gateway** ولا OCS charging حقيقي

### 3) الفجوات

- لا فواتير (invoice entity)، لا disputes، لا settlement batch
- لا voucher validation
- لا convergent billing (جهاز + خط)

### 4) خارطة الطريق

| الأولوية | البند |
|----------|-------|
| **Quick Win** | توحيد recharge عبر `IBillingSystemIntegration` مع audit موحّد |
| **Medium** | Payment gateway abstraction + invoice inquiry DTO |
| **Long-Term** | BillingAccount entity + transaction ledger |

---

## B.3 — تغيير نوع الخط (Change GSM Type)

**نسبة التغطية: 40%** | **الحالة: جزئي ضعيف**

### 1) الوضع الحالي

- `TelecomOperationKind.Migration` (MGR-) — ترحيل **باقة/عرض** وليس Prepaid↔Postpaid كعملية مستقلة
- UI: wizard migrate في Customer360 و TelecomHub
- `GetMigrationEligibleProducts.cs` — منتجات مؤهلة للترحيل

### 2) CBS / HLR

- Migration يمر بنفس orchestration: CBS provision → HLR `Migration_OfferChange`
- **لا** أمر CBS منفصل لتغيير GSM billing profile type

### 3) الفجوات

- لا `ChangeGsmType` operation kind
- لا decision matrix للتحويل Prepaid→Postpaid (التزامات، deposit)
- Blueprint يصف GSM technology change — الكود يخلطها مع package migration

### 4) Roadmap

| **Quick Win** | إضافة `TelecomOperationKind.ChangeGsmType` أو توسيع Migration metadata |
| **Medium** | CBS profile type switch + HLR subscriber profile update |
| **Long-Term** | Rules engine للتحويل التنظيمي |

---

## B.4 — نقل الملكية (Transfer of Ownership / TakeOver)

**نسبة التغطية: ~92%** | **الحالة: جاهز للديمو التشغيلي**

### 1) الوضع الحالي

- `TelecomOperationKind.TakeOver` (TKO-) + حقول سيادة (`TransferReason`, `DepositTransferPolicy`, `PriorSubscriberProfileId`, …)
- `ITakeOverEligibilityChecker` — VAL-07-01..05 (وثيقة، ذمم، قائمة سوداء، طلب مفتوح)
- فصل صلاحيات: `telecom.line.transfer_request` (إنشاء/معرض) و `telecom.line.transfer_ownership` (اعتماد BO)
- UI: Customer 360 / Hub / قائمة المشتركين — مسودة + هوية → `PendingDocuments` بدون Confirm من المعرض
- `TakeOverCompletionService` — SMS + `FieldChangesJson` (صندوق أسود)
- `GetTakeOverOwnershipKpis` + كرت Back Office
- HLR فشل → `VAL-07-04` rollback محلي + عكس CBS (`CbsTransferOwnership`)

### 2) CBS / HLR

- CBS `CbsTransferOwnership` + HLR `TakeOver_OwnershipTransfer`
- Re-validate عند Confirm + polling Hub (2s)

### 3) الفجوات المتبقية

- لا OTP للمالك القديم/الجديد (مؤجل بالخطة)
- لا regulatory cap على عدد عمليات النقل
- Obligation matrix كامل (B.5) — snapshot `TakeOverObligationStatus` فقط

### 4) Roadmap

| **Long-Term** | OTP + DMS للوثائق الموثقة |
| **Medium** | Obligation entity + CBS contract sync (B.5) |

---

## B.5 — الالتزامات الماليّة والتعاقدية (Obligation)

**نسبة التغطية: 15%** | **الحالة: حد أدنى**

### 1) الوضع الحالي

- **لا entity** `Obligation`, `Contract`, `InstallmentPlan`
- `PostpaidCreditLimit` على `SubscriberProfile` — حقل static
- debt check في TakeOver فقط

### 2) CBS / HLR

- **لا handshake** مخصص للالتزامات

### 3) الفجوات

- Blueprint يصف device financing obligation matrix — **غائب بالكامل**
- لا ربط contract → CBS account profile

### 4) Roadmap

| **Medium** | Obligation snapshot DTO على TakeOver/Postpaid activation |
| **Long-Term** | Contract entity + CBS contract ID sync |

---

## B.6 — تبديل شريحة (Change SIM / SimSwap)

**نسبة التغطية: ~90%** | **الحالة: جاهز للديمو التشغيلي** (S1–S4 مُنفَّذة 2026-06)

### 1) الوضع الحالي (منفَّذ)

- `TelecomOperationKind.SimSwap` (SIM-) + حقول سيادة: `ReplacementReason`, `IsLostOrStolenReport`, `PriorSimInventoryId`
- SQL: `TelecomSimSwapFields_Manual.sql` + فهرس `IX_TelecomOperationRequest_SimSwap`
- `SimSwapEligibilityChecker` — VAL-04-01..03 (ICCID/Luhn، blacklist، ذمم، طلب مفتوح، وثيقة BO)
- `ApplySimSwapAsync` — quarantine للشريحة السابقة + تفعيل الجديدة
- `CbsSimProfileUpdate` + `HlrSimProfileUpdate` + `TelecomHlrFailureCompensator` — VAL-04-ROLLBACK
- `SimSwapCompletionService` — `FieldChangesJson` + SMS بعد HLR
- صلاحيات: `telecom.line.simswap_request` (معرض) · `telecom.line.simswap_approve` (BO) · `telecom.line.simswap` (Standard confirm)
- FE: Customer 360 / Hub / Customer List — سبب التبديل + مسار مفقودة/مسروقة بدون Confirm فوري من المعرض
- Hub: اعتماد BO + polling 2s · badges TKO/SIM
- `GetSimSwapKpis` + كرت Back Office Dashboard

### 2) CBS / HLR

- Handshake مزدوج CBS ثم HLR؛ تعويض HLR يعيد الشريحة القديمة ويحرّر الجديدة للمخزن

### 3) الفجوات المتبقية (مؤجّلة عن الديمو)

- OTP خارجي لمسار الضياع (قرار: BO approval بدون OTP)
- eSIM / DP+ كامل
- Fraud hold مخصّص على Confirm (جزء منه مغطّى بـ VAL-04)

### 4) Roadmap

| **مؤجّل** | OTP + eSIM lifecycle كامل |
| **Medium** | Fraud hold integration موسّع على Confirm |

---

## B.7 — تغيير رقم الهاتف (Change Number / CNR)

**نسبة التغطية: ~85%** | **الحالة: مكتمل للديمو الداخلي (بدون MNP خارجي)**

### 1) الوضع الحالي

- `TelecomOperationKind.NumberPortability` — ترقيم **CNR-**، مسار داخلي (ليس Port-In/Out)
- `ChangeNumberEligibilityChecker` (VAL-05)، `ChangeNumberCompletionService`، `TelecomMsisdnChangeLog`
- CBS `CbsMsisdnReassign` + HLR `HlrMsisdnUpdate` + تعويض VAL-05-ROLLBACK
- أرقام مميزة (Silver/Gold/Platinum) → `ApprovalLevelRequired = BackOffice` + وثيقة دفع
- UI: **Telecom Hub** (tile + wizard)، **Customer 360**، **Customer List**، **Back Office KPIs** (`GetChangeNumberKpis`)
- صلاحيات: `telecom.line.change_number_request` / `_approve` / `change_number`

### 2) CBS / HLR

- تفعيل كامل عبر `TelecomActivationWorkflow.ApplyChangeNumberAsync` (ديمو)
- Directory/SMS mock عند الإكمال

### 3) الفجوات

- **MNP** (نقل مشغّل / Port-In/Out) — مؤجّل
- تسعير مميز تلقائي من كتالوج (حالياً إدخال يدوي + BO)

### 4) Roadmap

| **Medium** | MNP wizard + port-in/out states |
| **Long-Term** | Regulatory MNP gateway integration |

---

## B.8 — إنهاء وإلغاء الخط (Termination)

**نسبة التغطية: ~75%** | **الحالة: تشغيلي (ديمو داخلي)**

### 1) الوضع الحالي

- `TelecomOperationKind.Termination` (7) + ترقيم **TRM-**
- `TerminationEligibilityChecker` (VAL-10: طوعي + retention، ديون، عمليات مفتوحة، BO لـ Fraud/Regulatory/Collections)
- `ApplyTerminationAsync` + `RevertTerminationAsync` + `TerminationCompletionService`
- CBS: `CbsGenerateFinalBill` (ديمو) | HLR: `HlrDeactivateSubscriber`
- UI: **Telecom Hub**، **Customer 360**، **Customer List**، **Back Office KPIs** (`GetTerminationKpis`)

### 2) الفجوات المتبقية

- لا dunning/MNP؛ deposit refund chain محدود (ديمو)
- اختبارات integration workflow كاملة (مثل CNR end-to-end)

### 3) Roadmap

| **Medium** | اختبارات workflow + deposit refund حقيقي |
| **Long** | فترة حجز تنظيمي + involuntary automation |

---

## B.9 — الحظر المؤقت (Temporary Suspension / Barring)

**نسبة التغطية: 25%** | **الحالة: جزئي ضعيف**

### 1) الوضع الحالي

- `SubscriberProfile.Suspend()`, `SuspendInbound()`, `SuspendOutbound()`
- `MsisdnPoolStatus.Suspended`, `SimStatus.Suspended`
- `ResyncSubscriberFromHlr` قد يستدعي Suspend
- **لا** TelecomOperation للحظر من UI

### 2) CBS / HLR

- **لا** orchestrated barring — لا أمر HLR suspend subscriber

### 3) الفجوات

- Blueprint: fraud/billing/customer-request suspension — **لا مسارات منفصلة**
- لا Grace Period → Barred state machine على العملية

### 4) Roadmap

| **Quick Win** | `Suspension` operation + HLR + CBS bar |
| **Medium** | Inbound-only vs full bar UI |

---

## B.10 — تفعيل العروض وباقات VAS (Services & Subscription)

**نسبة التغطية: ~78%** | **الحالة: جزئي قوي**

### 1) الوضع الحالي

- `ProductOffering` + `ProductOfferingComponent` + `PricePlan`
- `TelecomSubscription.ProductOfferingId` + `OfferSubscriptionEligibilityChecker` (VAL-11)
- `Migration` (MGR-) + `CbsChangePrimaryOffer` + `MigrationCompletionService` + compensator rollback
- `ToggleSubscriberVasService.cs` → `ServiceModification` + VAL-11 + audit
- `GetOfferSubscriptionKpis` + Back Office panel
- UI: VasCatalogList, Customer360/Hub migrate + addpackage, ProductCatalog

### 2) CBS / HLR

- VAS toggle → CBS touch + HLR VAS mock
- Roaming كـ VAS seed (`VAS_ROAMING_INT`) — **ليس roaming provisioning flow**

### 3) الفجوات

- لا campaign-driven offer activation
- Roaming eligibility matrix غائب

### 4) Roadmap

| **Quick Win** | Offer activation audit trail |
| **Medium** | Campaign entity + eligibility rules |

---

## B.11 — بيع الأجهزة والراوترات (Selling Devices)

**نسبة التغطية: 5%** | **الحالة: غائب**

### 1) الوضع الحالي

- **لا** Device entity, installment plan, stock للأجهزة
- `Product.Physical` flag على منتج ERP legacy — **لا retail device flow**
- Blueprint Device Financing matrix — **غائب**

### 2) CBS / HLR

- **لا**

### 3) الفجوات

- كامل الموديول

### 4) Roadmap

| **Long-Term** | Device catalog + installment + CBS device charge + CRM order line |

---

## B.12 — استرداد التأمينات وكاش سيريتل (Refund & Deposit)

**نسبة التغطية: 15%** | **الحالة: حد أدنى**

### 1) الوضع الحالي

- `Customer360WalletBuilder` — عرض demo
- `ReverseProvisionAsync` — **compensation** وليس refund عميل
- `PrepaidBalance` على SubscriberProfile — static

### 2) CBS / HLR

- Reverse على CBS mock عند فشل HLR — **ليس refund user-initiated**

### 3) الفجوات

- لا refund approval workflow
- لا Syriatel Cash wallet integration

### 4) Roadmap

| **Medium** | Refund operation kind + CBS credit note |
| **Long-Term** | Wallet ledger entity |

---

## B.13 — إعادة تفعيل الخط المحظور (Reconnect / Reactivation)

**نسبة التغطية: 75%** | **الحالة: مكتمل تشغيلياً (Full-Stack §9)**

### 1) الوضع الحالي (Evidence)

| مسار | دليل |
|------|------|
| Matrix + VAL-09 | `ReconnectEligibilityMatrix.cs`, `ReconnectEligibilityChecker.cs` |
| API Pre-check | `GET /Telecom/GetReconnectEligibility` |
| Create → BO Queue | `PendingDocuments` عند `RequiresBackOfficeApproval` في `CreateTelecomOperationRequest` |
| Confirm + Fraud Audit | `FraudClearanceConfirmed` في `TelecomActivationWorkflow` |
| CBS/HLR | `ApplyReconnectAsync`, `CbsUnbarSubscriber`, compensator |
| Hub + BO Modal | `TelecomHub.cshtml(.js)`, `canApproveSecureOp` لـ kind 8/9 |
| Customer 360 | كرت RCN في `Customer360Profile` + deep-link للـ Hub |
| Tests | `ReconnectEligibilityIntegrationTests.cs` |
| Demo Seed | `EnsureHeroReconnectDemoAsync` (0939000091 Fraud، 0939000002 Billing) |

### 2) CBS / HLR

- CBS unbar + HLR reactivate عبر orchestrator (mock/production adapters)

### 3) متبقّي (~25%)

- E2E Playwright لمسار الـ cinematic walkthrough
- Production HTTP adapters (خارج نطاق الديمو)

### 4) Roadmap

| **مكتمل** | `Reconnect` kind 9 + Matrix + BO queue + 360 |
| **لاحقاً** | E2E automation + live CBS/HLR contracts |

---

## B.14 — تحصيل الديون المعدومة (Bad Debt Recovery)

**نسبة التغطية: 10%** | **الحالة: حد أدنى**

### 1) الوضع الحالي

- `GetOutstandingBalanceAsync` mock
- TakeOver blocks negative balance
- **لا** dunning stages, collection cases, write-off

### 2) CBS / HLR

- **لا** collection API

### 3) الفجوات

- Blueprint Collections/Dunning domain — **غائب**

### 4) Roadmap

| **Long-Term** | Dunning engine + collection agency handoff + HLR bar on default |

---

## B.15 — تحديث بيانات العميل (Update Customer Info / KYC)

**نسبة التغطية: 70%** | **الحالة: جزئي قوي**

### 1) الوضع الحالي

- `CreateCustomer`, `UpdateCustomer`, `DeleteCustomer`
- `IndividualCustomer` / `CorporateCustomer` TPH
- `CustomerIdentityDocument`, `RevealCustomerNationalId` (encrypted PII)
- `FindCustomerCandidates` — duplicate search partial
- `Customer.Blacklist()` / `ClearBlacklist()`
- UI: CustomerList, Customer360 profile tab

### 2) CBS / HLR

- Directory sync mock on MSISDN change — **لا** on profile update
- **لا** KYC verification workflow (Pending KYC states)

### 3) الفجوات

- لا Consent management entity
- لا Timeline Engine للتفاعلات
- Duplicate management partial

### 4) Roadmap

| **Quick Win** | Consent flag on Customer + audit |
| **Medium** | KYC status state machine on Customer |
| **Long-Term** | DMS integration for document verification |

---

## B.16 — التقارير والتحليلات (Reports & Strategic Analytics)

**نسبة التغطية: 50%** | **الحالة: جزئي قوي**

### 1) الوضع الحالي

- `GetStrategicMetrics.cs` — مقاييس استراتيجية + OrgUnit filter
- `GetTelecomDashboardKpis.cs` — ARPU demo + churn hardcoded
- Dashboard widgets (8 providers): `Infrastructure/Infrastructure/Dashboard/DI.cs`
- UI: `StrategicAnalytics.cshtml`, `TelecomMisReports.cshtml`, `DefaultDashboard.cshtml`
- `StrategicReportViewed` audit

### 2) CBS / HLR

- Integration health widget — **لا** billing reconciliation reports

### 3) الفجوات

- Churn/ARPU ليست من CDR/billing حقيقي
- لا MIS كامل كما في Blueprint Command Center
- SLA reporting غائب

### 4) Roadmap

| **Quick Win** | Wire KPIs to real operation counts (already partially done) |
| **Medium** | CBS reconciliation export |
| **Long-Term** | CDR pipeline + ML churn |

---

## B.17 — ملخص矩阵 16 موديول

| # | الموديول | % | الحالة | Roadmap dominant |
|---|----------|---|--------|------------------|
| 1 | بيع خط جديد | 72% | جزئي قوي | Medium (HTTP CBS/HLR) |
| 2 | الدفع والفواتير | 32% | جزئي ضعيف | Long |
| 3 | تغيير نوع الخط | 40% | جزئي ضعيف | Medium |
| 4 | نقل الملكية | 68% | جزئي قوي | Medium |
| 5 | الالتزامات | 15% | حد أدنى | Long |
| 6 | تبديل شريحة | ~90% | جاهز ديمو | OTP/eSIM (Long) |
| 7 | تغيير الرقم | 25% | جزئي ضعيف | Long |
| 8 | إنهاء الخط | 20% | جزئي ضعيف | Quick |
| 9 | الحظر المؤقت | 25% | جزئي ضعيف | Quick |
| 10 | VAS والعروض | 60% | جزئي قوي | Medium |
| 11 | بيع الأجهزة | 5% | غائب | Long |
| 12 | استرداد التأمينات | 15% | حد أدنى | Long |
| 13 | إعادة التفعيل | 75% | مكتمل §9 Full-Stack | Evidence |
| 14 | الديون المعدومة | 10% | حد أدنى | Long |
| 15 | تحديث بيانات العميل | 70% | جزئي قوي | Medium |
| 16 | التقارير | 50% | جزئي قوي | Medium |

**المتوسط الحسابي: ~48%**

---

# القسم C — Ghost Profile ودرع التزامن الموزّع (Saga)

## C.1 تعريف المخاطرة

**Ghost Profile (الخط الشبح):** حالة inconsistent حيث:

1. الربط المحلي (DB) **نجح**
2. **CBS** أنشأ/خصم حساباً مالياً **نجح**
3. **HLR** فشل في `CreateSubscriber` / network provision

**النتيجة:** العميل «مفعل مالياً» والأبراج «لا تراه» — أو العكس في سيناريوهات أخرى.

## C.2 المعمارية قبل وبعد التحسين

### قبل (فجوة Tier-1)

```
DB ✓ → CBS ✓ → HLR ✗  ⇒  Operation = Failed, CBS يبقى ✓  ⇒  Ghost Profile
```

### بعد (الوضع الحالي في الكود)

```
DB ✓ → CBS ✓ → HLR ✗ (hard fail)
    → ITelecomHlrFailureCompensator
        → ReverseProvisionAsync (CBS)
        → SubscriptionBindingCompensator (local unbind)
    → Operation = Failed + رسالة AR
```

**الملفات:**

| المكوّن | المسار |
|---------|--------|
| Compensator interface | `Core/Application/Common/Telecom/ITelecomHlrFailureCompensator.cs` |
| HLR handler | `Core/Application/Features/TelecomManager/Events/TelecomOperationProvisionedEventHandlers.cs` |
| CBS reverse | `Infrastructure/.../HuaweiCbsBillingIntegration.cs` → `CbsReverseAccount` |
| CBS fail after bind | `TelecomActivationWorkflow.cs` → compensator + reverse (existing) |

## C.3 PendingExternalSyncService — العامل الخلفي

**الدور:** إعادة محاولة العمليات في `TelecomOperationStatus.PendingExternal` عند عودة التكاملات.

**المسار:** `Infrastructure/Infrastructure/TelecomIntegrations/PendingExternalSyncService.cs`

**السلوك:**

1. Batch حتى 500 عملية
2. CBS `ProvisionAsync` — **idempotent** إذا success log موجود (`OperationId`)
3. HLR `ProvisionAsync`
4. عند فشل HLR صريح → compensator (بعد التحديث الأخير)

**تفعيل:** `UpdateGlobalSettings` عند re-enable Huawei/HLR integrations.

## C.4 ما يبقى لـ Tier-1 كامل

| البند | الحالة |
|-------|--------|
| Outbox pattern دائم | غائب — MediatR in-process |
| Auto technical ticket on compensation | اختياري — `ITechnicalTicketQueueIngestionService` موجود |
| E2E integration tests CBS+HLR fail | جزئي — unit tests على contracts |
| 2PC distributed transactions | **مُستبعد** by design — Saga فقط |

---

# القسم D — الاستخبارات الجغرافية وحوكمة الفروع (OrgUnit & Audit)

## D.1 نموذج OrgUnit

**الكيان:** `Core/Domain/Entities/OrgUnit.cs`  
**الأنواع:** `OrgUnitKind` — Headquarters, Region, Branch  
**الربط:** `Customer.OrgUnitId` — كل عميل مربوط بفرع  
**Seeder:** `StrategicMisDemoSeeder.cs` — توزيع عملاء على فروع دمشق/حلب/…

## D.2 StrategicDataScopeService

**المسار:** `Infrastructure/Infrastructure/Security/StrategicDataScopeService.cs`  
**Resolver:** `Core/Application/Common/Security/StrategicDataScopeResolver.cs`

**مستويات الوصول:**

| المستوى | السلوك |
|---------|--------|
| **GeneralManager** | يرى كل المناطق والفروع؛ يفلتر اختيارياً |
| **RegionalDirector** | مقفل على منطقته؛ branches ضمن المنطقة |
| **BranchManager** | مقفل على فرعه فقط |

**الاشتقاق:** من Role + `UserOrgUnitId` + `ManagedRegionId` عبر `TelecomEnterpriseRoleMatrix`.

## D.3 التقارير الاستراتيجية والتدقيق

**API:** `GET /Telecom/GetStrategicMetrics?regionId=&branchId=`  
**Handler:** `GetStrategicMetrics.cs` — يطبّق scope قبل aggregate  
**Audit:** عند كل عرض → `UserAuditActionTypes.StrategicReportViewed`  
**UI:** `StrategicAnalytics.cshtml` — permission-gated

**الضمان:** عزل بيانات إقليمي/فرعي + footprint طب شرعي لكل مشاهدة تقرير حساس.

---

# الملحق 1 — 22 نطاق Part 3 (Blueprint Executive Edition)

| # | النطاق | % |
|---|--------|---|
| 1 | Customer 360 & KYC | 75% |
| 2 | Selling Line & Activation | 72% |
| 3 | Order Management & Fulfillment | 35% |
| 4 | SIM/eSIM Lifecycle | 55% |
| 5 | MSISDN Lifecycle | 70% |
| 6 | Change GSM / Service Technology | 40% |
| 7 | Transfer of Ownership | 68% |
| 8 | Suspension & Barring | 25% |
| 9 | Reconnect / Reactivation | 75% |
| 10 | Service Termination | ~75% |
| 11 | Product Catalog & Subscription | ~78% |
| 12 | Recharge, Voucher & Payment | 35% |
| 13 | Billing Inquiry & Dispute | 25% |
| 14 | Device Sales & Installment | 5% |
| 15 | Refund, Deposit & Wallet | 15% |
| 16 | Collections, Dunning & Bad Debt | 10% |
| 17 | Complaint, Case & SLA | 45% |
| 18 | Dealer, Branch & Retail | 20% |
| 19 | Revenue Assurance & Fraud | 15% |
| 20 | Network-Aware CRM & Outage | 30% |
| 21 | Roaming & International | 15% |
| 22 | Reporting, KPI & Command Center | 50% |

**متوسط Part 3: ~49%**

---

# الملحق 2 — جدول التكاملات

| النظام | Interface | Implementation | % إنتاج |
|--------|-----------|----------------|---------|
| Huawei CBS | `IBillingSystemIntegration` | `HuaweiCbsBillingIntegration` | 45% |
| Core HLR | `INetworkProvisioningService` | `HlrNetworkProvisioningService` | 45% |
| HLR VAS | `IVasProvisioningService` | `HlrVasProvisioningService` | 40% |
| SMS | `ISmsGatewayIntegration` | `SmsGatewayMockIntegration` | 20% |
| Charging/OCS | `IChargingSystemIntegration` | `ChargingSystemMockIntegration` | 15% |
| eSIM SM-DP+ | `IESimDpPlusService` | `ESimDpPlusMockService` | 25% |
| Directory | `ITelecomDirectorySync` | `TelecomDirectoryMockSyncIntegration` | 20% |
| Payment Gateway | — | **Missing** | 0% |

**DI:** `Infrastructure/Infrastructure/DependencyInjection.cs` (lines 75–88)

---

# الملحق 3 — حوكمة Part 4 (Checklist)

| البند | % | Evidence |
|-------|---|----------|
| Audit trail | 75% | UserAudit, OperationAudit, Integration logs |
| RBAC / Personas | 82% | PermissionCatalog, TelecomPersonaResolver |
| Validations مركزية | 40% | FluentValidation per command |
| SLA / OLA | 8% | غائب |
| Fraud controls | 15% | Blacklist فقط |
| Regulatory (max SIMs) | 45% | GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual |
| Field encryption PII | 80% | AesGcmFieldEncryptionService |

---

# الملحق 4 — خارطة الطريق المجمّعة

## Quick Wins (1–2 sprint) — رفع ~5–10% إجمالي

1. `Suspension` + `Reconnect` + `Termination` كـ `TelecomOperationKind` جديدة
2. SLA fields على `TelecomTechnicalTicket` (DueAtUtc, Breached)
3. ~~Lost/Stolen → quarantine on SimSwap~~ ✅ (S1–S4)
4. Consent flag على Customer

## Medium (quarter) — رفع ~10–15%

1. HTTP CBS/HLR adapters (config-driven `IntegrationTarget`)
2. ChangeGsmType operation أو توسيع Migration
3. MNP wizard (basic)
4. Order header/lines فوق TelecomOperationRequest
5. Fraud hold flag on activation

## Long-Term (program)

1. Device sales + installment
2. Collections / Dunning / Bad debt
3. Dealer quota + reconciliation
4. Revenue Assurance + Fraud scoring engine
5. CDR + real ARPU/Churn
6. Self-care / omnichannel apps

---

# الخاتمة

| السؤال | الإجابة |
|--------|---------|
| هل الكود على خط Blueprint/MoM؟ | **نعم** — نفس المحور (360 → عمليات → CBS/HLR) |
| نسبة 16 MoM | **~48%** |
| Selling Line | **~72%** — الأقوى للديمو |
| المعمارية | **~85%** — نقطة قوة للجنة |
| BSS إنتاجي كامل | **~35–43%** — يحتاج HTTP integrations + lifecycle ops |
| Ghost Profile | **مُغلق جزئياً** — compensator + reverse CBS |

---

**Macquires | The Ultimate Solution**  
*مستند سيادي — للاستخدام الداخلي ولجنة سيريتل*
