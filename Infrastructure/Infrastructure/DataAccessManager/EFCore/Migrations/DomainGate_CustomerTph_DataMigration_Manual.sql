-- Domain Gate: Customer TPH + SubscriberProfile operational columns
-- Run on existing DB after deploying new EF model. Review in non-prod first.

IF COL_LENGTH('Customer', 'CustomerType') IS NULL
BEGIN
    ALTER TABLE [Customer] ADD [CustomerType] int NOT NULL CONSTRAINT DF_Customer_CustomerType DEFAULT 0;
END

IF COL_LENGTH('Customer', 'DisplayName') IS NULL AND COL_LENGTH('Customer', 'Name') IS NOT NULL
BEGIN
    EXEC sp_rename 'Customer.Name', 'DisplayName', 'COLUMN';
END

IF COL_LENGTH('Customer', 'AccountNumber') IS NULL AND COL_LENGTH('Customer', 'Number') IS NOT NULL
BEGIN
    EXEC sp_rename 'Customer.Number', 'AccountNumber', 'COLUMN';
END

-- Backfill identity from SubscriberProfile (legacy)
IF COL_LENGTH('SubscriberProfile', 'NationalId') IS NOT NULL
BEGIN
    UPDATE c SET c.[CustomerType] = 0
    FROM [Customer] c
    INNER JOIN [SubscriberProfile] sp ON sp.CustomerId = c.Id
    WHERE sp.NationalId IS NOT NULL AND c.[CustomerType] IS NULL;

    -- Manual: map NationalId -> IndividualCustomer columns when table split is applied by EF
END

IF COL_LENGTH('SubscriberProfile', 'ServiceLineType') IS NULL
BEGIN
    ALTER TABLE [SubscriberProfile] ADD [ServiceLineType] int NOT NULL CONSTRAINT DF_SP_ServiceLineType DEFAULT 0;
    ALTER TABLE [SubscriberProfile] ADD [OperationalStatus] int NOT NULL CONSTRAINT DF_SP_OpStatus DEFAULT 0;
    ALTER TABLE [SubscriberProfile] ADD [ActivationDateUtc] datetime2 NULL;
END

-- Domain Gate: drop legacy identity columns from SubscriberProfile (after Customer TPH backfill verified)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SubscriberProfile') AND name = N'IX_SubscriberProfile_CommercialRegistration')
    DROP INDEX IX_SubscriberProfile_CommercialRegistration ON dbo.SubscriberProfile;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SubscriberProfile') AND name = N'IX_SubscriberProfile_NationalId')
    DROP INDEX IX_SubscriberProfile_NationalId ON dbo.SubscriberProfile;

IF COL_LENGTH('dbo.SubscriberProfile', 'NationalId') IS NOT NULL
    ALTER TABLE dbo.SubscriberProfile DROP COLUMN NationalId;
IF COL_LENGTH('dbo.SubscriberProfile', 'SubscriberType') IS NOT NULL
    ALTER TABLE dbo.SubscriberProfile DROP COLUMN SubscriberType;
IF COL_LENGTH('dbo.SubscriberProfile', 'DateOfBirth') IS NOT NULL
    ALTER TABLE dbo.SubscriberProfile DROP COLUMN DateOfBirth;
IF COL_LENGTH('dbo.SubscriberProfile', 'CommercialRegistration') IS NOT NULL
    ALTER TABLE dbo.SubscriberProfile DROP COLUMN CommercialRegistration;
IF COL_LENGTH('dbo.SubscriberProfile', 'TaxNumber') IS NOT NULL
    ALTER TABLE dbo.SubscriberProfile DROP COLUMN TaxNumber;
IF COL_LENGTH('dbo.SubscriberProfile', 'AuthorizedSignatory') IS NOT NULL
    ALTER TABLE dbo.SubscriberProfile DROP COLUMN AuthorizedSignatory;
