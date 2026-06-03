-- Payment Services / Financial Billing Core (§12) — PAY- ledger.
-- Run after deploying build that includes Domain + EF configuration updates.

IF OBJECT_ID(N'dbo.TelecomPaymentTransaction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelecomPaymentTransaction (
        Id nvarchar(50) NOT NULL CONSTRAINT PK_TelecomPaymentTransaction PRIMARY KEY,
        IsDeleted bit NOT NULL CONSTRAINT DF_TelecomPaymentTransaction_IsDeleted DEFAULT (0),
        CreatedAtUtc datetime2 NULL,
        CreatedById nvarchar(50) NULL,
        UpdatedAtUtc datetime2 NULL,
        UpdatedById nvarchar(50) NULL,
        Number nvarchar(32) NOT NULL,
        CorrelationId nvarchar(50) NULL,
        TransactionType int NOT NULL,
        Status int NOT NULL,
        PaymentChannel int NOT NULL,
        ServiceChannel int NOT NULL CONSTRAINT DF_TelecomPaymentTransaction_ServiceChannel DEFAULT (0),
        Amount decimal(18, 2) NOT NULL,
        Currency nvarchar(8) NOT NULL CONSTRAINT DF_TelecomPaymentTransaction_Currency DEFAULT (N'SYP'),
        CustomerId nvarchar(50) NOT NULL,
        SubscriberProfileId nvarchar(50) NOT NULL,
        TelecomSubscriptionId nvarchar(50) NULL,
        Msisdn nvarchar(32) NULL,
        GatewayReference nvarchar(128) NULL,
        GatewayTransactionId nvarchar(128) NULL,
        ReceiptNumber nvarchar(64) NULL,
        BalanceBefore decimal(18, 2) NULL,
        BalanceAfter decimal(18, 2) NULL,
        FailureReason nvarchar(512) NULL,
        ConfirmedAtUtc datetime2 NULL,
        VoucherCode nvarchar(64) NULL,
        TelecomOperationRequestId nvarchar(50) NULL
    );
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomPaymentTransaction_Number')
BEGIN
    CREATE UNIQUE INDEX IX_TelecomPaymentTransaction_Number
        ON dbo.TelecomPaymentTransaction (Number)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TelecomPaymentTransaction_GatewayReference')
BEGIN
    CREATE UNIQUE INDEX UX_TelecomPaymentTransaction_GatewayReference
        ON dbo.TelecomPaymentTransaction (GatewayReference)
        WHERE GatewayReference IS NOT NULL AND IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomPaymentTransaction_CustomerId')
BEGIN
    CREATE INDEX IX_TelecomPaymentTransaction_CustomerId
        ON dbo.TelecomPaymentTransaction (CustomerId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomPaymentTransaction_Msisdn_CreatedAtUtc')
BEGIN
    CREATE INDEX IX_TelecomPaymentTransaction_Msisdn_CreatedAtUtc
        ON dbo.TelecomPaymentTransaction (Msisdn, CreatedAtUtc DESC)
        WHERE Msisdn IS NOT NULL;
END
GO

IF COL_LENGTH('dbo.BillingIntegrationLog', 'TelecomPaymentTransactionId') IS NULL
BEGIN
    ALTER TABLE dbo.BillingIntegrationLog ADD TelecomPaymentTransactionId nvarchar(50) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.BillingIntegrationLog')
      AND name = N'TelecomOperationRequestId'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.BillingIntegrationLog ALTER COLUMN TelecomOperationRequestId nvarchar(50) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BillingIntegrationLog_TelecomPaymentTransactionId')
BEGIN
    CREATE INDEX IX_BillingIntegrationLog_TelecomPaymentTransactionId
        ON dbo.BillingIntegrationLog (TelecomPaymentTransactionId)
        WHERE TelecomPaymentTransactionId IS NOT NULL;
END
GO
