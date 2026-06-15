-- §10 Service Termination (TRM-) — operation fields.

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TerminationType') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TerminationType nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TerminationReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TerminationReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'TerminationEffectiveDateUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD TerminationEffectiveDateUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'FinalBillAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD FinalBillAmount decimal(18, 2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DepositSettlementAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DepositSettlementAmount decimal(18, 2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DepositSettlementStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DepositSettlementStatus nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RetentionOfferOutcome') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RetentionOfferOutcome nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeprovisionStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DeprovisionStatus nvarchar(64) NULL;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_Termination')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_Termination
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 7 AND IsDeleted = 0;
END
GO
