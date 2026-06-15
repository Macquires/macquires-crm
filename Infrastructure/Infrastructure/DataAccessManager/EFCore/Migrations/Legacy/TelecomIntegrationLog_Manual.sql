-- Run once on the application database (idempotent).
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.TelecomIntegrationLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelecomIntegrationLog (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        IsDeleted BIT NOT NULL CONSTRAINT DF_TelecomIntegrationLog_IsDeleted DEFAULT (0),
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL,
        Msisdn NVARCHAR(256) NULL,
        IntegrationSystem INT NOT NULL,
        OperationName NVARCHAR(256) NOT NULL,
        RequestPayload NVARCHAR(MAX) NULL,
        ResponsePayload NVARCHAR(MAX) NULL,
        ExecutionTimeMs BIGINT NOT NULL,
        IsSuccess BIT NOT NULL,
        ResponseStatusCode NVARCHAR(64) NULL,
        OccurredAtUtc DATETIME2 NOT NULL
    );

    CREATE INDEX IX_TelecomIntegrationLog_Msisdn ON dbo.TelecomIntegrationLog (Msisdn) WHERE Msisdn IS NOT NULL;
    CREATE INDEX IX_TelecomIntegrationLog_IntegrationSystem ON dbo.TelecomIntegrationLog (IntegrationSystem);
    CREATE INDEX IX_TelecomIntegrationLog_OccurredAtUtc ON dbo.TelecomIntegrationLog (OccurredAtUtc DESC);
END
GO
