SET QUOTED_IDENTIFIER ON;
GO

DECLARE @DebtMsisdn NVARCHAR(32) = N'0939000002';

DECLARE @AssetId NVARCHAR(450);
DECLARE @ProfileId NVARCHAR(450);

SELECT TOP (1)
    @AssetId = m.Id,
    @ProfileId = m.SubscriberProfileId
FROM dbo.MsisdnAsset m
WHERE m.IsDeleted = 0 AND m.Msisdn = @DebtMsisdn;

IF @AssetId IS NULL
BEGIN
    PRINT N'Skip BDR demo patch: 0939000002 not found.';
END
ELSE IF EXISTS (
    SELECT 1 FROM dbo.TelecomOperationRequest o
    WHERE o.IsDeleted = 0
      AND o.Kind = 12
      AND o.MsisdnAssetId = @AssetId
      AND o.Status = 5)
BEGIN
    PRINT N'BDR PendingDocuments demo already present.';
END
ELSE
BEGIN
    INSERT INTO dbo.TelecomOperationRequest (
        Id, IsDeleted, CreatedAtUtc, Kind, Number, Status, DocumentStatus,
        SubscriberProfileId, MsisdnAssetId, CollectionAction, DunningStage, PriorDunningStage,
        OutstandingBalanceSnapshot, WriteOffAmount, CollectionNote, CollectionSettlementStatus,
        ApprovalLevelRequired, ProvisioningResult, Notes,
        IsLostOrStolenReport, FraudClearanceConfirmed, AutoReconnectEnabled, NotificationSuppressed)
    VALUES (
        NEWID(), 0, SYSUTCDATETIME(), 12,
        N'BDR-DEMO-SEED', 5, 1,
        @ProfileId, @AssetId, N'WriteOffPartial', N'WriteOffPending', N'Reminder2',
        -15000, 10000, N'ديمو BDR SQL patch', N'Pending',
        N'BackOffice', N'Pending', N'BDR|sql-patch',
        0, 0, 0, 0);
    PRINT N'Inserted BDR PendingDocuments demo for 0939000002.';
END
GO
