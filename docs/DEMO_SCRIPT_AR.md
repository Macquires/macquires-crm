# سكريبت الديمو — nSuite Telecom CRM

## الإعداد
- شغّل `docker compose up` أو شغّل التطبيق + Network Simulator على المنفذ 5099.
- فعّل `IsDemoVersion=true` و`TelecomIntegrations:Mode=Http`.

## الرحلات الست

1. **تفعيل خط جديد (ACT)** — Customer 360 → حجز MSISDN → رفع KYC → Confirm → راقب Live Integration Console.
2. **Suspension → Reconnect (RCN)** — خط `093...5` → حظر → إعادة تفعيل مع فحص أهلية.
3. **SimSwap مفقود/مسروق** — تبديل شريحة مع pre-swap suspension تلقائي.
4. **فشل HLR + تعويض** — فعّل Chaos على Simulator → لاحظ التذكرة الفنية + عكس CBS.
5. **Revenue Leakage** — خط معلّق في CRM وACTIVE على HLR → تنبيه RA تلقائي.
6. **Customer 360** — عرض المحفظة، الخطوط، التذاكر، وسجل التكامل.

## شاشة Live Integration
- افتح `/Telecom/IntegrationHealth` أو اتصل بـ SignalR hub `/hubs/integration-live`.
