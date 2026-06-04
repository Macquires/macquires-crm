-- SIM Swap (§4) — SIM- operation security fields.

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'ReplacementReason') IS NULL

BEGIN

    ALTER TABLE dbo.TelecomOperationRequest ADD ReplacementReason nvarchar(256) NULL;

END

GO



IF COL_LENGTH('dbo.TelecomOperationRequest', 'IsLostOrStolenReport') IS NULL

BEGIN

    ALTER TABLE dbo.TelecomOperationRequest ADD IsLostOrStolenReport bit NOT NULL
        CONSTRAINT DF_TelecomOp_IsLostOrStolen DEFAULT 0;

END

GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest')
      AND name = N'IsLostOrStolenReport' AND is_nullable = 1)
BEGIN
    UPDATE dbo.TelecomOperationRequest SET IsLostOrStolenReport = 0 WHERE IsLostOrStolenReport IS NULL;
    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN IsLostOrStolenReport bit NOT NULL;
END

GO



IF COL_LENGTH('dbo.TelecomOperationRequest', 'PriorSimInventoryId') IS NULL

BEGIN

    ALTER TABLE dbo.TelecomOperationRequest ADD PriorSimInventoryId nvarchar(50) NULL;

END

GO



SET QUOTED_IDENTIFIER ON;

GO



IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_SimSwap')

BEGIN

    CREATE INDEX IX_TelecomOperationRequest_SimSwap

        ON dbo.TelecomOperationRequest (Kind, CreatedAtUtc DESC)

        WHERE Kind = 3 AND IsDeleted = 0;

END

GO


