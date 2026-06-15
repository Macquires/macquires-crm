using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BillingIntegrationLogConfiguration : BaseEntityConfiguration<BillingIntegrationLog>
{
    public override void Configure(EntityTypeBuilder<BillingIntegrationLog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TelecomOperationRequestId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.BranchId).HasMaxLength(50);
        builder.Property(x => x.TelecomPaymentTransactionId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Message).HasMaxLength(DescriptionConsts.MaxLength).IsRequired();
        builder.Property(x => x.IntegrationTarget).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.CorrelationId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.RequestPayload).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.ResponsePayload).HasMaxLength(DescriptionConsts.MaxLength);

        builder.HasIndex(x => x.CorrelationId)
            .IsUnique()
            .HasFilter("[CorrelationId] IS NOT NULL AND [Success] = 1 AND [IsDeleted] = 0");

        builder.HasOne(x => x.TelecomOperationRequest)
            .WithMany()
            .HasForeignKey(x => x.TelecomOperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TelecomPaymentTransaction)
            .WithMany()
            .HasForeignKey(x => x.TelecomPaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TelecomPaymentTransactionId)
            .HasFilter("[TelecomPaymentTransactionId] IS NOT NULL");

        builder.HasIndex(x => new { x.TelecomOperationRequestId, x.AttemptNumber })
            .HasFilter("[TelecomOperationRequestId] IS NOT NULL");

        builder.HasIndex(x => x.BranchId)
            .HasFilter("[BranchId] IS NOT NULL");
    }
}
