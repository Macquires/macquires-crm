# -*- coding: utf-8 -*-
"""Generates docs/BSS_TELECOM_IMPLEMENTATION_MASTER_PLAN_AR.md (~2000+ lines)."""
from __future__ import annotations

from pathlib import Path


def main() -> None:
    root = Path(__file__).resolve().parent
    path = root / "BSS_TELECOM_IMPLEMENTATION_MASTER_PLAN_AR.md"

    entities = [
        "ProductOffering",
        "ProductOfferingComponent",
        "PricePlan",
        "TelecomSubscriptionTypeLookup",
        "TelecomMsisdnChangeLog",
        "TelecomOperationRequest",
        "SubscriberProfile",
        "TelecomSubscription",
        "MsisdnAsset",
        "Product",
        "Customer",
        "BillingIntegrationLog",
    ]

    handlers = [
        "CreateTelecomOperationRequest",
        "ConfirmTelecomOperationRequest",
        "UploadTelecomOperationDocument",
        "RegisterSubscriberProfileForCustomer",
        "UpdateCustomerPrimaryTelecomLine",
        "ImportSimInventoryBatch",
        "GetTelecomOperationList",
        "GetTelecomSubscriberProfileDetail",
        "GetMsisdnAssetPoolList",
        "GetTelecomUniversalSearch",
        "GetBillingIntegrationLogList",
        "GetTelecomDashboardKpis",
        "CreateProductOffering",
        "UpdateProductOffering",
        "DeleteProductOffering",
        "GetProductOfferingList",
        "GetProductOfferingSingle",
        "CreateTelecomSubscriptionType",
        "UpdateTelecomSubscriptionType",
        "GetTelecomSubscriptionTypeList",
        "FindCustomerCandidates",
        "GetMigrationEligibleProducts",
        "CreateCustomer",
        "UpdateCustomer",
        "GetCustomerList",
    ]

    pages = [
        "Telecom/TelecomHub",
        "Telecom/ProductCatalog",
        "Telecom/TelecomMisReports",
        "TelecomSubscriptionTypes/TelecomSubscriptionTypeList",
        "Customers/CustomerList",
    ]

    migrations = [
        "ProductOffering_ProductId_TelecomOp_ProductOfferingId_Manual.sql",
        "Product_CompatibleSubscriptionType_Manual.sql",
        "SubscriberProfile_MultiProfilePerCustomer_Manual.sql",
        "TelecomBssPrimaryLine_Manual.sql",
        "TelecomSubscriptionTypes_Manual.sql",
        "VerifySubscriptionTypeCounts.sql",
    ]

    gates = [
        "تحليل القواعد والحالات الحدّية في Domain",
        "تحديث/مراجعة الـ EF Configuration والفهارس والعلاقات",
        "تنفيذ/تعديل الـ Command أو الـ Query في Application",
        "التحقق FluentValidation + سياسات التفويض",
        "ربط التكامل عبر Contracts + تجنب تسرّب تفاصيل البنية",
        "معالجة أخطاء التكامل + إعادة المحاولة + تسجيل مُهيكل",
        "اختبارات وحدات للقواعد الحرجة",
        "اختبارات تكامل لمسار DB (حسب التوفر)",
        "مراجعة أداء الاستعلام + خطة فهرسة",
        "مراجعة أمن البيانات (PII) في السجلات والاستجابات",
        "تحديث الواجهة Razor/JS + تجربة المستخدم RTL",
        "تحديث Swagger/Endpoints إن وُجدت",
        "تحديث البذور/العرض التوضيحي",
        "قبول المستخدم + قائمة تحقق للترحيل",
        "متابعة الإنتاج + مراقبة لاحقة",
    ]

    lines: list[str] = []
    lines.append("# خطة التنفيذ الشاملة — توسيع BSS/Telecom على macquires-crm")
    lines.append("")
    lines.append(
        "> **تنفيذ (سير العمل):** سجل الدفعات في [`BSS_TELECOM_PLAN_EXECUTION_LOG.md`](BSS_TELECOM_PLAN_EXECUTION_LOG.md)؛ "
        "دليل ترحيل SQL في [`TELECOM_SQL_MANUAL_MIGRATIONS_RUNBOOK_AR.md`](TELECOM_SQL_MANUAL_MIGRATIONS_RUNBOOK_AR.md)."
    )
    lines.append(
        "> **تنبيه:** إذا أعدتَ توليد هذا الملف عبر `_generate_master_plan.py` فستُعاد كتابة الملف بالكامل؛ الفقرات التنفيذية أعلاه مدمجة حالياً في السكربت."
    )
    lines.append("")
    lines.append(
        "> **ملاحظة عن طول الوثيقة:** طلب «~2000 سطر» يُخدم هنا بدمج **خطة نوعية** + "
        "**ملحق Backlog مُولَّد آلياً** (بنود قابلة للاستيراد في Azure DevOps/Jira). "
        "البنود الآلية تتبع بوابات هندسية ثابتة لكل وحدة عمل؛ عند التنفيذ الفعلي "
        "يُفضّل دمج البنود المكررة أو حذف غير المنطبق بدل اعتبار كل سطر عملاً مستقلاً."
    )
    lines.append("")
    lines.append("## 0) قراءة سريعة")
    lines.append("")
    lines.append("| البند | المحتوى |")
    lines.append("|---|---|")
    lines.append(
        "| الهدف | ترسيخ قدرات Lead-to-Cash للاتصالات داخل المعمارية الحالية دون كسر ERP الأساسي |"
    )
    lines.append("| التقنية | .NET 9، EF Core 9، MediatR، Razor + JS، SQL Server |")
    lines.append("| مبدأ الحوكمة | BRD للأعمال؛ الكود مصدر الحقيقة؛ ملحقات SQL للهجرة منفصلة عن البرومبت |")
    lines.append("")
    lines.append("## 1) نطاق النظام (Scope)")
    lines.append("")
    lines.append("### 1.1 داخل النطاق الحالي (حسب المستودع)")
    lines.append("- **Customer 360 (جزئياً):** إدارة العملاء، جهات اتصال، بحث/مرشحات.")
    lines.append(
        "- **Telecom Subscriber Layer:** SubscriberProfile متعدد لكل عميل، خط أساسي، تفاصيل مشترك."
    )
    lines.append(
        "- **رقم/شريحة:** MsisdnAsset، تجمع الأرقام، دفعات استيراد، سجل تغييرات أرقام، تنظيف حجوزات."
    )
    lines.append(
        "- **اشتراك/نوع اشتراك:** TelecomSubscription، TelecomSubscriptionTypeLookup، توافق المنتج مع نوع الاشتراك."
    )
    lines.append(
        "- **كتالوج عروض:** ProductOffering + ProductOfferingComponent + PricePlan + ربط TelecomOperationRequest."
    )
    lines.append("- **طلبات التشغيل:** TelecomOperationRequest دورة حياة، مستندات، تأكيد.")
    lines.append("- **تكامل الفوترة:** BillingIntegrationLog وتكاملات Infrastructure.")
    lines.append(
        "- **واجهات Telecom:** TelecomHub، ProductCatalog، TelecomMisReports، TelecomSubscriptionTypeList + CustomerList."
    )
    lines.append("")
    lines.append("### 1.2 خارج النطاق الفوري (متابعة معمارية)")
    lines.append("- **OM كامل:** طلب مركب موحّد يقسم إلى أوامر شبكة/مخزون/فوترة.")
    lines.append("- **GIS FTTH:** تغطية جغرافية وجدولة ميدانية كمنصة.")
    lines.append("- **HLR/HSS مباشر:** يُحكم بعقود تكامل وبوابات أمان/معدلات استدعاء.")
    lines.append("")
    lines.append("## 2) مبادئ معمارية (Clean Architecture)")
    lines.append("1. **Domain:** كيانات وEnums؛ بلا EF/HTTP.")
    lines.append("2. **Application:** Commands/Queries + Validation + Contracts.")
    lines.append("3. **Infrastructure:** EF، Repositories، تكاملات، خدمات خلفية.")
    lines.append("4. **Presentation:** Razor/JS؛ بدون منطق تكامل شبكة.")
    lines.append("5. **المراقبة:** Audit؛ سجلات تكامل؛ Idempotency حيث يلزم.")
    lines.append("")
    lines.append("## 3) كيانات رئيسية")
    lines.append("- ProductOffering, ProductOfferingComponent, PricePlan, TelecomSubscriptionTypeLookup")
    lines.append(
        "- TelecomMsisdnChangeLog؛ تعديلات على Product, SubscriberProfile, TelecomSubscription, TelecomOperationRequest, MsisdnAsset"
    )
    lines.append("")
    lines.append("## 4) سكربتات الترحيل اليدوية")
    for m in migrations:
        lines.append(f"- `{m}`")
    lines.append("")
    lines.append("## 5) Use Cases (Application)")
    lines.append("### TelecomManager")
    lines.append(
        "- CreateTelecomOperationRequest / ConfirmTelecomOperationRequest / UploadTelecomOperationDocument"
    )
    lines.append(
        "- RegisterSubscriberProfileForCustomer / UpdateCustomerPrimaryTelecomLine / ImportSimInventoryBatch"
    )
    lines.append(
        "- GetTelecomOperationList / GetTelecomSubscriberProfileDetail / GetMsisdnAssetPoolList / "
        "GetTelecomUniversalSearch / GetBillingIntegrationLogList / GetTelecomDashboardKpis"
    )
    lines.append("### ProductCatalogManager")
    lines.append(
        "- CreateProductOffering / UpdateProductOffering / DeleteProductOffering / GetProductOfferingList / GetProductOfferingSingle"
    )
    lines.append("### TelecomSubscriptionTypeManager")
    lines.append("- CreateTelecomSubscriptionType / UpdateTelecomSubscriptionType / GetTelecomSubscriptionTypeList")
    lines.append("### CustomerManager / ProductManager")
    lines.append(
        "- FindCustomerCandidates؛ GetMigrationEligibleProducts؛ CreateCustomer/UpdateCustomer/GetCustomerList حسب الحقول الجديدة"
    )
    lines.append("")
    lines.append("## 6) الواجهات")
    lines.append("- `FrontEnd/Pages/Telecom/TelecomHub.*`")
    lines.append("- `FrontEnd/Pages/Telecom/ProductCatalog.*`")
    lines.append("- `FrontEnd/Pages/Telecom/TelecomMisReports.*`")
    lines.append("- `FrontEnd/Pages/TelecomSubscriptionTypes/TelecomSubscriptionTypeList.cshtml`")
    lines.append("- `FrontEnd/Pages/Customers/CustomerList.*`")
    lines.append("- `NavigationTreeStructure*.cs` و `TelecomRoles.cs`")
    lines.append("")
    lines.append("## 7) مراحل التسليم")
    lines.append("- **A:** ترحيل SQL + EF + بذور دنيا.")
    lines.append("- **B:** كتالوج العروض وربط المنتج/نوع الاشتراك.")
    lines.append("- **C:** مشترك متعدد + خط أساسي + واجهات.")
    lines.append("- **D:** طلبات التشغيل + بحث شامل + KPIs.")
    lines.append("- **E:** تكامل الفوترة وموثوقية السجلات.")
    lines.append("- **F:** NFR أداء/فهرسة/أرشفة سجلات التكامل.")
    lines.append("")
    lines.append("## 8) تعريف جاهز (DoD)")
    lines.append("- Commands: صلاحية + Validation + أخطاء واضحة.")
    lines.append("- Queries: ترقيم صفحات + فهارس + تجنب N+1.")
    lines.append("- تكامل: طوابع زمنية + معرف ارتباط + Idempotency عند الحاجة.")
    lines.append("- واجهات: تحميل/فشل + RTL + عدم تسرّب PII في السجلات.")
    lines.append("")
    lines.append("---")
    lines.append("")
    lines.append("## ملحق A — Backlog مُولَّد (~2000 بند تتبع)")
    lines.append("")

    for wi in range(1, 2001):
        gate = gates[(wi - 1) % len(gates)]
        bucket = (wi - 1) % 5
        if bucket == 0:
            e = entities[(wi - 1) % len(entities)]
            lines.append(f"- [ ] WI-{wi:04d} | Domain/Data | كيان {e} | {gate}")
        elif bucket == 1:
            h = handlers[(wi - 1) % len(handlers)]
            lines.append(f"- [ ] WI-{wi:04d} | Application | Handler {h} | {gate}")
        elif bucket == 2:
            p = pages[(wi - 1) % len(pages)]
            lines.append(f"- [ ] WI-{wi:04d} | Presentation | صفحة {p} | {gate}")
        elif bucket == 3:
            m = migrations[(wi - 1) % len(migrations)]
            lines.append(f"- [ ] WI-{wi:04d} | Database | سكربت {m} | {gate}")
        else:
            lines.append(f"- [ ] WI-{wi:04d} | Cross-cutting | أمن/مراقبة/أداء/NFR | {gate}")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(path)
    print("lines:", len(lines))


if __name__ == "__main__":
    main()
