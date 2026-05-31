/*
  Manual migration (Telecom primary line / MSISDN audit) — idempotent on SQL Server.
  Mirrors ApplyTelecomBssPrimaryLineSchemaPatches in DataAccessManager/EFCore/DI.cs.
  Use when applying schema outside the app startup patch (e.g. DBA pipeline).
*/
IF OBJECT_ID(N'dbo.TelecomMsisdnChangeLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelecomMsisdnChangeLog (
        [Id] NVARCHAR(50) NOT NULL CONSTRAINT PK_TelecomMsisdnChangeLog PRIMARY KEY,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_TelecomMsisdnChangeLog_IsDeleted DEFAULT (0),
        [CreatedAtUtc] DATETIME2(7) NULL,
        [CreatedById] NVARCHAR(450) NULL,
        [UpdatedAtUtc] DATETIME2(7) NULL,
        [UpdatedById] NVARCHAR(450) NULL,
        [CustomerId] NVARCHAR(50) NOT NULL,
        [SubscriberProfileId] NVARCHAR(50) NOT NULL,
        [TelecomSubscriptionId] NVARCHAR(50) NOT NULL,
        [MsisdnAssetId] NVARCHAR(50) NOT NULL,
        [OldMsisdn] NVARCHAR(32) NULL,
        [NewMsisdn] NVARCHAR(32) NULL,
        [OldSubscriptionType] NVARCHAR(32) NULL,
        [NewSubscriptionType] NVARCHAR(32) NULL,
        [ExternalSyncSuccess] BIT NULL,
        [ExternalSyncMessage] NVARCHAR(255) NULL,
        CONSTRAINT FK_TelecomMsisdnChangeLog_Customer FOREIGN KEY ([CustomerId]) REFERENCES dbo.Customer ([Id])
    );
    CREATE INDEX IX_TelecomMsisdnChangeLog_CustomerId ON dbo.TelecomMsisdnChangeLog ([CustomerId]);
    CREATE INDEX IX_TelecomMsisdnChangeLog_MsisdnAssetId ON dbo.TelecomMsisdnChangeLog ([MsisdnAssetId]);
    CREATE INDEX IX_TelecomMsisdnChangeLog_CreatedAtUtc ON dbo.TelecomMsisdnChangeLog ([CreatedAtUtc]);
END
GO

IF OBJECT_ID(N'dbo.MsisdnAsset', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MsisdnAsset') AND name = N'IX_MsisdnAsset_Msisdn')
        DROP INDEX IX_MsisdnAsset_Msisdn ON dbo.MsisdnAsset;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MsisdnAsset') AND name = N'IX_MsisdnAsset_Msisdn_ActiveOnly')
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX IX_MsisdnAsset_Msisdn_ActiveOnly
        ON dbo.MsisdnAsset ([Msisdn])
        WHERE ([IsDeleted] = 0);
    END
END
GO
