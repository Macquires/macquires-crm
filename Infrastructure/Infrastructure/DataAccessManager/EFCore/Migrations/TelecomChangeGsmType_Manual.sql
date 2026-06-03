-- Change GSM Type / Service Technology (§6) — CGT- operation fields.

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SourceSubscriptionTypeId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SourceSubscriptionTypeId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TargetSubscriptionTypeId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TargetSubscriptionTypeId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'GsmMigrationReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD GsmMigrationReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'GsmEffectiveDateUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD GsmEffectiveDateUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'GsmCompatibilityStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD GsmCompatibilityStatus nvarchar(64) NULL;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_ChangeGsm')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_ChangeGsm
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 6 AND IsDeleted = 0;
END
GO
