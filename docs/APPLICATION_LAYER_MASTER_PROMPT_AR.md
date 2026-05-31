# Application Layer Master Prompt — BSS Orchestration & Resiliency

> يُنفَّذ بعد [DOMAIN_CORE_TELECOM_DEEP_DIVE_AR.md](./DOMAIN_CORE_TELECOM_DEEP_DIVE_AR.md).

## الهدف

محرك عمليات (Orchestration Engine) لـ Syr-Tel BSS باستخدام **MediatR** و **.NET 9**: تحويل Domain إلى رحلات مستخدم آمنة، متكاملة، وغير قابلة للتكرار بالخطأ.

---

## 1) خط الإنتاج: TelecomOperationRequest Pipeline

| الحالة | المعنى |
|--------|--------|
| `Draft` | مسودة |
| `PendingDocuments` | وثائق مرفوعة — بانتظار التأكيد |
| `Confirmed` | تأكيد محلي من الموظف |
| `Provisioning` | تفعيل CBS/HLR جارٍ |
| `PendingExternal` | تأجيل / إعادة محاولة خارجية |
| `Completed` / `Failed` | نهائي |

**التنفيذ:**

- `TelecomOperationLifecycle` — قواعد الانتقال
- `ITelecomOperationOrchestrator` — انتقال + `TelecomOperationAuditLog`
- `ITelecomActivationWorkflow` — رحلة التأكيد الكاملة
- `CorrelationId` (GUID v7) عند إنشاء الطلب

**المسارات:**

| Command | الدور |
|---------|--------|
| `CreateTelecomOperationRequest` | Draft + CorrelationId |
| `UploadTelecomOperationDocument` | → PendingDocuments |
| `ConfirmTelecomOperationRequest` | → Confirmed → Provisioning → Completed/Failed |

---

## 2) Idempotency & Resiliency

- **Idempotency:** إذا كان الطلب `Completed`، يُعاد: «الطلب معالج مسبقاً» دون إعادة CBS/HLR/Binding.
- **Polly:** `HuaweiCbsBillingIntegration` (Retry) + `HlrNetworkProvisioningService` (Retry + Circuit Breaker → `PendingExternal`).
- **Logging:** `BillingIntegrationLog` يحمل `CorrelationId`, `RequestPayload`, `ResponsePayload`.

---

## 3) المايسترو: ISubscriptionBindingExecutor

- التفعيل الثلاثي عبر `TelecomActivationWorkflow.ApplyLocalActivationAsync`
- فشل Domain → `Failed` + رسالة عربية (`BusinessRuleViolationException`)

---

## 4) عقود التكامل

| واجهة | الدور |
|--------|--------|
| `IBillingSystemIntegration` | Huawei CBS |
| `INetworkProvisioningService` | HLR / شبكة |
| `IChargingSystemIntegration` | شحن |
| `ISmsGatewayIntegration` | SMS |

---

## 5) الاستعلامات

| Query | الوصف |
|-------|--------|
| `GetCustomer360` | عميل + خطوط + آخر 10 عمليات |
| `TelecomUnifiedSearch` | NationalId (hash) / MSISDN / ICCID |
| `FindCustomerCandidates` | مرشحون CRM |

استخدم `AsNoTracking()` و projections خفيفة.

---

## ممنوعات

1. ربط SIM/MSISDN في Controller أو Razor
2. عرض NationalId دون مسار التشفير/التمويه
3. أي كيان ERP محذوف

---

## تعريف «تم»

- [x] Pipeline Draft → Completed مع Audit
- [x] Idempotency على إعادة التأكيد
- [x] CorrelationId في Integration Logs
- [x] Triple-Binding عبر Executor
- [ ] اختبارات Application integration (لاحقاً)
- [ ] صلاحيات فك التشفير per-role (لاحقاً)

---

## SQL يدوي

`Infrastructure/.../ApplicationLayer_Pipeline_Manual.sql`
