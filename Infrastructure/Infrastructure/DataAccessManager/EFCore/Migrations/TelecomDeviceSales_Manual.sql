-- §14 Device Sales & Installment (DEV-) — tables + operation fields.

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.DeviceInventory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceInventory (
        Id nvarchar(450) NOT NULL PRIMARY KEY,
        Imei nvarchar(32) NOT NULL,
        Model nvarchar(128) NOT NULL,
        Sku nvarchar(64) NULL,
        ListPrice decimal(18, 2) NOT NULL CONSTRAINT DF_DeviceInventory_ListPrice DEFAULT (0),
        Status int NOT NULL CONSTRAINT DF_DeviceInventory_Status DEFAULT (0),
        BranchId nvarchar(64) NULL,
        ReservedByOperationId nvarchar(450) NULL,
        SoldAtUtc datetime2 NULL,
        RowVersion rowversion NULL,
        CreatedAtUtc datetime2 NULL,
        CreatedById nvarchar(450) NULL,
        UpdatedAtUtc datetime2 NULL,
        UpdatedById nvarchar(450) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_DeviceInventory_IsDeleted DEFAULT (0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeviceInventory_Imei')
BEGIN
    CREATE UNIQUE INDEX IX_DeviceInventory_Imei
        ON dbo.DeviceInventory (Imei)
        WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.InstallmentPlan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InstallmentPlan (
        Id nvarchar(450) NOT NULL PRIMARY KEY,
        Code nvarchar(32) NOT NULL,
        NameAr nvarchar(128) NOT NULL,
        Months int NOT NULL,
        MinDownPaymentPercent decimal(5, 2) NOT NULL,
        InterestRatePercent decimal(5, 2) NOT NULL,
        MinCreditScore int NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_InstallmentPlan_IsActive DEFAULT (1),
        CreatedAtUtc datetime2 NULL,
        CreatedById nvarchar(450) NULL,
        UpdatedAtUtc datetime2 NULL,
        UpdatedById nvarchar(450) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_InstallmentPlan_IsDeleted DEFAULT (0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InstallmentPlan_Code')
BEGIN
    CREATE UNIQUE INDEX IX_InstallmentPlan_Code
        ON dbo.InstallmentPlan (Code)
        WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.DeviceInstallmentContract', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceInstallmentContract (
        Id nvarchar(450) NOT NULL PRIMARY KEY,
        TelecomOperationRequestId nvarchar(450) NOT NULL,
        ContractNumber nvarchar(32) NOT NULL,
        Status int NOT NULL,
        DownPayment decimal(18, 2) NOT NULL,
        MonthlyAmount decimal(18, 2) NOT NULL,
        CreditScoreSnapshot int NULL,
        DelinquencyStatus nvarchar(64) NULL,
        CbsContractId nvarchar(128) NULL,
        WarrantyStartsAtUtc datetime2 NULL,
        InstallmentPlanId nvarchar(450) NULL,
        CreatedAtUtc datetime2 NULL,
        CreatedById nvarchar(450) NULL,
        UpdatedAtUtc datetime2 NULL,
        UpdatedById nvarchar(450) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_DeviceInstallmentContract_IsDeleted DEFAULT (0)
    );
END
GO

IF OBJECT_ID(N'dbo.DeviceInstallmentScheduleLine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceInstallmentScheduleLine (
        Id nvarchar(450) NOT NULL PRIMARY KEY,
        DeviceInstallmentContractId nvarchar(450) NOT NULL,
        Sequence int NOT NULL,
        DueDateUtc datetime2 NOT NULL,
        Amount decimal(18, 2) NOT NULL,
        Status int NOT NULL,
        PaidAtUtc datetime2 NULL,
        PaymentReference nvarchar(128) NULL,
        CreatedAtUtc datetime2 NULL,
        CreatedById nvarchar(450) NULL,
        UpdatedAtUtc datetime2 NULL,
        UpdatedById nvarchar(450) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_DeviceInstallmentScheduleLine_IsDeleted DEFAULT (0)
    );
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceInventoryId') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceInventoryId nvarchar(450) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceSaleType') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceSaleType int NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'InstallmentPlanId') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD InstallmentPlanId nvarchar(450) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceDownPaymentAmount') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceDownPaymentAmount decimal(18, 2) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceMonthlyInstallmentAmount') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceMonthlyInstallmentAmount decimal(18, 2) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceCreditScoreSnapshot') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceCreditScoreSnapshot int NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceInstallmentContractId') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceInstallmentContractId nvarchar(450) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceFinancingDecision') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceFinancingDecision int NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceOverrideReasonCode') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceOverrideReasonCode nvarchar(64) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceApprovalLevelRequired') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceApprovalLevelRequired nvarchar(64) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceFinancingNoteAr') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceFinancingNoteAr nvarchar(512) NULL;
GO
IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceWarrantyStartsAtUtc') IS NULL
    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceWarrantyStartsAtUtc datetime2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_DeviceSale')
BEGIN
    CREATE INDEX IX_TelecomOperationRequest_DeviceSale
        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)
        WHERE Kind = 10 AND IsDeleted = 0;
END
GO

-- Seed default installment plans (idempotent by Code)
IF NOT EXISTS (SELECT 1 FROM dbo.InstallmentPlan WHERE Code = N'INST-6' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.InstallmentPlan (Id, Code, NameAr, Months, MinDownPaymentPercent, InterestRatePercent, MinCreditScore, IsActive, IsDeleted, CreatedAtUtc)
    VALUES (NEWID(), N'INST-6', N'تقسيط 6 أشهر', 6, 20.00, 0.00, 550, 1, 0, SYSUTCDATETIME());
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.InstallmentPlan WHERE Code = N'INST-12' AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.InstallmentPlan (Id, Code, NameAr, Months, MinDownPaymentPercent, InterestRatePercent, MinCreditScore, IsActive, IsDeleted, CreatedAtUtc)
    VALUES (NEWID(), N'INST-12', N'تقسيط 12 شهراً', 12, 15.00, 2.50, 500, 1, 0, SYSUTCDATETIME());
END
GO
