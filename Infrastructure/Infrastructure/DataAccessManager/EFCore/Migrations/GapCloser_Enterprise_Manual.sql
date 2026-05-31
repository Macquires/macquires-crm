IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryBulkImportJob')
BEGIN
    CREATE TABLE InventoryBulkImportJob (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        JobStatus INT NOT NULL,
        TotalRows INT NOT NULL DEFAULT 0,
        ProcessedRows INT NOT NULL DEFAULT 0,
        SuccessCount INT NOT NULL DEFAULT 0,
        ErrorCount INT NOT NULL DEFAULT 0,
        ErrorSummary NVARCHAR(MAX) NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        StartedAtUtc DATETIME2 NULL,
        CompletedAtUtc DATETIME2 NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL
    );
END
