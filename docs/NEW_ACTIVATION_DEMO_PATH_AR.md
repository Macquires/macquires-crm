# مسار الديمو: تفعيل خط جديد (NewActivation)

## الواجهات المتاحة

| الشاشة | المسار | ملاحظة |
|--------|--------|--------|
| Customer 360 | `/Telecom/Customer360Profile?customerId={id}` | معالج **تفعيل خط جديد** — يشمل Confirm داخل المعالج |
| قائمة العملاء | `/Customers/CustomerList` | زر **تفعيل خط جديد** — مسار سريع create → document → confirm |
| Telecom Hub | `/Telecom/TelecomHub` | معالج التفعيل + تأكيد من جدول العمليات |

## سلسلة الـ API (بالترتيب)

1. `POST /Telecom/ReserveMsisdnForCustomer` — `{ msisdnAssetId, customerId, reservedByUserId }`
2. `POST /Telecom/CreateTelecomOperation` — `{ kind: 0, subscriberProfileId, msisdnAssetId, productOfferingId, simIccid, notes }`
3. `POST /Telecom/UploadTelecomOperationDocument` — `{ id, updatedById }`
4. `POST /Telecom/ConfirmTelecomOperation` — `{ id, updatedById }`

## ما يحدث في الباك إند عند Confirm

`ConfirmTelecomOperationRequest` → `ITelecomActivationWorkflow.ConfirmActivationAsync`:

1. **DB:** ربط MSISDN + SIM (ICCID) + اشتراك (`SubscriptionBindingExecutor`)
2. **CBS:** `IBillingSystemIntegration.ProvisionAsync` (Huawei CBS — Mock/Demo مع سجلات)
3. **HLR:** `TelecomOperationProvisionedNotification` → `TelecomOperationHlrHandler` → `INetworkProvisioningService.ProvisionAsync`

## إثبات التنفيذ أثناء الديمو

| ماذا تُظهر | API / مكان |
|------------|------------|
| حالة العملية | `GET /Telecom/GetTelecomOperationDetail?id={operationId}` |
| سجل محاولات CBS | جدول `BillingIntegrationLog` أو شاشة Billing Integration |
| سجل HLR | `TelecomIntegrationLog` (نظام Huawei_HLR) |
| عملية `PendingExternal` | حالة العملية = مزامنة خارجية — تُعاد عند تفعيل التكاملات من الإعدادات العامة |

## رسائل للجنة

- الواجهة **Trigger**؛ المحرك **`TelecomActivationWorkflow`**.
- فشل CBS بعد الربط المحلي → تعويض محلي + `ReverseProvisionAsync`.
- فشل HLR الصريح بعد نجاح CBS → `ReverseProvisionAsync` + تعويض الربط المحلي (Ghost Profile mitigation).
- انقطاع مؤقت → `PendingExternal` دون عكس CBS.

## مسار بديل (API فقط)

`POST /TelecomBackOffice/ExecuteTechnicalAction` — `actionType: LINEACTIVATION` (خطوة واحدة على السيرفر؛ غير مربوط بالـ FrontEnd حالياً).
