-- §11 Product Catalog — link subscription to commercial offering (TM Forum).

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomSubscription', 'ProductOfferingId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomSubscription ADD ProductOfferingId NVARCHAR(50) NULL;
END
ELSE
BEGIN
    ALTER TABLE dbo.TelecomSubscription ALTER COLUMN ProductOfferingId NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorProductId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PriorProductId NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorProductOfferingId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PriorProductOfferingId NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_TelecomSubscription_ProductOffering_ProductOfferingId'
)
BEGIN
    ALTER TABLE dbo.TelecomSubscription WITH CHECK
        ADD CONSTRAINT FK_TelecomSubscription_ProductOffering_ProductOfferingId
            FOREIGN KEY (ProductOfferingId) REFERENCES dbo.ProductOffering (Id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TelecomSubscription_ProductOfferingId')
BEGIN
    CREATE INDEX IX_TelecomSubscription_ProductOfferingId
        ON dbo.TelecomSubscription (ProductOfferingId)
        WHERE ProductOfferingId IS NOT NULL AND IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TelecomOperationRequest_OfferSubscription')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_OfferSubscription
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind IN (1, 4) AND IsDeleted = 0;
END
GO
