/*
  Allow multiple SubscriberProfile rows per CRM Customer (one party → many telecom profiles).
  Earlier EF configuration used a UNIQUE index on SubscriberProfile.CustomerId.

  Run on SQL Server when upgrading an existing database created before this change.
*/
IF OBJECT_ID(N'dbo.SubscriberProfile', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
          AND i.name = N'IX_SubscriberProfile_CustomerId'
          AND i.is_unique = 1)
    BEGIN
        DROP INDEX IX_SubscriberProfile_CustomerId ON dbo.SubscriberProfile;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
          AND i.name = N'IX_SubscriberProfile_CustomerId')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_SubscriberProfile_CustomerId
        ON dbo.SubscriberProfile ([CustomerId]);
    END
END
