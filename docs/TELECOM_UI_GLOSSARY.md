# مسرد واجهة التليكوم (TELECOM UI Glossary)

مرجع **حاكم** لكل نصوص واجهة Macquires Telecom / Syriatel CRM. أي مفتاح جديد في [`telecom.en.json`](../Presentation/ASPNET/wwwroot/locales/telecom.en.json) و [`telecom.ar.json`](../Presentation/ASPNET/wwwroot/locales/telecom.ar.json) يجب أن يطابق هذا المسرد.

**مراجع النطاق:** [CRM_MASTER_PROMPT_AR.md](./CRM_MASTER_PROMPT_AR.md) · [SYRIATEL_MASTER_PROMPT.md](./SYRIATEL_MASTER_PROMPT.md)

---

## قواعد ذهبية

1. **لا خلط لغات في جملة واحدة** — عربي كامل أو إنجليزي كامل؛ الاختصارات المعيارية (MSISDN, ICCID, IMSI, HLR, CBS, OCS, VAS, SLA, BO, MGR, TKO, …) مسموحة في كلا اللغتين.
2. **لغة موظف التشغيل** — العربية تشغيلية سورية/عربية (مشترك، رقم الخط، ترحيل باقة)، وليست ترجمة حرفية من الإنجليزية العامة.
3. **لغة EN** — مصطلحات BSS/OSS و TM Forum SID حيث ينطبق (Subscriber, Product offering, Provisioning, Dunning, Write-off).
4. **ممنوع في واجهة الإنتاج:** Demo, Mock, Test, قمرة (للـ Back Office), Fallout وحده بدون سياق، عناوين `@*-demo.local`.
5. **أكواد العمليات** تبقى كما هي في الجداول والتقارير: `ACT-`, `MGR-`, `TKO-`, `SIM-`, `CNR-`, `CGT-`, `TRM-`, `SUS-`, `RCN-`, `RFD-`, `BDR-`, `DEV-`, `PAY-`.

---

## كيانات أساسية (Core entities)

| المفهوم | EN (UI) | AR (UI) | ملاحظة |
|--------|---------|---------|--------|
| Customer (Party) | Customer / Account | عميل / حساب | طرف CRM تجاري |
| Subscriber profile | Subscriber profile | ملف المشترك | مرتبط بخط/خطوط |
| Subscription | Subscription | اشتراك | حالة الخط على العرض |
| MSISDN | MSISDN | رقم الخط (MSISDN) | لا «رقم موبايل» في الشاشات التشغيلية |
| ICCID / SIM | SIM / ICCID | شريحة / ICCID | تبديل شريحة = SIM swap |
| Product offering | Product offering / Plan | عرض / باقة | من الكتالوج التجاري |
| Technical ticket | Technical ticket / Trouble ticket | تذكرة فنية | ليس «شكوى» فقط في سياق BO |
| Back office | Back-office operations center | مركز العمليات الخلفية | **لا** «قمرة» |
| Showroom | Showroom / Retail channel | معرض / قناة البيع | |
| Provisioning | Provisioning / Network execution | تنفيذ على الشبكة | |
| Fallout (KPI) | Provisioning fallout rate | نسبة فشل التنفيذ على الشبكة | لا «Fallout» منفرداً |
| Manual override | Manual override | استثناء يدوي | Overrides في KPI |
| Integration | Huawei CBS integration | تكامل Huawei CBS | **لا** Mock في العنوان |

---

## أنواع العمليات (Operation kinds → Hub tiles)

| Kind | EN title | AR title | Sub (badge) | Enum / Domain |
|------|----------|----------|-------------|---------------|
| activate | New activation | تفعيل خط جديد | ACT | NewActivation |
| migrate | Package migration | ترحيل باقة | MGR | Migration |
| changeGsm | Line technology change | تحويل تقنية الخط | CGT | ChangeGsmType |
| takeover | Ownership transfer | نقل ملكية | TKO | TakeOver |
| simswap | SIM replacement | تبديل شريحة | SIM | SimSwap |
| changeNumber | MSISDN change | تغيير رقم الخط | CNR | NumberPortability |
| termination | Line termination | إنهاء الخط | TRM | Termination |
| suspension | Temporary suspension | حظر مؤقت | SUS | TemporarySuspension |
| reconnect | Reconnection | إعادة تفعيل | RCN | Reconnect |
| refund | Refund / deposit settlement | استرداد وتسوية | RFD | DepositRefundSettlement |
| badDebt | Bad debt recovery | تحصيل ديون معدومة | BDR | BadDebtRecovery |
| deviceSale | Device sale | بيع جهاز | DEV | DeviceSale |
| addpackage | VAS / add-on activation | تفعيل خدمة إضافية | VAS | ServiceModification |
| support | Service modification / trouble ticket | تذكرة دعم فني | TKT | ServiceModification (تمييز UX عن addpackage) |

