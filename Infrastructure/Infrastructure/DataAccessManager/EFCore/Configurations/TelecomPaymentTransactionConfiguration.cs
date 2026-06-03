using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomPaymentTransactionConfiguration : BaseEntityConfiguration<TelecomPaymentTransaction>
{
    public override void Configure(EntityTypeBuilder<TelecomPaymentTransaction> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.TelecomSubscriptionId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Msisdn).HasMaxLength(CodeConsts.MaxLength);
        builder.Property(x => x.GatewayReference).HasMaxLength(128);
        builder.Property(x => x.GatewayTransactionId).HasMaxLength(128);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(64);
        builder.Property(x => x.FailureReason).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.VoucherCode).HasMaxLength(64);
        builder.Property(x => x.TelecomOperationRequestId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.OriginalPaymentId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.ReversalReasonCode).HasMaxLength(64);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceBefore).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);

        builder.HasIndex(x => x.Number).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.GatewayReference)
            .IsUnique()
            .HasFilter("[GatewayReference] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.Msisdn, x.CreatedAtUtc });
        builder.HasIndex(x => x.CorrelationId).HasFilter("[CorrelationId] IS NOT NULL");
    }
}
