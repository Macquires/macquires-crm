-- Enterprise bulk import: extend job table + error detail table (idempotent)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryBulkImportJob')
BEGIN
    CREATE TABLE dbo.InventoryBulkImportJob (
        Id NVARCHAR(50) NOT NULL PRIMARY KEY,
        JobStatus INT NOT NULL CONSTRAINT DF_InvBulkJob_Status DEFAULT 0,
        JobType INT NOT NULL CONSTRAINT DF_InvBulkJob_Type DEFAULT 0,
        FileName NVARCHAR(512) NULL,
        StoredFilePath NVARCHAR(1024) NULL,
        TotalRows INT NOT NULL CONSTRAINT DF_InvBulkJob_Total DEFAULT 0,
        ProcessedRows INT NOT NULL CONSTRAINT DF_InvBulkJob_Processed DEFAULT 0,
        SuccessCount INT NOT NULL CONSTRAINT DF_InvBulkJob_Success DEFAULT 0,
        ErrorCount INT NOT NULL CONSTRAINT DF_InvBulkJob_Errors DEFAULT 0,
        ErrorSummary NVARCHAR(4000) NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        StartedAtUtc DATETIME2 NULL,
        CompletedAtUtc DATETIME2 NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_InvBulkJob_IsDeleted DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(50) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(50) NULL
    );
    CREATE INDEX IX_InvBulkJob_Status ON dbo.InventoryBulkImportJob (JobStatus);
    CREATE INDEX IX_InvBulkJob_Type ON dbo.InventoryBulkImportJob (JobType);
    CREATE INDEX IX_InvBulkJob_CreatedBy ON dbo.InventoryBulkImportJob (CreatedById);
    CREATE INDEX IX_InvBulkJob_CreatedAt ON dbo.InventoryBulkImportJob (CreatedAtUtc);
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.InventoryBulkImportJob', 'JobType') IS NULL
        ALTER TABLE dbo.InventoryBulkImportJob ADD JobType INT NOT NULL CONSTRAINT DF_InvBulkJob_Type_Existing DEFAULT 0;
    IF COL_LENGTH('dbo.InventoryBulkImportJob', 'FileName') IS NULL
        ALTER TABLE dbo.InventoryBulkImportJob ADD FileName NVARCHAR(512) NULL;
    IF COL_LENGTH('dbo.InventoryBulkImportJob', 'StoredFilePath') IS NULL
        ALTER TABLE dbo.InventoryBulkImportJob ADD StoredFilePath NVARCHAR(1024) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvBulkJob_Type' AND object_id = OBJECT_ID('dbo.InventoryBulkImportJob'))
        CREATE INDEX IX_InvBulkJob_Type ON dbo.InventoryBulkImportJob (JobType);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvBulkJob_CreatedBy' AND object_id = OBJECT_ID('dbo.InventoryBulkImportJob'))
        CREATE INDEX IX_InvBulkJob_CreatedBy ON dbo.InventoryBulkImportJob (CreatedById);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvBulkJob_CreatedAt' AND object_id = OBJECT_ID('dbo.InventoryBulkImportJob'))
        CREATE INDEX IX_InvBulkJob_CreatedAt ON dbo.InventoryBulkImportJob (CreatedAtUtc);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryBulkImportError')
BEGIN
    CREATE TABLE dbo.InventoryBulkImportError (
        Id NVARCHAR(50) NOT NULL PRIMARY KEY,
        JobId NVARCHAR(50) NOT NULL,
        RowNumber INT NOT NULL,
        Identifier NVARCHAR(128) NULL,
        ErrorMessageAr NVARCHAR(2000) NULL,
        ErrorMessageEn NVARCHAR(2000) NULL,
        RawRowDataJson NVARCHAR(4000) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_InvBulkErr_IsDeleted DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(50) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(50) NULL,
        CONSTRAINT FK_InvBulkErr_Job FOREIGN KEY (JobId) REFERENCES dbo.InventoryBulkImportJob(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_InvBulkErr_JobId ON dbo.InventoryBulkImportError (JobId);
    CREATE INDEX IX_InvBulkErr_JobRow ON dbo.InventoryBulkImportError (JobId, RowNumber);
END
