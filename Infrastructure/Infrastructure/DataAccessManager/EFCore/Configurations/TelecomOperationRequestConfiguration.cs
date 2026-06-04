using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomOperationRequestConfiguration : BaseEntityConfiguration<TelecomOperationRequest>
{
    public override void Configure(EntityTypeBuilder<TelecomOperationRequest> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.SimInventoryId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.SecondarySubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.MsisdnAssetId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProductOfferingId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.TargetOfferName).HasMaxLength(LengthConsts.M).IsRequired(false);
        builder.Property(x => x.IdentityDocumentStorageKey).HasMaxLength(LengthConsts.L).IsRequired(false);
        builder.Property(x => x.ActivationChannel).HasConversion<int>();
        builder.Property(x => x.DealerCode).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.BranchId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PaymentReference).HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.InitialDepositAmount).HasPrecision(18, 2);
        builder.Property(x => x.OverrideReasonCode).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.SourceSubscriptionTypeId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.TargetSubscriptionTypeId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.GsmMigrationReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.GsmCompatibilityStatus).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.TransferReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.TakeOverObligationStatus).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.DepositTransferPolicy).HasConversion<int?>();
        builder.Property(x => x.ApprovalLevelRequired).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.OldCustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.NewCustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PriorSubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ReplacementReason).HasMaxLength(256).IsRequired(false);
        // Required + no EF store default: HasDefaultValue(false) with NullableBit converter caused NULL on INSERT.
        builder.Property(x => x.IsLostOrStolenReport).IsRequired();
        builder.Property(x => x.AutoReconnectEnabled).IsRequired();
        builder.Property(x => x.NotificationSuppressed).IsRequired();
        builder.Property(x => x.FraudClearanceConfirmed).IsRequired();
        builder.Property(x => x.PriorSimInventoryId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PriorMsisdnAssetId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.TargetMsisdnAssetId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.NumberChangeReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.PremiumFeeAmount).HasPrecision(18, 2);
        builder.Property(x => x.NumberChangeMode).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.TerminationType).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.TerminationReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.DepositSettlementStatus).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.RetentionOfferOutcome).HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.DeprovisionStatus).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.FinalBillAmount).HasPrecision(18, 2);
        builder.Property(x => x.DepositSettlementAmount).HasPrecision(18, 2);
        builder.Property(x => x.SuspensionType).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.SuspensionReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.BarringLevel).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.BarStatus).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.PriorOperationalStatus).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.ReconnectReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.ClearanceType).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.SourceSuspensionOperationId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.FraudClearanceByUserId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProvisioningResult).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.DeviceInventoryId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DeviceSaleType).HasConversion<int?>();
        builder.Property(x => x.InstallmentPlanId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DeviceDownPaymentAmount).HasPrecision(18, 2);
        builder.Property(x => x.DeviceMonthlyInstallmentAmount).HasPrecision(18, 2);
        builder.Property(x => x.DeviceInstallmentContractId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DeviceFinancingDecision).HasConversion<int?>();
        builder.Property(x => x.DeviceOverrideReasonCode).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.DeviceApprovalLevelRequired).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.DeviceFinancingNoteAr).HasMaxLength(512).IsRequired(false);
        builder.Property(x => x.RefundType).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.RefundReason).HasMaxLength(256).IsRequired(false);
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.RefundMethod).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.DepositBalanceSnapshot).HasPrecision(18, 2);
        builder.Property(x => x.WalletBalanceSnapshot).HasPrecision(18, 2);
        builder.Property(x => x.RefundSettlementStatus).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.RefundCbsReference).HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.RefundGatewayReference).HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.RequiresDualApproval).IsRequired();
        builder.Property(x => x.CollectionAction).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.DunningStage).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.PriorDunningStage).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.OutstandingBalanceSnapshot).HasPrecision(18, 2);
        builder.Property(x => x.CollectedAmount).HasPrecision(18, 2);
        builder.Property(x => x.WriteOffAmount).HasPrecision(18, 2);
        builder.Property(x => x.AgencyReference).HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.CollectionNote).HasMaxLength(512).IsRequired(false);
        builder.Property(x => x.CollectionSettlementStatus).HasMaxLength(32).IsRequired(false);

        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.ActivationChannel);
        builder.HasIndex(x => x.DealerCode).HasFilter("[DealerCode] IS NOT NULL");
        builder.HasIndex(x => x.BranchId).HasFilter("[BranchId] IS NOT NULL");

        builder.HasOne(x => x.SubscriberProfile)
            .WithMany(x => x.OperationRequests)
            .HasForeignKey(x => x.SubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecondarySubscriberProfile)
            .WithMany()
            .HasForeignKey(x => x.SecondarySubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MsisdnAsset)
            .WithMany()
            .HasForeignKey(x => x.MsisdnAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SimInventory)
            .WithMany()
            .HasForeignKey(x => x.SimInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CorrelationId).HasFilter("[CorrelationId] IS NOT NULL");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductOffering)
            .WithMany()
            .HasForeignKey(x => x.ProductOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DeviceInventory)
            .WithMany()
            .HasForeignKey(x => x.DeviceInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InstallmentPlan)
            .WithMany()
            .HasForeignKey(x => x.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DeviceInstallmentContract)
            .WithOne(x => x.TelecomOperationRequest)
            .HasForeignKey<DeviceInstallmentContract>(x => x.TelecomOperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
