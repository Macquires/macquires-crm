-- Manual migration: ProgramManager -> TelecomTechnicalTicket (run once before dropping legacy tables)
IF OBJECT_ID(N'dbo.TelecomTechnicalTicket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelecomTechnicalTicket (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL,
        TicketNumber NVARCHAR(64) NOT NULL,
        Msisdn NVARCHAR(32) NOT NULL,
        CustomerId NVARCHAR(450) NULL,
        SubscriberProfileId NVARCHAR(450) NULL,
        IssueType INT NOT NULL DEFAULT 0,
        Priority INT NOT NULL DEFAULT 1,
        Status INT NOT NULL DEFAULT 0,
        AssignedToGroupId NVARCHAR(450) NULL,
        OpenedByUserId NVARCHAR(450) NOT NULL,
        ResolvedByUserId NVARCHAR(450) NULL,
        Notes NVARCHAR(MAX) NULL,
        ResolutionNotes NVARCHAR(MAX) NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        ResolvedAtUtc DATETIME2 NULL
    );
    CREATE UNIQUE INDEX IX_TelecomTechnicalTicket_TicketNumber ON dbo.TelecomTechnicalTicket(TicketNumber);
    CREATE INDEX IX_TelecomTechnicalTicket_Msisdn ON dbo.TelecomTechnicalTicket(Msisdn);
    CREATE INDEX IX_TelecomTechnicalTicket_Status ON dbo.TelecomTechnicalTicket(Status);
END
GO

IF OBJECT_ID(N'dbo.ProgramManager', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.TelecomTechnicalTicket)
BEGIN
    INSERT INTO dbo.TelecomTechnicalTicket (
        Id, IsDeleted, CreatedAtUtc, CreatedById, UpdatedAtUtc, UpdatedById,
        TicketNumber, Msisdn, IssueType, Priority, Status, OpenedByUserId, Notes, PayloadJson)
    SELECT
        NEWID(),
        pm.IsDeleted,
        pm.CreatedAtUtc,
        pm.CreatedById,
        pm.UpdatedAtUtc,
        pm.UpdatedById,
        COALESCE(pm.Number, CONCAT('TT-LEG-', LEFT(pm.Id, 8))),
        COALESCE(
            (SELECT TOP 1 SUBSTRING(pm.Summary, PATINDEX('%09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]%', pm.Summary), 10)
             WHERE pm.Summary LIKE '%09[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]%'),
            '0900000000'),
        CASE
            WHEN pm.Summary LIKE N'%فاتورة%' OR pm.Summary LIKE N'%billing%' THEN 1
            WHEN pm.Summary LIKE N'%شريحة%' OR pm.Summary LIKE N'%SIM%' THEN 2
            WHEN pm.Summary LIKE N'%تفعيل%' OR pm.Summary LIKE N'%provision%' THEN 3
            ELSE 0
        END,
        COALESCE(pm.Priority, 1),
        CASE pm.Status
            WHEN 2 THEN 1
            WHEN 3 THEN 1
            WHEN 4 THEN 2
            ELSE 0
        END,
        COALESCE(pm.CreatedById, 'legacy-migration'),
        pm.Title,
        CONCAT(N'{"legacyTitle":"', REPLACE(ISNULL(pm.Title,''), '"', ''''), N'","summary":"', REPLACE(ISNULL(pm.Summary,''), '"', ''''), N'"}')
    FROM dbo.ProgramManager pm;
END
GO
