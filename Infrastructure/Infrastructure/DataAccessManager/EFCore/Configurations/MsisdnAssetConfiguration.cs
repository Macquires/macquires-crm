using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class MsisdnAssetConfiguration : BaseEntityConfiguration<MsisdnAsset>
{
    public override void Configure(EntityTypeBuilder<MsisdnAsset> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Msisdn).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BranchId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.PairedIccid).HasMaxLength(32);
        builder.Property(x => x.PairedImsi).HasMaxLength(32);
        builder.Property(x => x.CountryCode).HasMaxLength(8);
        builder.Property(x => x.Prefix).HasMaxLength(8);
        builder.Property(x => x.ReservedForCustomerId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.PoolStatus).HasConversion<int>();
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.IntendedSubscriptionTypeId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.IntendedSubscriptionTypeId);

        builder.HasIndex(x => x.Msisdn)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.BranchId).HasFilter("[BranchId] IS NOT NULL");

        builder.HasIndex(x => new { x.PoolStatus, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.PoolStatus, x.Msisdn })
            .HasFilter("[IsDeleted] = 0 AND [PoolStatus] IN (0, 2)");

        builder.HasOne(x => x.SubscriberProfile)
            .WithMany(x => x.MsisdnAssets)
            .HasForeignKey(x => x.SubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IntendedSubscriptionTypeLookup)
            .WithMany()
            .HasForeignKey(x => x.IntendedSubscriptionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
