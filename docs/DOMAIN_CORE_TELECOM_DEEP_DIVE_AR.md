# Domain Core — Telecom Deep Dive (قلب النظام)

> مرجع تنفيذي بعد [DOMAIN_GATE_MASTER_PROMPT_AR.md](./DOMAIN_GATE_MASTER_PROMPT_AR.md) (Purge B + Identity A).

**قرارات معتمدة:**

| المحور | القرار |
|--------|--------|
| Purge / هوية | B + A (منفّذ) |
| أولوية | Triple-Binding + Lifecycle (MSISDN 24h + Quarantine 90d) |
| تشفير PII | Application Value Converter + Key Vault (Block 3) |

---

## 1) Triple-Binding (القلب النابض)

### العقد

`BindSubscription(Customer, Msisdn, Sim, ProductOffering)` عبر `ISubscriptionBindingService` في `Core/Domain/Services/`.

### قواعد صارمة

1. `MsisdnAsset.PoolStatus` = `Available`، أو `Reserved` لنفس العميل ضمن `ReservedUntilUtc`.
2. `SimInventory.Status` = `Available` وغير مربوط بخط نشط آخر.
3. حد أقصى خطوط للعميل (سياسة في Application: `ICustomerLineLimitPolicy`).
4. `CorrelationId` على `TelecomOperationRequest` يربط العملية بكل الأصول.

### Msisdn lifecycle

| الحالة | المعنى |
|--------|--------|
| Available | جاهز للبيع |
| Reserved | محجوز — `ReservedUntilUtc` (افتراضي 24 ساعة) |
| Active | مرتبط بمشترك |
| Suspended | موقوف مؤقتاً |
| Quarantined | 90 يوم بعد الفصل قبل إعادة التدوير |

### نتيجة النجاح

- `TelecomSubscription` جديد أو محدّث
- `MsisdnAsset` + `SimInventory` → `SubscriberProfileId`
- انتقال الحالتين إلى `Active`

---

## 2) SubscriberProfile (تشغيلي)

- `ServiceLineType`: Mobile, Broadband, FixedLine
- `LanguagePreference`: ar / en
- `OperationalStatus`: Pending, Active, SuspendedInbound, SuspendedOutbound, Terminated, Deactivated
- الخط الأساسي: `TelecomSubscription.IsPrimaryLine`

---

## 3) Customer 360 (TPH)

- `IndividualCustomer`: NationalId (unique), وثائق `CustomerIdentityDocument`
- `CorporateCustomer`: CommercialRegistryNumber (unique), `ParentCustomerId`, `BillingConsolidationMode`
- `CustomerStatus`: Active, Suspended, Closed, **Blacklisted** + `StatusReasonCode`

---

## 4) Product Catalog (TM Forum)

- `ProductOffering`: `PaymentType`, `BillingCycle` (enums)
- `PricePlan`: PricePerMinute, PricePerMegabyte, PricePerSms
- `ProductOfferingComponent.RequiresProductOfferingId` لـ VAS

---

## 5) PII (Block 3)

حقول مشفّرة عبر EF Value Converter:

- `IndividualCustomer.NationalId`
- `SimInventory.Pin1`, `Pin2`
- بحث NationalId: عمود `NationalIdSearchHash` (HMAC) — Block 3

---

## 6) ترتيب التنفيذ

| Block | المحتوى |
|-------|---------|
| 1 | Lifecycle + SubscriptionBindingService |
| 2 | Customer 360 + Catalog |
| 3 | PII encryption |
| 4 | APPLICATION_LAYER_MASTER_PROMPT_AR.md (لاحقاً) |

---

## 7) ممنوعات

- إعادة كيانات ERP
- `NationalId` على `SubscriberProfile`
- دمج MSISDN و SIM في كيان واحد
- منطق الربط داخل Razor/Handlers فقط بدون Domain Service
