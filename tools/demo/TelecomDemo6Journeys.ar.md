# سكريبت ديمو Telecom — 6 رحلات (Syriatel nSuite)

> المتطلبات: `docker compose up` + تسجيل دخول `admin@root.com` + فتح **Integration Monitor** (`/Telecom/IntegrationMonitor`).

## 1) تفعيل خط جديد (New Activation)
1. Customer 360 → عميل **سعدون** (أو إنشاء عميل فردي جديد).
2. عملية **New Activation** → اختيار MSISDN متاح + SIM.
3. رفع KYC → **Confirm**.
4. راقب CBS ثم HLR في Integration Monitor (SignalR).
5. تحقق: الخط `Active` + اشتراك في Customer 360.

## 2) Suspension → Reconnect
1. من خط نشط: **Temporary Suspension** (Billing).
2. Confirm → تحقق HLR bar.
3. **Reconnect** بنفس الخط مع `Payment` clearance.
4. Confirm → الخط يعود `Active`.

## 3) SimSwap (مفقود/مسروق)
1. خط نشط → **SimSwap** مع `IsLostOrStolenReport = true`.
2. Confirm → pre-swap suspension + شريحة جديدة `Active`.
3. تحقق: الشريحة القديمة `Burned` أو `Quarantined`.

## 4) فشل HLR + تعويض حي
1. في Network Simulator: فعّل **HLR timeout** أو **500** لـ MSISDN الاختبار.
2. نفّذ Confirm على عملية (Activation أو Reconnect).
3. تحقق: العملية `Failed` + تذكرة fallout + **لا Ghost Profile** (CBS compensated).
4. أوقف العطل في Simulator → أعد المحاولة بنجاح.

## 5) Revenue Leakage Detection
1. من Back Office → Revenue Assurance / Reconciliation.
2. شغّل **Reconciliation Job** (أو انتظر الجدولة).
3. تحقق: تذكرة انحراف عند اختلاف CRM vs Simulator.

## 6) Customer 360 الشامل
1. افتح **سعدون** / **Debt Subscriber** / **Hero MSISDN**.
2. راجع: خطوط، اشتراكات، عمليات مفتوحة، تذاكر، ديون CBS mock.
3. نفّذ **Device Sale** أو **Refund** من نفس الشاشة للتحقق من مسار مالي كامل.

---

## اختصارات API (للديمو التقني)
- Health: `GET /health`
- KPIs: `GET /api/Telecom/Dashboard/Kpis`
- Confirm: `POST /api/Telecom/ConfirmActivation/{operationId}`

## إعدادات الديمو
- `IsDemoVersion=true` في appsettings أو GlobalSettings.
- `TelecomIntegrations:Mode=Http` + Simulator على `:5099`.
- `RabbitMq:Enabled=true` عند تشغيل compose.
