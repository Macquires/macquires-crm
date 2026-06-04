-- Idempotent §9 RCN demo state (updates only — full fraud line 0939000091 via EnsureHeroReconnectDemoAsync on app start).
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();
DECLARE @DebtMsisdn NVARCHAR(32) = N'0939000002';
DECLARE @DebtAssetId NVARCHAR(50);
DECLARE @DebtProfileId NVARCHAR(50);

SELECT TOP (1)
    @DebtAssetId = m.Id,
    @DebtProfileId = m.SubscriberProfileId
FROM dbo.MsisdnAsset m
WHERE m.IsDeleted = 0 AND m.Msisdn = @DebtMsisdn;

IF @DebtProfileId IS NOT NULL
BEGIN
    UPDATE dbo.SubscriberProfile
    SET OperationalStatus = 2,
        UpdatedAtUtc = @NowUtc
    WHERE Id = @DebtProfileId AND OperationalStatus <> 3;

    UPDATE dbo.MsisdnAsset
    SET PoolStatus = 3,
        UpdatedAtUtc = @NowUtc
    WHERE Id = @DebtAssetId AND PoolStatus <> 3;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.TelecomOperationRequest
        WHERE IsDeleted = 0 AND Kind = 8 AND MsisdnAssetId = @DebtAssetId
          AND Status = 3 AND SuspensionType = N'Billing')
    BEGIN
        INSERT INTO dbo.TelecomOperationRequest (
            Id, IsDeleted, CreatedAtUtc, Kind, Number, Status, DocumentStatus,
            SubscriberProfileId, MsisdnAssetId, SuspensionType, SuspensionReason,
            BarringLevel, SuspensionStartDateUtc, BarStatus, ConfirmedAtUtc, Notes)
        VALUES (
            LOWER(CONVERT(NVARCHAR(50), NEWID())), 0, DATEADD(DAY, -15, @NowUtc), 8,
            N'SUS-DEMO-BILL-' + RIGHT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 8),
            3, 2, @DebtProfileId, @DebtAssetId, N'Billing',
            N'ديمو RCN: حظر فواتير — مسار Payment clearance.', N'Full',
            DATEADD(DAY, -14, @NowUtc), N'Active', DATEADD(DAY, -14, @NowUtc),
            N'ديمو RCN: خط موقوف فواتير — مسار تسوية Payment بالمعرض.');
        PRINT N'Inserted completed Billing SUS for 0939000002.';
    END
    ELSE
        PRINT N'Debt demo line (0939000002) already suspended with Billing SUS.';
END
ELSE
    PRINT N'Skip: 0939000002 not found — run TelecomSyriatelSeeder first.';

-- If fraud demo line already exists (from app seed), ensure suspended + Fraud SUS
DECLARE @FraudMsisdn NVARCHAR(32) = N'0939000091';
DECLARE @FraudAssetId NVARCHAR(50);
DECLARE @FraudProfileId NVARCHAR(50);

SELECT TOP (1)
    @FraudAssetId = m.Id,
    @FraudProfileId = m.SubscriberProfileId
FROM dbo.MsisdnAsset m
WHERE m.IsDeleted = 0 AND m.Msisdn = @FraudMsisdn;

IF @FraudProfileId IS NOT NULL
BEGIN
    UPDATE dbo.SubscriberProfile
    SET OperationalStatus = 2, UpdatedAtUtc = @NowUtc
    WHERE Id = @FraudProfileId AND OperationalStatus <> 3;

    UPDATE dbo.MsisdnAsset
    SET PoolStatus = 3, UpdatedAtUtc = @NowUtc
    WHERE Id = @FraudAssetId;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.TelecomOperationRequest
        WHERE IsDeleted = 0 AND Kind = 8 AND MsisdnAssetId = @FraudAssetId
          AND Status = 3 AND SuspensionType = N'Fraud')
    BEGIN
        INSERT INTO dbo.TelecomOperationRequest (
            Id, IsDeleted, CreatedAtUtc, Kind, Number, Status, DocumentStatus,
            SubscriberProfileId, MsisdnAssetId, SuspensionType, SuspensionReason,
            BarringLevel, SuspensionStartDateUtc, BarStatus, ConfirmedAtUtc,
            ApprovalLevelRequired, Notes)
        VALUES (
            LOWER(CONVERT(NVARCHAR(50), NEWID())), 0, DATEADD(DAY, -20, @NowUtc), 8,
            N'SUS-DEMO-FRD-' + RIGHT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 8),
            3, 2, @FraudProfileId, @FraudAssetId, N'Fraud',
            N'ديمو RCN: خط موقوف احتيال.', N'Full',
            DATEADD(DAY, -19, @NowUtc), N'Active', DATEADD(DAY, -19, @NowUtc), N'BackOffice',
            N'ديمو RCN: خط موقوف احتيال — BO queue.');
        PRINT N'Inserted Fraud SUS for 0939000091.';
    END
END
ELSE
    PRINT N'Note: 0939000091 not found — start app once to run EnsureHeroReconnectDemoAsync.';

PRINT N'RCN hero demo patch complete.';
GO
