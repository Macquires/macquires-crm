-- Idempotent patch: hero «سعدون الشامي» — MIX_500 on subscription + completed MGR/VAS for KPIs/360.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @HeroMsisdn NVARCHAR(32) = N'0939000001';
DECLARE @MixId NVARCHAR(50);
DECLARE @YaHalaId NVARCHAR(50);
DECLARE @MixProductId NVARCHAR(50);
DECLARE @YaProductId NVARCHAR(50);
DECLARE @MixName NVARCHAR(256);
DECLARE @HeroAssetId NVARCHAR(50);
DECLARE @HeroProfileId NVARCHAR(50);
DECLARE @SubId NVARCHAR(50);
DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();

SELECT @MixId = Id, @MixProductId = ProductId, @MixName = Name
FROM dbo.ProductOffering
WHERE IsDeleted = 0 AND Code = N'MIX_500';

SELECT @YaHalaId = Id, @YaProductId = ProductId
FROM dbo.ProductOffering
WHERE IsDeleted = 0 AND Code = N'YA_HALA_30';

SELECT TOP (1)
    @HeroAssetId = m.Id,
    @HeroProfileId = m.SubscriberProfileId
FROM dbo.MsisdnAsset m
WHERE m.IsDeleted = 0 AND m.Msisdn = @HeroMsisdn;

IF @HeroAssetId IS NULL
BEGIN
    PRINT N'Skip: hero MSISDN not found.';
    RETURN;
END

SELECT TOP (1) @SubId = s.Id
FROM dbo.TelecomSubscription s
WHERE s.IsDeleted = 0 AND s.MsisdnAssetId = @HeroAssetId
ORDER BY s.IsPrimaryLine DESC, s.CreatedAtUtc;

IF @SubId IS NOT NULL AND @MixId IS NOT NULL
BEGIN
    UPDATE dbo.TelecomSubscription
    SET ProductOfferingId = @MixId,
        ProductId = COALESCE(@MixProductId, ProductId),
        UpdatedAtUtc = @NowUtc
    WHERE Id = @SubId;
    PRINT N'Updated hero subscription ProductOfferingId → MIX_500';
END

IF @HeroProfileId IS NOT NULL AND @MixId IS NOT NULL
BEGIN
    UPDATE dbo.TelecomOperationRequest
    SET Status = 3,
        DocumentStatus = 2,
        ProductOfferingId = @MixId,
        ProductId = @MixProductId,
        PriorProductOfferingId = @YaHalaId,
        PriorProductId = @YaProductId,
        TargetOfferName = @MixName,
        ConfirmedAtUtc = DATEADD(MINUTE, 8, @NowUtc),
        CreatedAtUtc = COALESCE(CreatedAtUtc, DATEADD(MINUTE, -12, @NowUtc)),
        Notes = N'ديمو MGR مكتمل: يا هلا → سيريتل ميكس 500 — KPIs وعرض 360.',
        UpdatedAtUtc = @NowUtc
    WHERE IsDeleted = 0
      AND Kind = 1
      AND SubscriberProfileId = @HeroProfileId
      AND (MsisdnAssetId IS NULL OR MsisdnAssetId = @HeroAssetId)
      AND Status IN (0, 1, 2, 5, 6);

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.TelecomOperationRequest (
            Id, IsDeleted, CreatedAtUtc, Kind, Number, Status, DocumentStatus,
            SubscriberProfileId, MsisdnAssetId, ProductOfferingId, ProductId,
            PriorProductOfferingId, PriorProductId, TargetOfferName, ConfirmedAtUtc, Notes)
        VALUES (
            LOWER(CONVERT(NVARCHAR(50), NEWID())), 0, DATEADD(MINUTE, -12, @NowUtc), 1,
            N'MGR-DEMO-' + CONVERT(NVARCHAR(8), @NowUtc, 112), 3, 2,
            @HeroProfileId, @HeroAssetId, @MixId, @MixProductId,
            @YaHalaId, @YaProductId, @MixName, DATEADD(MINUTE, 8, @NowUtc),
            N'ديمو MGR مكتمل: يا هلا → سيريتل ميكس 500 — KPIs وعرض 360.');
        PRINT N'Inserted completed hero MGR operation.';
    END
    ELSE
        PRINT N'Updated hero MGR operation(s) to Completed.';
END

IF @HeroProfileId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM dbo.TelecomOperationRequest
       WHERE IsDeleted = 0 AND Kind = 4 AND SubscriberProfileId = @HeroProfileId
             AND Notes LIKE N'%Activate VAS VAS_CALLER_ID%'
   )
BEGIN
    INSERT INTO dbo.TelecomOperationRequest (
        Id, IsDeleted, CreatedAtUtc, Kind, Number, Status, DocumentStatus,
        SubscriberProfileId, MsisdnAssetId, Notes)
    VALUES (
        LOWER(CONVERT(NVARCHAR(50), NEWID())), 0, DATEADD(MINUTE, -5, @NowUtc), 4,
        N'VAS-DEMO-' + CONVERT(NVARCHAR(8), @NowUtc, 112), 3, 2,
        @HeroProfileId, @HeroAssetId,
        N'Activate VAS VAS_CALLER_ID — ديمو كاشف الأرقام.');
    PRINT N'Inserted completed hero VAS operation.';
END
GO
