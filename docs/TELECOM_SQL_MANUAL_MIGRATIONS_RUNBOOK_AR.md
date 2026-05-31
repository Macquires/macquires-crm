# دليل ترحيل SQL اليدوي — طبقة Telecom / كتالوج العروض

يُكمّل هذا الدليل خطة التنفيذ الشاملة (`BSS_TELECOM_IMPLEMENTATION_MASTER_PLAN_AR.md`) ويركّز على **المرحلة A** (قاعدة البيانات خارج أو بجانب ترحيلات EF الرسمية).

## 1) مبدأ عام

- في بيئات **التطوير المحلية** غالباً تُطبَّق نفس التغييرات تلقائياً عند تشغيل التطبيق عبر دوال الـ patch في  
  `Infrastructure/Infrastructure/DataAccessManager/EFCore/DI.cs` (انظر القسم 3).
- في **الإنتاج / لدى DBA** يُفضَّل تنفيذ سكربتات SQL **مراجَعة ومُعتمدة** من المجلد  
  `Infrastructure/Infrastructure/DataAccessManager/EFCore/Migrations/` بدل الاعتماد فقط على تشغيل التطبيق.

## 2) ترتيب مقترح لتنفيذ السكربتات اليدوية (اعتماديات FK)

1. **`TelecomSubscriptionTypes_Manual.sql`**  
   ملف مرجعي؛ إنشاء الجدول والبيانات قد يكون عبر `ApplyTelecomSubscriptionTypeSchemaPatches` في `DI.cs`.  
   **يجب** أن يوجد جدول `dbo.TelecomSubscriptionTypes` قبل أي سكربت يضيف FK إليه.

2. **`Product_CompatibleSubscriptionType_Manual.sql`**  
   يضيف `Product.CompatibleSubscriptionTypeId` و FK إلى `TelecomSubscriptionTypes`.  
   **لا تنفّذ** قبل توفر الجدول أعلاه.

3. **`SubscriberProfile_MultiProfilePerCustomer_Manual.sql`**  
   يزيل الفهرس الفريد على `CustomerId` ويستبدله بفهرس غير فريد.  
   يمكن تنفيذه بشكل مستقل بمجرد وجود `dbo.SubscriberProfile`.

4. **`ProductOffering_ProductId_TelecomOp_ProductOfferingId_Manual.sql`**  
   يضيف `ProductOffering.ProductId` و `TelecomOperationRequest.ProductOfferingId` والمفاتيح الأجنبية.  
   **يتطلّب** وجود الجداول `ProductOffering`، `Product`، `TelecomOperationRequest`.

5. **`TelecomBssPrimaryLine_Manual.sql`**  
   ينشئ `TelecomMsisdnChangeLog` (إن لم يكن موجوداً) ويعدّل فهرسة `MsisdnAsset`.  
   **يتطلّب** وجود `dbo.Customer` (FK في سجل التغيير كما في السكربت).

6. **`VerifySubscriptionTypeCounts.sql`**  
   للتحقق والتحليل بعد الترحيل (استعلامات مُعلّقة داخل الملف؛ تفعّلها يدوياً حسب حالة الأعمدة لديكم).

## 3) ترتيب الـ patch عند تشغيل التطبيق (مرجع)

من `DI.cs` (تقريباً بالترتيب التالي عند تهيئة الـ DbContext):

- `ApplyTelecomOperationRequestSchemaPatches`
- `ApplyTelecomBssPrimaryLineSchemaPatches`
- `ApplyTelecomSubscriptionTypeSchemaPatches`
- `ApplyProductCompatibleSubscriptionTypeSchemaPatches`
- `ApplyProductOfferingSmartFieldsPatch`
- `ApplyProductOfferingProductIdAndOperationOfferingPatch`
- `ApplySubscriberProfileCustomerIndexNonUniquePatch`
- `ApplyDomainGateSubscriberProfileLegacyCleanupPatch` (إزالة أعمدة الهوية القديمة من `SubscriberProfile`)

عند تعارض بين سكربت يدوي وpatch وقت التشغيل: **الهدف idempotency**؛ راجع السجلات و`sys.columns` / `sys.foreign_keys` قبل إعادة التنفيذ.

## 4) سجل التنفيذ

راجع وحدّث: `docs/BSS_TELECOM_PLAN_EXECUTION_LOG.md` بعد كل بيئة (Dev / Staging / Prod).
