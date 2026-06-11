-- GeoCity — Syrian city reference data (idempotent)
IF OBJECT_ID(N'dbo.GeoCity', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeoCity (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        IsDeleted BIT NOT NULL CONSTRAINT DF_GeoCity_IsDeleted DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL,
        Name NVARCHAR(255) NULL,
        Governorate NVARCHAR(255) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_GeoCity_IsActive DEFAULT 1,
        SortOrder INT NOT NULL CONSTRAINT DF_GeoCity_SortOrder DEFAULT 0
    );
    CREATE INDEX IX_GeoCity_Name_Governorate ON dbo.GeoCity(Name, Governorate);
    CREATE INDEX IX_GeoCity_Active_Sort ON dbo.GeoCity(IsActive, SortOrder);
END
GO
