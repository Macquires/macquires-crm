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
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.SecondarySubscriberProfileId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.MsisdnAssetId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.TargetOfferName).HasMaxLength(LengthConsts.M).IsRequired(false);

        builder.HasIndex(x => x.Number).IsUnique();

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

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
