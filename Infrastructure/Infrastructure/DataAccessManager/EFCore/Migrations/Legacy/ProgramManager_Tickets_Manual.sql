-- Network tickets (ProgramManager) — optional manual run; app also applies schema patch on startup.
IF OBJECT_ID(N'[dbo].[ProgramManagerResource]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProgramManagerResource] (
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(255) NULL,
        [Description] NVARCHAR(4000) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [CreatedAtUtc] DATETIME2 NULL,
        [CreatedById] NVARCHAR(450) NULL,
        [UpdatedAtUtc] DATETIME2 NULL,
        [UpdatedById] NVARCHAR(450) NULL
    );
    CREATE INDEX [IX_ProgramManagerResource_Name] ON [dbo].[ProgramManagerResource]([Name]);
END

IF OBJECT_ID(N'[dbo].[ProgramManager]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProgramManager] (
        [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [Title] NVARCHAR(255) NULL,
        [Number] NVARCHAR(50) NULL,
        [Summary] NVARCHAR(4000) NULL,
        [Status] INT NULL,
        [Priority] INT NULL,
        [ProgramManagerResourceId] NVARCHAR(50) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [CreatedAtUtc] DATETIME2 NULL,
        [CreatedById] NVARCHAR(450) NULL,
        [UpdatedAtUtc] DATETIME2 NULL,
        [UpdatedById] NVARCHAR(450) NULL,
        CONSTRAINT [FK_ProgramManager_ProgramManagerResource] FOREIGN KEY ([ProgramManagerResourceId])
            REFERENCES [dbo].[ProgramManagerResource]([Id])
    );
    CREATE INDEX [IX_ProgramManager_Title] ON [dbo].[ProgramManager]([Title]);
    CREATE INDEX [IX_ProgramManager_Number] ON [dbo].[ProgramManager]([Number]);
END

PRINT 'ProgramManager_Tickets_Manual.sql completed.';
