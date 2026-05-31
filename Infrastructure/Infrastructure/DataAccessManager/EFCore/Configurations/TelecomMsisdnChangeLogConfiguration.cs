using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomMsisdnChangeLogConfiguration : BaseEntityConfiguration<TelecomMsisdnChangeLog>
{
    public override void Configure(EntityTypeBuilder<TelecomMsisdnChangeLog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.CustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.TelecomSubscriptionId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.MsisdnAssetId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.OldMsisdn).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.NewMsisdn).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.OldSubscriptionType).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.NewSubscriptionType).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.ExternalSyncMessage).HasMaxLength(Constants.LengthConsts.M).IsRequired(false);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.MsisdnAssetId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
