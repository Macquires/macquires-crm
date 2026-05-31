using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class UserAuditLogConfiguration : BaseEntityConfiguration<UserAuditLog>
{
    public override void Configure(EntityTypeBuilder<UserAuditLog> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.ActorUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.ActionType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128);
        builder.Property(x => x.EntityId).HasMaxLength(450);
        builder.Property(x => x.SummaryAr).HasMaxLength(512);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}
