-- Catalog-driven telecom: ProductOffering -> Product (technical), TelecomOperationRequest -> ProductOffering (commercial selection).
-- Run against your CRM database after deploying the application build that includes these columns.

IF COL_LENGTH('dbo.ProductOffering', 'ProductId') IS NULL
BEGIN
    ALTER TABLE dbo.ProductOffering ADD ProductId nvarchar(450) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProductOffering_Product_ProductId'
)
BEGIN
    ALTER TABLE dbo.ProductOffering WITH CHECK
    ADD CONSTRAINT FK_ProductOffering_Product_ProductId
        FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id);
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ProductOfferingId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ProductOfferingId nvarchar(450) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TelecomOperationRequest_ProductOffering_ProductOfferingId'
)
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest WITH CHECK
    ADD CONSTRAINT FK_TelecomOperationRequest_ProductOffering_ProductOfferingId
        FOREIGN KEY (ProductOfferingId) REFERENCES dbo.ProductOffering (Id);
END
GO

/*
-- Optional backfill examples (uncomment and adjust if you already have ProductOffering rows without ProductId):

UPDATE po
SET po.ProductId = p.Id
FROM dbo.ProductOffering po
INNER JOIN dbo.Product p ON p.ServiceCode = N'YAHALA_SHABAB' AND p.IsDeleted = 0
WHERE po.Code = N'YA_HALA_30' AND po.IsDeleted = 0;

UPDATE po
SET po.ProductId = p.Id
FROM dbo.ProductOffering po
INNER JOIN dbo.Product p ON p.ServiceCode = N'SYR_MIX_HYBRID' AND p.IsDeleted = 0
WHERE po.Code = N'MIX_500' AND po.IsDeleted = 0;
*/