---

## حالات التذكرة الفنية

| Code | EN | AR |
|------|----|----|
| 0 | Open | مفتوحة |
| 1 | In progress | قيد التنفيذ |
| 2 | Resolved | تم الحل |
| 3 | Escalated | مُصعَّدة |

**إجراء الصف:** EN `Work ticket` · AR `تنفيذ معالجة التذكرة` (مفتاح `backOffice.dashboard.grid.treat` — ليس «معالجة» العامة).

---

## أولوية التذكرة

| Code | EN | AR |
|------|----|----|
| 0 | Low | منخفضة |
| 1 | Medium | متوسطة |
| 2 | High | عالية |
| 3 | Critical | حرجة |

---

## نوع المشكلة (Issue type)

| Code | EN | AR |
|------|----|----|
| 0 | Network | شبكة |
| 1 | Billing | فوترة |
| 2 | SIM barring | حظر شريحة |
| 3 | Activation | تفعيل |

---

## مراحل التحصيل (Dunning) — عرض UI

| Code | EN | AR |
|------|----|----|
| Reminder1 | Payment reminder | تذكير دفع |
| SoftBar | Soft barring | حظر جزئي |
| HardBar | Hard barring | حظر كامل |
| Agency | Collection agency | إحالة وكالة تحصيل |
| WriteOff | Write-off | شطب دين |

---

## حالات الدفع (Payment transaction)

| Code | EN | AR |
|------|----|----|
| 0 | Draft | مسودة |
| 1 | Gateway pending | بانتظار البوابة |
| 2 | Completed | مكتمل |
| 3 | Failed | فاشل |
| 4 | Reversed | معكوس |

---

## مصطلحات مرفوضة → البديل المعتمد

| مرفوض | البديل EN | البديل AR |
|-------|-----------|-----------|
| قمرة العمليات | Back-office operations center | مركز العمليات الخلفية |
| Fallout (label only) | Provisioning fallout rate | نسبة فشل التنفيذ على الشبكة |
| Treat (ambiguous) | Work ticket | تنفيذ معالجة التذكرة |
| (demo) في UI | — (احذف) | — (احذف) |
| Mock CBS | Huawei CBS integration | تكامل Huawei CBS |
| Add package vs Support same label | VAS activation vs Trouble ticket | تفعيل VAS vs تذكرة دعم فني |
| Command center (إن أربك) | Operations center | مركز العمليات |

---

## ربط الملفات (أين تُطبَّق)

| آلية | الملفات | مفتاح JSON |
|------|---------|------------|
| Razor ثابت | `data-telecom-i18n` | `telecom.backOffice.*`, أقسام جديدة `customerList`, `customer360`, … |
| Vue Hub | `t('telecom.*')` | `telecom.wizard`, `telecom.tiles`, `telecom.ops` |
| Badges | `telecom-ui-badges.js` | `telecom.backOffice.dashboard.badges.*` |
| تنقل | `NavigationTreeStructure.TelecomBilingual.cs` | `telecom.navigation.leafs.*` |

---

## عملية المراجعة عند إضافة نص

1. هل المصطلح موجود في جدول هذا المسرد؟
2. هل المفتاح موجود في **EN و AR** معاً؟
3. هل الجملة خالية من Demo/Mock/خلط لغات؟
4. هل الاختصار التشغيلي (MGR, BO) موضّح في السياق أول مرة إن لزم؟

---

## سجل تنقيح (الخطوة 1)

| تاريخ | تغيير |
|-------|--------|
| 2026-06-04 | إنشاء المسرد؛ استبدال «قمرة» بـ «مركز العمليات الخلفية» في `telecom.ar.json`؛ إزالة `(demo)` من اعتماد التحصيل؛ إضافة `kpi.falloutRate`؛ توحيد عناوين BO EN |
