-- DashboardWidgets — dynamic Bento cockpit metadata (idempotent)
IF OBJECT_ID(N'dbo.DashboardWidget', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DashboardWidget (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        IsDeleted BIT NOT NULL CONSTRAINT DF_DashboardWidget_IsDeleted DEFAULT 0,
        CreatedAtUtc DATETIME2 NULL,
        CreatedById NVARCHAR(450) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedById NVARCHAR(450) NULL,
        WidgetKey NVARCHAR(256) NOT NULL,
        TitleAr NVARCHAR(256) NOT NULL,
        TitleEn NVARCHAR(256) NULL,
        Icon NVARCHAR(64) NULL,
        ProviderKey NVARCHAR(256) NULL,
        PersonasAllowed NVARCHAR(256) NOT NULL,
        GridSize INT NOT NULL CONSTRAINT DF_DashboardWidget_GridSize DEFAULT 6,
        SortOrder INT NOT NULL CONSTRAINT DF_DashboardWidget_SortOrder DEFAULT 0,
        WidgetKind INT NOT NULL CONSTRAINT DF_DashboardWidget_WidgetKind DEFAULT 0,
        RefreshIntervalSeconds INT NULL,
        CtaUrl NVARCHAR(512) NULL,
        CtaLabelAr NVARCHAR(256) NULL,
        CtaLabelEn NVARCHAR(256) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DashboardWidget_IsActive DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_DashboardWidget_WidgetKey ON dbo.DashboardWidget(WidgetKey) WHERE IsDeleted = 0;
    CREATE INDEX IX_DashboardWidget_Active_Sort ON dbo.DashboardWidget(IsActive, SortOrder);
END
GO
