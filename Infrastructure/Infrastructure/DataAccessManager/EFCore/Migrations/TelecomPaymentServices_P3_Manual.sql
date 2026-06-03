-- Payment Services P3 — reversal fields + audit log.

IF COL_LENGTH('dbo.TelecomPaymentTransaction', 'IsReversal') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomPaymentTransaction ADD IsReversal bit NOT NULL
        CONSTRAINT DF_TelecomPaymentTransaction_IsReversal DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.TelecomPaymentTransaction', 'OriginalPaymentId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomPaymentTransaction ADD OriginalPaymentId nvarchar(50) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomPaymentTransaction', 'ReversalReasonCode') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomPaymentTransaction ADD ReversalReasonCode nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomPaymentTransaction', 'ReversedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomPaymentTransaction ADD ReversedAtUtc datetime2 NULL;
END
GO

IF OBJECT_ID(N'dbo.TelecomPaymentAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelecomPaymentAuditLog (
        Id nvarchar(50) NOT NULL CONSTRAINT PK_TelecomPaymentAuditLog PRIMARY KEY,
        IsDeleted bit NOT NULL CONSTRAINT DF_TelecomPaymentAuditLog_IsDeleted DEFAULT (0),
        CreatedAtUtc datetime2 NULL,
        CreatedById nvarchar(50) NULL,
        UpdatedAtUtc datetime2 NULL,
        UpdatedById nvarchar(50) NULL,
        TelecomPaymentTransactionId nvarchar(50) NOT NULL,
        Action nvarchar(64) NOT NULL,
        FromStatus int NULL,
        ToStatus int NULL,
        BalanceBefore decimal(18, 2) NULL,
        BalanceAfter decimal(18, 2) NULL,
        GatewayReference nvarchar(128) NULL,
        ActorUserId nvarchar(50) NULL,
        ReasonCode nvarchar(64) NULL,
        Note nvarchar(512) NULL,
        OccurredAtUtc datetime2 NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomPaymentAuditLog_PaymentId')
BEGIN
    CREATE INDEX IX_TelecomPaymentAuditLog_PaymentId
        ON dbo.TelecomPaymentAuditLog (TelecomPaymentTransactionId, OccurredAtUtc DESC);
END
GO
