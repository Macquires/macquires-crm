using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomSubscriptionConfiguration : BaseEntityConfiguration<TelecomSubscription>
{
    public override void Configure(EntityTypeBuilder<TelecomSubscription> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.MsisdnAssetId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        builder.HasOne(x => x.SubscriberProfile)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.SubscriberProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MsisdnAsset)
            .WithMany()
            .HasForeignKey(x => x.MsisdnAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ProductOfferingId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.HasOne(x => x.ProductOffering)
            .WithMany()
            .HasForeignKey(x => x.ProductOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.SubscriptionTypeId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.HasOne(x => x.SubscriptionTypeLookup)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
