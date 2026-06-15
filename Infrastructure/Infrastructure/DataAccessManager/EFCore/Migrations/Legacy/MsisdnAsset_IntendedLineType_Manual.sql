-- Stamps provisioning line type on pool MSISDNs (IN vs CBS routing) before reservation/sale.
-- Run against the database in ConnectionStrings:DefaultConnection if auto-patch did not apply.
-- Safe to re-run (idempotent). Replace [dbo] if your schema differs.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'MsisdnAsset' AND COLUMN_NAME = 'IntendedSubscriptionTypeId')
BEGIN
    ALTER TABLE dbo.MsisdnAsset ADD IntendedSubscriptionTypeId NVARCHAR(50) NULL;
END

GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MsisdnAsset') AND name = N'IX_MsisdnAsset_IntendedSubscriptionTypeId')
BEGIN
    CREATE INDEX IX_MsisdnAsset_IntendedSubscriptionTypeId ON dbo.MsisdnAsset ([IntendedSubscriptionTypeId]);
END
GO

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Product' AND COLUMN_NAME = 'CompatibleSubscriptionTypeId')
BEGIN
    UPDATE m
    SET IntendedSubscriptionTypeId = p.CompatibleSubscriptionTypeId
    FROM dbo.MsisdnAsset m
    INNER JOIN dbo.Product p ON p.Id = m.ProductId AND p.IsDeleted = 0
    WHERE m.IsDeleted = 0
      AND m.IntendedSubscriptionTypeId IS NULL
      AND p.CompatibleSubscriptionTypeId IS NOT NULL;
END
GO

DECLARE @Pre NVARCHAR(50) = N'a0e0e0e0-0000-4000-8000-000000000001';
DECLARE @Post NVARCHAR(50) = N'a0e0e0e0-0000-4000-8000-000000000002';
DECLARE @Hyb NVARCHAR(50) = N'a0e0e0e0-0000-4000-8000-000000000003';

UPDATE m
SET IntendedSubscriptionTypeId = CASE ABS(CHECKSUM(m.Msisdn)) % 3
    WHEN 0 THEN @Pre
    WHEN 1 THEN @Post
    ELSE @Hyb
END
FROM dbo.MsisdnAsset m
WHERE m.IsDeleted = 0 AND m.IntendedSubscriptionTypeId IS NULL;
GO
