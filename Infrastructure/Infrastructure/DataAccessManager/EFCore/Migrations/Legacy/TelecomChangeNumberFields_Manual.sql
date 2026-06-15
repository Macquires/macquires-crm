-- Change Number (§5) — CNR- operation fields.

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorMsisdnAssetId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PriorMsisdnAssetId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TargetMsisdnAssetId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TargetMsisdnAssetId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'NumberChangeReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD NumberChangeReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PremiumFeeAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PremiumFeeAmount decimal(18, 2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'NumberChangeMode') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD NumberChangeMode nvarchar(32) NULL;
END
GO

UPDATE dbo.TelecomOperationRequest SET NumberChangeMode = N'Internal' WHERE NumberChangeMode IS NULL AND Kind = 5;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_ChangeNumber')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_ChangeNumber
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 5 AND IsDeleted = 0;
END
GO

