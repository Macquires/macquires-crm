# فحص يدوي سريع — Telecom E2E Smoke

**التاريخ:** 2026-06-04  
**قاعدة:** `SyriaTeltest18` (أو بيئة الديمو) + تشغيل سكربتات `RUN_*_Manual.ps1` عند الحاجة.

## قبل البدء

- [ ] `dotnet test Tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~Telecom"` — **102+** اختبار Telecom (KPI + VAL-14-04 queue)
- [ ] بعد migrations يدوية قديمة: `RUN_Database_AllNullableBitColumnsFix_Manual.ps1` (يمنع `SqlNullValueException` على أعمدة `bit NULL`)
- [ ] Logout / Login بعد تحديث `PermissionCatalog` أو seed
- [ ] مستخدم Showroom + مستخدم BackOffice بصلاحيات telecom

## Customer 360 (`/Telecom/Customer360Profile`)

| # | السيناريو | الخطوات | متوقع |
|---|-----------|---------|--------|
| 1 | SUS | اختيار خط نشط → حظر مؤقت → CustomerRequest → تأكيد | عملية SUS- + Completed أو PendingDocuments (Fraud) |
| 2 | RCN | خط محظور → إعادة تفعيل → فحص أهلية → تأكيد | RCN- + CBS unbar (mock) |
| 3 | RFD | استرداد تأمين/محفظة | RFD- + BO إن Syriatel Cash |
| 4 | BDR | تحصيل دفعة على 0939000002 | BDR- + تحديث ذمة |
| 5 | DEV | بيع جهاز | DEV- + عقد تقسيط |

## Customer List — مودالات inline

| # | إجراء القائمة | متوقع |
|---|---------------|--------|
| 6 | SUS من dropdown الخط | فتح `C360SuspensionModal` بدون redirect |
| 7 | RCN | `C360ReconnectModal` + أهلية |
| 8 | RFD | `C360RefundModal` |
| 9 | BDR | `C360BadDebtModal` |
| 10 | DEV | redirect لـ 360 wizard (`deviceSale`) |

## Payment Services (PAY-)

| # | السيناريو | متوقع |
|---|-----------|--------|
| P1 | List/360 → شحن محفظة + مرجع دفع | PAY- + Completed + SMS mock + رصيد محدّث |
| P2 | قسيمة `ValidateVoucher` ثم Create type=1 | Face value من Mock |
| P3 | BO → عكس خلال 120 دقيقة | `Reversed` + CBS reverse |
| P4 | BO KPI panel | `GetPaymentServicesKpis` |

**SQL:** `TelecomPaymentServices_Manual.sql` + `TelecomPaymentRechargePermission_Manual.sql`

## Hub + Back Office

| # | لوحة | متوقع |
|---|------|--------|
| 11 | Hub tiles | إنشاء عمليات SUS/RCN/RFD/BDR/DEV |
| 12 | BO KPI SUS/RCN | أرقام من `GetSuspensionKpis` / `GetReconnectKpis` |
| 13 | BO KPI DEV | `loadDeviceSaleKpis` |
| 14 | BO queue | اعتماد Fraud/Regulatory/Syriatel Cash |

## تكاملات خلفية

| # | بند | متوقع |
|---|-----|--------|
| 15 | CGT SMS | إشعار بعد تغيير رقم |
| 16 | VAL-14-04 | قسط متأخر → تذكرة `Collections` (HostedService كل 6 ساعات أو seed) |

## أبطال الديمو (seed)

- `0939000091` — RCN Fraud (BO)
- `0939000002` — BDR ذمة سالبة
- خطوط SUS في `EnsureHeroReconnectDemoAsync`

---

*بعد أي فشل: راجع `TelecomOperationRequest` + Integration logs + console الشبكة في المتصفح.*
