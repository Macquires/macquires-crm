-- §8 Temporary Suspension (SUS-) — operation fields.

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SuspensionType') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SuspensionType nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SuspensionReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SuspensionReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SuspensionStartDateUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SuspensionStartDateUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SuspensionEndDateUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SuspensionEndDateUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'BarringLevel') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD BarringLevel nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'AutoReconnectEnabled') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD AutoReconnectEnabled bit NOT NULL CONSTRAINT DF_TelecomOp_AutoReconnect DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'NotificationSuppressed') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD NotificationSuppressed bit NOT NULL CONSTRAINT DF_TelecomOp_NotifSuppress DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'BarStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD BarStatus nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorOperationalStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PriorOperationalStatus nvarchar(32) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_Suspension')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_Suspension
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 8 AND IsDeleted = 0;
END
GO
