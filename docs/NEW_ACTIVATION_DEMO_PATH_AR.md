# مسار الديمو: تفعيل خط جديد (NewActivation / Selling Line)

## الواجهات المتاحة (3 surfaces متطابقة)

| الشاشة | المسار | ملاحظة |
|--------|--------|--------|
| Customer 360 | `/Telecom/Customer360Profile?customerId={id}` | معالج كامل: قناة + دفع + Confirm + polling |
| قائمة العملاء | `/Customers/CustomerList` | modal سريع — نفس تسلسل API مع دفع إلزامي للباقات المدفوعة |
| Telecom Hub | `/Telecom/TelecomHub` | معالج POS — Confirm داخل المعالج (Showroom مسموح) |

## سلسلة الـ API (بالترتيب)

1. `POST /Telecom/ReserveMsisdnForCustomer` — `{ msisdnAssetId, customerId, reservedByUserId }`
2. `POST /Telecom/CreateTelecomOperation` — `{ kind: 0, subscriberProfileId, msisdnAssetId, productOfferingId, simIccid, activationChannel, dealerCode?, notes }`
3. `POST /Telecom/UploadTelecomOperationDocument` — `{ id, updatedById }`
4. `POST /Telecom/RecordSellingLinePayment` — **إلزامي** إذا `Product.UnitPrice > 0`
5. `POST /Telecom/FetchCashierPayment` — جلب مبلغ الإيصال من كاشير سيريتل (Sandbox) — `{ paymentReference, operationId?, expectedAmount? }` — إيصالات `REC-*`
6. `POST /Telecom/ConfirmTelecomOperation` — `{ id, updatedById, overrideReasonCode? }`
7. `GET /Telecom/GetTelecomOperationDetail?id={operationId}` — polling

## الأدوار (RBAC)

| الإجراء | الأدوار |
|---------|---------|
| Create / Reserve / Payment | `TelecomShowroom`, `TelecomBackOffice`, `TelecomAdmin` |
| Confirm | `TelecomShowroom`, `TelecomBackOffice`, `TelecomAdmin` (+ صلاحية `telecom.line.activate`) |
| Override KYC | `TelecomBackOffice`, `TelecomAdmin` فقط (حقل `overrideReasonCode` في Confirm) |

## ما يحدث في الباك إند عند Confirm

`ConfirmTelecomOperationRequest` → `ISellingLineEligibilityChecker` (VAL-02) → `ITelecomActivationWorkflow.ConfirmActivationAsync`:

1. **DB:** ربط MSISDN + SIM + اشتراك (نوع الخط من `IntendedSubscriptionTypeId` على الرقم)
2. **Provisioning حسب النوع:**
   - **Prepaid:** `IIntelligentNetworkService` (Provision + Credit إيداع) — **تجاوز CBS**
   - **Postpaid / Hybrid:** `IBillingSystemIntegration.ProvisionAsync` (CBS)
3. **HLR:** `TelecomOperationProvisionedNotification` → handler غير متزامن
4. **Fallout:** فشل IN/CBS/HLR → `Failed` + تعويض Saga + تذكرة

راجع أيضاً: [DEMO_SCRIPT_IN_CBS_AR.md](./DEMO_SCRIPT_IN_CBS_AR.md)

## قواعد VAL-02

| ID | القاعدة |
|----|---------|
| VAL-02-01 | KYC أو override معتمد |
| VAL-02-02 | MSISDN Available/Reserved |
| VAL-02-03 | SIM Available |
| VAL-02-04 | توافق الباقة مع نوع الخط (من المخزن / اختيار الويزارد) |
| VAL-02-06 | تطابق نوع الخط المختار مع `IntendedSubscriptionTypeId` على MSISDN |
| VAL-02-05 | `AmountPaid >= requiredDeposit` عند تسجيل الدفع |

## KPIs (Back Office)

`GET /Telecom/GetSellingLineActivationKpis` — لوحة `#boSellingLineKpiPanel` في BackOffice Dashboard.

## مسارات الديمو الموصى بها للجنة

1. **E1 Happy POS:** Hub → باقة مدفوعة → `REC-2026-ACT` + **جلب من الكاشier** → Record → Confirm → Completed
2. **E3 Payment gate:** List → تفعيل بدون payment → رفض `VAL-02-05`
3. **E5 Fallout:** Mock HLR fail → banner + رابط تذكرة TT
