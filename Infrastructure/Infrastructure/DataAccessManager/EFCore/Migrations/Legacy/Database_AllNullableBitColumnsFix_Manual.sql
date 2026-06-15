-- Coalesce NULL on every user-table nullable bit column (SqlNullValueException prevention).
-- Run once per database after manual ALTER ADD ... bit NULL migrations.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @sql nvarchar(max) = N'';

SELECT @sql = @sql + N'
UPDATE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
    + N' SET ' + QUOTENAME(c.name) + N' = '
    + CASE WHEN c.name = N'IsActive' THEN N'1' ELSE N'0' END
    + N' WHERE ' + QUOTENAME(c.name) + N' IS NULL;'
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE c.system_type_id = 104
  AND c.is_nullable = 1
  AND t.is_ms_shipped = 0
  AND t.type = N'U';

IF LEN(@sql) > 0
BEGIN
    PRINT N'Applying nullable bit coalesce across user tables...';
    EXEC sp_executesql @sql;
END
ELSE
    PRINT N'No nullable bit columns found.';
GO

-- TelecomOperationRequest: enforce NOT NULL on known flags (idempotent)
IF COL_LENGTH('dbo.TelecomOperationRequest', 'IsLostOrStolenReport') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest')
         AND name = N'IsLostOrStolenReport' AND is_nullable = 1)
BEGIN
    UPDATE dbo.TelecomOperationRequest SET IsLostOrStolenReport = 0 WHERE IsLostOrStolenReport IS NULL;
    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN IsLostOrStolenReport bit NOT NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'AutoReconnectEnabled') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest') AND name = N'AutoReconnectEnabled' AND is_nullable = 1)
BEGIN
    UPDATE dbo.TelecomOperationRequest SET AutoReconnectEnabled = 0 WHERE AutoReconnectEnabled IS NULL;
    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN AutoReconnectEnabled bit NOT NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'NotificationSuppressed') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest') AND name = N'NotificationSuppressed' AND is_nullable = 1)
BEGIN
    UPDATE dbo.TelecomOperationRequest SET NotificationSuppressed = 0 WHERE NotificationSuppressed IS NULL;
    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN NotificationSuppressed bit NOT NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'FraudClearanceConfirmed') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest') AND name = N'FraudClearanceConfirmed' AND is_nullable = 1)
BEGIN
    UPDATE dbo.TelecomOperationRequest SET FraudClearanceConfirmed = 0 WHERE FraudClearanceConfirmed IS NULL;
    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN FraudClearanceConfirmed bit NOT NULL;
END
GO

IF COL_LENGTH('dbo.TelecomOperationRequest', 'RequiresDualApproval') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest') AND name = N'RequiresDualApproval' AND is_nullable = 1)
BEGIN
    UPDATE dbo.TelecomOperationRequest SET RequiresDualApproval = 0 WHERE RequiresDualApproval IS NULL;
    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN RequiresDualApproval bit NOT NULL;
END
GO

PRINT N'Database_AllNullableBitColumnsFix_Manual.sql completed.';
GO
