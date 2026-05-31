# عينات اختبار الاستيراد الضخم (Bulk Import)

## الملفات

| الملف | نوع Job | الأعمدة |
|-------|---------|---------|
| `sample-msisdn-assets.csv` | MsisdnAsset | MSISDN, IMSI, ICCID, Pin1, Puk1 |
| `sample-customer-profiles.csv` | CustomerProfiles | CustomerCode, FullNameAr, FullNameEn, NationalId, CustomerType |
| `sample-package-migration.csv` | PackageMigration | MSISDN, CurrentOfferCode, NewOfferCode |

## التوليد

1. انسخ `config.sample.json` إلى `config.local.json`.
2. املأ المعرفات من قاعدة بياناتك:

```sql
SELECT TOP 1 Id, Name FROM CustomerGroup WHERE IsDeleted = 0;
SELECT TOP 1 Id, Name FROM CustomerCategory WHERE IsDeleted = 0;
SELECT TOP 1 Id, Name FROM Product WHERE IsDeleted = 0;
-- اختياري:
SELECT TOP 1 Id, Name FROM ProductOffering WHERE IsDeleted = 0;
```

3. شغّل:

```powershell
cd testdata\bulk-import
.\generate-samples.ps1
.\generate-samples.ps1 -RowCount 10000
```

الافتراضي: **3000 صف** لكل ملف، مع ~2% صفوف خاطئة عمداً لاختبار `PartiallySucceeded` وDrawer الأخطاء.

## ترتيب الاختبار الموصى به

1. **MsisdnAsset** — ارفع `sample-msisdn-assets.csv` من `/Telecom/BulkImportMonitor`.
2. **CustomerProfiles** — بعد ضبط `CustomerGroupId` / `CustomerCategoryId` في config.
3. **PackageMigration** — يحتاج اشتراكات (`TelecomSubscription`) على نفس أرقام MSISDN؛ إن لم تُنشأ بعد، توقّع أخطاء صف «لا يوجد اشتراك» (مفيد لاختبار التصدير والأخطاء).

## ملاحظات

- أرقام MSISDN بصيغة `09xxxxxxxx` (نطاق تجريبي).
- ICCID بطول 20 مع Luhn صالح.
- الرقم الوطني للأفراد: 10 خانات فريدة لكل صف.
- لا تُرفع ملفات `config.local.json` إلى Git (أضفها لـ `.gitignore` إن لزم).
