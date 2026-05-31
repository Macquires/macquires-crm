using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductOfferingComponentConfiguration : BaseEntityConfiguration<ProductOfferingComponent>
{
    public override void Configure(EntityTypeBuilder<ProductOfferingComponent> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductOfferingId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.ComponentType).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Quota).HasPrecision(18, 2).IsRequired(false);
        builder.Property(x => x.QuotaUnit).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.IsUnlimited).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();

        builder.HasIndex(x => x.ProductOfferingId);

        builder.Property(x => x.RequiresProductOfferingId).HasMaxLength(IdConsts.MaxLength);

        builder.HasOne(x => x.ProductOffering)
            .WithMany(po => po.Components)
            .HasForeignKey(x => x.ProductOfferingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RequiresProductOffering)
            .WithMany()
            .HasForeignKey(x => x.RequiresProductOfferingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
