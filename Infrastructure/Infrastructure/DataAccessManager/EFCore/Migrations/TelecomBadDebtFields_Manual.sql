SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'CollectionAction') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD CollectionAction nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'DunningStage') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD DunningStage nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorDunningStage') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PriorDunningStage nvarchar(32) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'OutstandingBalanceSnapshot') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD OutstandingBalanceSnapshot decimal(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'CollectedAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD CollectedAmount decimal(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'WriteOffAmount') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD WriteOffAmount decimal(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'AgencyReference') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD AgencyReference nvarchar(128) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'PaymentPlanMonths') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD PaymentPlanMonths int NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'NextDunningDueUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD NextDunningDueUtc datetime2 NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'CollectionNote') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD CollectionNote nvarchar(512) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'CollectionSettlementStatus') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD CollectionSettlementStatus nvarchar(32) NULL;
END
GO

PRINT N'TelecomBadDebtFields_Manual.sql applied.';
GO
