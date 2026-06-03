using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomOperationAuditLogConfiguration : BaseEntityConfiguration<TelecomOperationAuditLog>
{
    public override void Configure(EntityTypeBuilder<TelecomOperationAuditLog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TelecomOperationRequestId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<int>();
        builder.Property(x => x.ToStatus).HasConversion<int>();
        builder.Property(x => x.ActorUserId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Note).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.ActivationChannel).HasConversion<int>().IsRequired(false);
        builder.Property(x => x.BranchId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.DealerCode).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.OverrideReasonCode).HasMaxLength(64).IsRequired(false);
        builder.Property(x => x.CorrelationId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.FieldChangesJson).IsRequired(false);

        builder.HasIndex(x => x.TelecomOperationRequestId);
        builder.HasIndex(x => x.OccurredAtUtc);

        builder.HasOne(x => x.TelecomOperationRequest)
            .WithMany(o => o.AuditLogs)
            .HasForeignKey(x => x.TelecomOperationRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
