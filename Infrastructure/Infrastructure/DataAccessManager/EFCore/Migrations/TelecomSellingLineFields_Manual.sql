-- Selling Line / New Activation — Blueprint fields (pages 17, 22).
-- Run after deploying build that includes Domain + EF configuration updates.

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ActivationChannel') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ActivationChannel int NOT NULL
        CONSTRAINT DF_TelecomOperationRequest_ActivationChannel DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DealerCode') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DealerCode nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'BranchId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD BranchId nvarchar(450) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PaymentReference') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PaymentReference nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'InitialDepositAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD InitialDepositAmount decimal(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'OverrideReasonCode') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD OverrideReasonCode nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'KycVerifiedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD KycVerifiedAtUtc datetime2 NULL;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_ActivationChannel')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_ActivationChannel
        ON dbo.TelecomOperationRequest (ActivationChannel);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_DealerCode')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_DealerCode
        ON dbo.TelecomOperationRequest (DealerCode)
        WHERE DealerCode IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_BranchId')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_BranchId
        ON dbo.TelecomOperationRequest (BranchId)
        WHERE BranchId IS NOT NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationAuditLog', 'ActivationChannel') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationAuditLog ADD ActivationChannel int NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationAuditLog', 'BranchId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationAuditLog ADD BranchId nvarchar(450) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationAuditLog', 'DealerCode') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationAuditLog ADD DealerCode nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationAuditLog', 'OverrideReasonCode') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationAuditLog ADD OverrideReasonCode nvarchar(64) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationAuditLog', 'CorrelationId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationAuditLog ADD CorrelationId nvarchar(450) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationAuditLog', 'FieldChangesJson') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationAuditLog ADD FieldChangesJson nvarchar(max) NULL;
END
GO
