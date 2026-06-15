-- §9 Reconnect (RCN-) — operation fields.

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ReconnectReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ReconnectReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ClearanceType') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ClearanceType nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SourceSuspensionOperationId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SourceSuspensionOperationId nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'FraudClearanceConfirmed') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD FraudClearanceConfirmed bit NOT NULL CONSTRAINT DF_TelecomOp_FraudClear DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'FraudClearanceByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD FraudClearanceByUserId nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ReactivationAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ReactivationAtUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ProvisioningResult') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ProvisioningResult nvarchar(64) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_Reconnect')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_Reconnect
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 9 AND IsDeleted = 0;
END
GO

UPDATE dbo.TelecomOperationRequest SET IsLostOrStolenReport = 0 WHERE IsLostOrStolenReport IS NULL;
GO
UPDATE dbo.TelecomOperationRequest SET FraudClearanceConfirmed = 0 WHERE FraudClearanceConfirmed IS NULL;
GO
UPDATE dbo.TelecomOperationRequest SET AutoReconnectEnabled = 0 WHERE AutoReconnectEnabled IS NULL;
GO
UPDATE dbo.TelecomOperationRequest SET NotificationSuppressed = 0 WHERE NotificationSuppressed IS NULL;
GO
