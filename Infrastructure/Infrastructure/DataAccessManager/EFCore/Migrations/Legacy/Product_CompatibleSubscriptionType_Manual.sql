/*
  Manual migration — Product.CompatibleSubscriptionTypeId (nullable FK to TelecomSubscriptionTypes).
  Idempotent on SQL Server. Mirrors ApplyProductCompatibleSubscriptionTypeSchemaPatches in Infrastructure/DataAccessManager/EFCore/DI.cs.
*/
IF COL_LENGTH(OBJECT_ID(N'dbo.Product', N'U'), N'CompatibleSubscriptionTypeId') IS NULL
BEGIN
    ALTER TABLE dbo.Product ADD [CompatibleSubscriptionTypeId] NVARCHAR(50) NULL;
END
GO

IF OBJECT_ID(N'dbo.TelecomSubscriptionTypes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_CompatibleSubscriptionType')
BEGIN
    ALTER TABLE dbo.Product
    ADD CONSTRAINT FK_Product_CompatibleSubscriptionType
    FOREIGN KEY ([CompatibleSubscriptionTypeId]) REFERENCES dbo.TelecomSubscriptionTypes ([Id]);
END
GO

DECLARE @Pre NVARCHAR(50) = N'a0e0e0e0-0000-4000-8000-000000000001';
DECLARE @Post NVARCHAR(50) = N'a0e0e0e0-0000-4000-8000-000000000002';
DECLARE @Hyb NVARCHAR(50) = N'a0e0e0e0-0000-4000-8000-000000000003';

UPDATE dbo.Product SET CompatibleSubscriptionTypeId = @Pre
WHERE IsDeleted = 0 AND ServiceCode = N'YAHALA_SHABAB';

UPDATE dbo.Product SET CompatibleSubscriptionTypeId = @Hyb
WHERE IsDeleted = 0 AND ServiceCode = N'SYR_MIX_HYBRID';

UPDATE dbo.Product SET CompatibleSubscriptionTypeId = @Post
WHERE IsDeleted = 0 AND ServiceCode = N'SYR_POST_PLAT';

UPDATE dbo.Product SET CompatibleSubscriptionTypeId = NULL
WHERE IsDeleted = 0 AND (
    ServiceCode IN (N'SABA_10GB', N'BUSINESS_PRO_50', N'NIGHT_UNL', N'HW_ROUTER_5G', N'HW_WINGLE', N'SRV_ACTIVATION', N'SRV_PROMO_DISC')
    OR ServiceCode IS NULL OR LTRIM(RTRIM(ServiceCode)) = N''
);
GO
