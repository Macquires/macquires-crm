-- Application Layer: operation audit trail + integration log correlation

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TelecomOperationAuditLog')
BEGIN
    CREATE TABLE TelecomOperationAuditLog (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        TelecomOperationRequestId NVARCHAR(450) NOT NULL,
        FromStatus INT NOT NULL,
        ToStatus INT NOT NULL,
        ActorUserId NVARCHAR(450) NULL,
        Note NVARCHAR(MAX) NULL,
        OccurredAtUtc DATETIME2 NOT NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL
    );
    CREATE INDEX IX_TelecomOperationAuditLog_OperationId ON TelecomOperationAuditLog(TelecomOperationRequestId);
END

IF COL_LENGTH('BillingIntegrationLog', 'CorrelationId') IS NULL
    ALTER TABLE BillingIntegrationLog ADD CorrelationId NVARCHAR(450) NULL;
IF COL_LENGTH('BillingIntegrationLog', 'RequestPayload') IS NULL
    ALTER TABLE BillingIntegrationLog ADD RequestPayload NVARCHAR(MAX) NULL;
IF COL_LENGTH('BillingIntegrationLog', 'ResponsePayload') IS NULL
    ALTER TABLE BillingIntegrationLog ADD ResponsePayload NVARCHAR(MAX) NULL;
