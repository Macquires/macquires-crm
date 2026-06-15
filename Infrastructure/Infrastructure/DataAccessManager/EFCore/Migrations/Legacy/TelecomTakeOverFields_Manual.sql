-- Transfer of Ownership (§7) — TKO- operation fields.

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TransferReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TransferReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TakeOverObligationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TakeOverObligationStatus nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DepositTransferPolicy') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DepositTransferPolicy int NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ApprovalLevelRequired') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ApprovalLevelRequired nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TakeOverEffectiveDateUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TakeOverEffectiveDateUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'OldCustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD OldCustomerId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'NewCustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD NewCustomerId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorSubscriberProfileId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PriorSubscriberProfileId nvarchar(50) NULL;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_TakeOver')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_TakeOver
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 2 AND IsDeleted = 0;
END
GO
