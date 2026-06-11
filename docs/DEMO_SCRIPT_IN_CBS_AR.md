# سكربت الديمو الآمن: Prepaid (IN) vs Postpaid (CBS)

## قبل العرض

1. تأكد أن `Integration.IN.Enabled = true` و `Integration.Huawei.Enabled = true` من `/Administration/GlobalSettings?tab=integrations`.
2. راقب السجل من `/Telecom/IntegrationMonitor`.

## E1 — تفعيل Prepaid (مسار IN)

1. `/Telecom/TelecomHub` → **تفعيل خط جديد**
2. اختر **نوع الخط: شحن (Prepaid)** ثم MSISDN من المستودع (تجنّب أرقام تنتهي بـ `9` أو `7` في السيناريو السعيد)
3. اختر باقة متوافقة → رفع وثيقة → `REC-*` من الكاشير → Record Payment → Confirm
4. تحقق في Integration Monitor: `Huawei_IN` (Provision + Credit) ثم `Huawei_HLR`

## E2 — تفعيل Postpaid (مسار CBS)

1. نفس المعالج مع **نوع خط فاتورة**
2. Confirm → تحقق من `Huawei_CBS` ثم `Huawei_HLR`

## E3 — فشل IN (Fallout)

1. اختر MSISDN ينتهي بـ **`9`**
2. Confirm → يجب أن يفشل التفعيل ويُعاد الرقم **Available** في المستودع
3. تذكرة fallout في Hub أو Integration Monitor

## E4 — Timeout IN (Resilience)

1. Global Settings: `Integration.IN.TimeoutMilliseconds = 1500`
2. MSISDN ينتهي بـ **`7`** → Confirm → فشل + تعويض Saga
3. أعد المهلة إلى `5000` بعد العرض

## E5 — شحن لاحق (Top-up)

- **ليس من Hub** — استخدم `/Telecom/Customer360Profile` أو `/Customers/CustomerList`
- Recharge / Voucher → `CreatePaymentTransaction` → `ConfirmPaymentTransaction` → CBS `RechargeAsync`

## E6 — قاطع طوارئ IN

1. أوقف `Integration.IN.Enabled`
2. فعّل prepaid → يكمل بـ Fallback (`QueuedForSync`) محلياً
3. أعد التشغيل لمزامنة الطابور

## ملاحظات للجنة

| الميزة | Prepaid | Postpaid |
|--------|---------|----------|
| Provisioning عند التفعيل | IN | CBS |
| وديعة أولية | `CreditPrepaidBalanceAsync` (IN) | CBS Ledger |
| شحن لاحق | CBS (Customer360) | CBS |
| Hybrid | CBS (ليس IN) | CBS |
| إنهاء Prepaid | CBS فاتورة نهائية + IN Quarantined + HLR | CBS + HLR |
