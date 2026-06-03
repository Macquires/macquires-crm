-- §15 Deposit / wallet refund (RFD-) — operation fields.

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundType') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundType nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundReason') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundReason nvarchar(256) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundAmount decimal(18, 2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundMethod') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundMethod nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DepositBalanceSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DepositBalanceSnapshot decimal(18, 2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'WalletBalanceSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD WalletBalanceSnapshot decimal(18, 2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundSettlementStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundSettlementStatus nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundCbsReference') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundCbsReference nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundGatewayReference') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RefundGatewayReference nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RequiresDualApproval') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD RequiresDualApproval bit NOT NULL CONSTRAINT DF_TelecomOp_RefundDualApproval DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_Refund')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_Refund
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 11 AND IsDeleted = 0;
END
GO
