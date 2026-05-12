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
        builder.Property(x => x.Iccid).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.Imsi).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.Puk1).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.Puk2).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        builder.HasIndex(x => x.Msisdn).IsUnique();

        builder.HasOne(x => x.SubscriberProfile)
            .WithMany(x => x.MsisdnAssets)
            .HasForeignKey(x => x.SubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
