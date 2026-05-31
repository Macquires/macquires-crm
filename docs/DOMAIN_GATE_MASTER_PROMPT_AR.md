# بوابة النطاق — Domain Gate Master Prompt (تنفيذي)

> **الغرض:** مرجع إلزامي لأي ذكاء اصطناعي أو مطوّر يعمل على `macquires-crm`. يمنع سياسة «الترقيع» ويحوّل المستودع إلى **نواة BSS اتصالات (سيريتل)** فقط.

**القرار الهندسي المعتمد:** **Purge B** (تطهير كامل فوري) + **Identity A** (Customer TPH + SubscriberProfile تشغيلي).

**مراجع:** [CRM_MASTER_PROMPT_AR.md](./CRM_MASTER_PROMPT_AR.md) · [BSS_TELECOM_IMPLEMENTATION_MASTER_PLAN_AR.md](./BSS_TELECOM_IMPLEMENTATION_MASTER_PLAN_AR.md) · [SYRIATEL_MASTER_PROMPT.md](./SYRIATEL_MASTER_PROMPT.md)

---

## 1) الحوكمة — قواعد لا تُكسر

| # | القاعدة |
|---|---------|
| G1 | **ممنوع** إعادة إدخال كيانات ERP (مخزون، مشتريات، فواتير تجزئة، موردين، حجوزات عامة). |
| G2 | **ممنوع** وضع `NationalId` أو `CommercialRegistryNumber` على `SubscriberProfile` — الهوية على `Customer` TPH فقط. |
| G3 | **ممنوع** دمج SIM و MSISDN في كيان واحد — `MsisdnAsset` للأرقام، `SimInventory` للشرائح. |
| G4 | كل كيان يرث `BaseEntity`؛ soft-delete عبر `IsDeleted` + Global Query Filter. |
| G5 | Domain بلا EF/HTTP؛ التحقق من التفرد (NationalId, ICCID) في Application + فهرس DB. |
| G6 | تنفيذ البلوكات **1→5** بالترتيب؛ لا commit بنصف purge. |
| G7 | **ممنوع** إعادة patch يضيف `NationalId`/`CommercialRegistration` على `SubscriberProfile` — التنظيف عبر `ApplyDomainGateSubscriberProfileLegacyCleanupPatch` فقط. |

---

## 2) Purge B — قائمة الحذف (Checklist)

### Domain — احذف الكيانات

- Inventory: `Warehouse`, `InventoryTransaction`, `StockCount`, `GoodsReceive`, `TransferIn`, `TransferOut`, `PositiveAdjustment`, `NegativeAdjustment`, `Scrapping`, `DeliveryOrder`
- Procurement: `PurchaseOrder`, `PurchaseOrderItem`, `PurchaseRequisition`, `PurchaseRequisitionItem`, `PurchaseReturn`, `Vendor`, `VendorGroup`, `VendorCategory`, `VendorContact`
- Sales ERP: `SalesOrder`, `SalesOrderItem`, `SalesReturn`, `SalesQuotation`, `SalesQuotationItem`
- Finance: `Invoice`, `Bill`, `PaymentReceive`, `PaymentDisburse`, `CreditNote`, `DebitNote`, `Expense`, `Budget`, `Tax`, `PaymentMethod`
- Product master ERP: `ProductGroup`, `UnitMeasure`
- Productivity: `Booking`, `BookingGroup`, `BookingResource`, `ProgramManager`, `ProgramManagerResource`, `Todo`, `TodoItem`
- Marketing: `Lead`, `LeadContact`, `LeadActivity`, `Campaign`, `SalesTeam`, `SalesRepresentative`

### Domain — يُبقى

- Cross-cutting: `Company`, `NumberSequence`, `Token`, `FileDocument`, `FileImage`
- Party: `Customer` (TPH), `IndividualCustomer`, `CorporateCustomer`, `CustomerContact`, `CustomerGroup`, `CustomerCategory`
- BSS: `SubscriberProfile`, `MsisdnAsset`, `SimInventory`, `TelecomSubscription`, `TelecomOperationRequest`, `TelecomMsisdnChangeLog`, `TelecomSubscriptionTypeLookup`, `ProductOffering`, `ProductOfferingComponent`, `PricePlan`, `Product`, `BillingIntegrationLog`

### طبقات عليا — احذف لكل كيان محذوف

