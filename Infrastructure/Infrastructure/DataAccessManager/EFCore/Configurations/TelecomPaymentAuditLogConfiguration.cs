using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomPaymentAuditLogConfiguration : BaseEntityConfiguration<TelecomPaymentAuditLog>
{
    public override void Configure(EntityTypeBuilder<TelecomPaymentAuditLog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TelecomPaymentTransactionId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GatewayReference).HasMaxLength(128);
        builder.Property(x => x.ActorUserId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.ReasonCode).HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(512);
        builder.Property(x => x.BalanceBefore).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TelecomPaymentTransactionId, x.OccurredAtUtc });

        builder.HasOne(x => x.TelecomPaymentTransaction)
            .WithMany()
            .HasForeignKey(x => x.TelecomPaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
