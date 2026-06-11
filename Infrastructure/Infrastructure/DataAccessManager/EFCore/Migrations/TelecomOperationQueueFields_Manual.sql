SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'SlaExpirationTimeUtc') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD SlaExpirationTimeUtc datetime2(7) NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ClaimedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.TelecomOperationRequest ADD ClaimedByUserId nvarchar(450) NULL;
END
GO

PRINT N'TelecomOperationQueueFields_Manual.sql applied.';
GO