- `Core/Application/Features/{X}Manager/`
- `Presentation/ASPNET/BackEnd/Controllers/{X}Controller.cs`
- `Presentation/ASPNET/FrontEnd/Pages/{X}/`
- Seeders في `Infrastructure/SeedManager/Demos/`
- DbSet + EF Configuration + إدخالات `IEntityDbSet`
- عناصر القائمة في `NavigationTreeStructure.cs`

---

## 3) Identity A — عقود الكيانات

### Customer TPH (جدول واحد + Discriminator)

```
Customer (abstract)
├── IndividualCustomer  → NationalId [unique], DateOfBirth, Nationality, Gender, Occupation
└── CorporateCustomer   → CommercialRegistryNumber [unique], TaxNumber, AuthorizedSignatoryName, CompanyLegalStatus
```

**حقول مشتركة:** `AccountNumber`, `ContactEmail`, `PrimaryPhone`, `PostalAddress` (Value Object), `CustomerStatus`, `SubscriberProfiles`.

### SubscriberProfile — تشغيلي فقط

- `CustomerId`, `ServiceLineType` (Mobile | Broadband | FixedLine)
- `OperationalStatus`, `ActivationDateUtc`
- `MasterSubscriberProfileId` (شجرة حسابات)
- `LoyaltyPoints`, `PostpaidCreditLimit`, `PrepaidBalance`, `ChurnRiskScore` (تجاري/عرض)
- **لا** `NationalId`, **لا** `SubscriberType`

### علاقة Customer 360

```
Customer 1 ──* SubscriberProfile 1 ──* TelecomSubscription *──1 MsisdnAsset
                              └──* SimInventory (nullable حتى التفعيل)
```

---

## 4) الأصول — MsisdnAsset و SimInventory

| كيان | حقول أساسية | State machine |
|------|-------------|---------------|
| `MsisdnAsset` | `Msisdn`, `MsisdnCategory`, `PoolStatus`, `QuarantineEndsUtc` | Available → Reserved → Active → Suspended → Quarantined → Available |
| `SimInventory` | `Iccid` [unique], `Imsi`, `Pin1/2`, `Puk1/2`, `SimStatus` | نفس المنطق |

**فهارس إلزامية (filtered, non-clustered):**

- `IX_Customer_NationalId` (Individual, IsDeleted=0)
- `IX_Customer_CommercialRegistryNumber` (Corporate, IsDeleted=0)
- `IX_MsisdnAsset_Msisdn` (IsDeleted=0)
- `IX_SimInventory_Iccid` (IsDeleted=0)

---

## 5) Persistence

- Configuration منفصلة لكل كيان تحت `Infrastructure/.../Configurations/`
- `HasDiscriminator<string>("CustomerType")` على `Customer`
- Value Objects: `OwnsOne` لـ `PostalAddress`
- `private set` + domain methods؛ EF backing fields حيث يلزم
- Global Query Filter: `IsDeleted == false` على كل `IHasIsDeleted`

**سكربتات SQL يدوية:**

- `DomainGate_CustomerTph_DataMigration_Manual.sql`
- `DomainGate_DropLegacyTables_Manual.sql`

---

## 6) ترتيب البلوكات و DoD

| Block | المحتوى | DoD |
|-------|---------|-----|
| 0 | هذه الوثيقة | معتمدة من الفريق |
| 1 | Domain purge + TPH + SimInventory | `Domain` يبني |
| 2 | EF + SQL | `Infrastructure` يبني |
| 3 | Application purge + handlers | `Application` يبني |
| 4 | UI + Navigation | Solution كامل يبني |
| 5 | Tests + CRM_MASTER_PROMPT ملحق | اختبارات خضراء |

---

## 7) تعليمات للـ AI عند التنفيذ

1. اقرأ هذا الملف قبل أي تعديل.
2. لا تُضيف `DbSet` لكيان ERP «مؤقتاً».
3. عند إنشاء عميل: استخدم factory على `IndividualCustomer` / `CorporateCustomer`.
4. Unified Search: استعلم `Customer` مع `OfType<IndividualCustomer>()` / فلاتر discriminator.
5. `ImportSimInventoryBatch` يكتب `SimInventory` وليس حقول ICCID على `MsisdnAsset`.

---

*آخر تحديث: Domain Gate B+A — macquires-crm*
