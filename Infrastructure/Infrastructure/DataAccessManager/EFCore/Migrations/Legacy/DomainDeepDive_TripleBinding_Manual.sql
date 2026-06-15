-- Domain Deep Dive: Triple-Binding + lifecycle (run after backup)

IF COL_LENGTH('MsisdnAsset', 'CountryCode') IS NULL
    ALTER TABLE MsisdnAsset ADD CountryCode NVARCHAR(8) NULL;
IF COL_LENGTH('MsisdnAsset', 'Prefix') IS NULL
    ALTER TABLE MsisdnAsset ADD Prefix NVARCHAR(8) NULL;
IF COL_LENGTH('MsisdnAsset', 'ReservedUntilUtc') IS NULL
    ALTER TABLE MsisdnAsset ADD ReservedUntilUtc DATETIME2 NULL;
IF COL_LENGTH('MsisdnAsset', 'ReservedForCustomerId') IS NULL
    ALTER TABLE MsisdnAsset ADD ReservedForCustomerId NVARCHAR(450) NULL;

IF COL_LENGTH('SimInventory', 'SimType') IS NULL
    ALTER TABLE SimInventory ADD SimType INT NOT NULL CONSTRAINT DF_SimInventory_SimType DEFAULT 0;
IF COL_LENGTH('SimInventory', 'Eid') IS NULL
    ALTER TABLE SimInventory ADD Eid NVARCHAR(64) NULL;
IF COL_LENGTH('SimInventory', 'ActivationCode') IS NULL
    ALTER TABLE SimInventory ADD ActivationCode NVARCHAR(64) NULL;

IF COL_LENGTH('TelecomOperationRequest', 'CorrelationId') IS NULL
    ALTER TABLE TelecomOperationRequest ADD CorrelationId NVARCHAR(450) NULL;
IF COL_LENGTH('TelecomOperationRequest', 'SimInventoryId') IS NULL
    ALTER TABLE TelecomOperationRequest ADD SimInventoryId NVARCHAR(450) NULL;

IF COL_LENGTH('Customer', 'StatusReasonCode') IS NULL
    ALTER TABLE Customer ADD StatusReasonCode INT NOT NULL CONSTRAINT DF_Customer_StatusReasonCode DEFAULT 0;
IF COL_LENGTH('Customer', 'StatusReasonNote') IS NULL
    ALTER TABLE Customer ADD StatusReasonNote NVARCHAR(MAX) NULL;

IF COL_LENGTH('Customer', 'ParentCustomerId') IS NULL
    ALTER TABLE Customer ADD ParentCustomerId NVARCHAR(450) NULL;
IF COL_LENGTH('Customer', 'BillingConsolidationMode') IS NULL
    ALTER TABLE Customer ADD BillingConsolidationMode INT NOT NULL CONSTRAINT DF_Customer_BillingConsolidation DEFAULT 0;

IF COL_LENGTH('Customer', 'NationalIdSearchHash') IS NULL
    ALTER TABLE Customer ADD NationalIdSearchHash NVARCHAR(64) NULL;

IF COL_LENGTH('SubscriberProfile', 'LanguagePreference') IS NULL
    ALTER TABLE SubscriberProfile ADD LanguagePreference INT NOT NULL CONSTRAINT DF_SubscriberProfile_Lang DEFAULT 0;

IF COL_LENGTH('PricePlan', 'PricePerMinute') IS NULL
    ALTER TABLE PricePlan ADD PricePerMinute DECIMAL(18,4) NULL;
IF COL_LENGTH('PricePlan', 'PricePerMegabyte') IS NULL
    ALTER TABLE PricePlan ADD PricePerMegabyte DECIMAL(18,4) NULL;
IF COL_LENGTH('PricePlan', 'PricePerSms') IS NULL
    ALTER TABLE PricePlan ADD PricePerSms DECIMAL(18,4) NULL;

IF COL_LENGTH('ProductOffering', 'PaymentType') IS NULL
    ALTER TABLE ProductOffering ADD PaymentType INT NULL;
IF COL_LENGTH('ProductOffering', 'BillingCycleEnum') IS NULL
    ALTER TABLE ProductOffering ADD BillingCycleEnum INT NULL;

IF COL_LENGTH('ProductOfferingComponent', 'RequiresProductOfferingId') IS NULL
    ALTER TABLE ProductOfferingComponent ADD RequiresProductOfferingId NVARCHAR(450) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomerIdentityDocument')
BEGIN
    CREATE TABLE CustomerIdentityDocument (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        CustomerId NVARCHAR(450) NOT NULL,
        DocumentType INT NOT NULL,
        DocumentNumber NVARCHAR(64) NOT NULL,
        ExpiresUtc DATETIME2 NULL,
        FileDocumentId NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomOperationRequest_CorrelationId')
    CREATE INDEX IX_TelecomOperationRequest_CorrelationId ON TelecomOperationRequest(CorrelationId) WHERE CorrelationId IS NOT NULL;
