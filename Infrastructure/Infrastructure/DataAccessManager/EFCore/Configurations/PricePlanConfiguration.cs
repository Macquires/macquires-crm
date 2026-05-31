using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PricePlanConfiguration : BaseEntityConfiguration<PricePlan>
{
    public override void Configure(EntityTypeBuilder<PricePlan> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductOfferingId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.PlanType).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PricePerMinute).HasPrecision(18, 4);
        builder.Property(x => x.PricePerMegabyte).HasPrecision(18, 4);
        builder.Property(x => x.PricePerSms).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(CodeConsts.MaxLength).HasDefaultValue("SYP").IsRequired();
        builder.Property(x => x.ActivationFee).HasPrecision(18, 2).IsRequired(false);
        builder.Property(x => x.ValidityDays).IsRequired(false);
        builder.Property(x => x.IsDefault).HasDefaultValue(false).IsRequired();

        builder.HasIndex(x => x.ProductOfferingId);

        builder.HasOne(x => x.ProductOffering)
            .WithMany(po => po.PricePlans)
            .HasForeignKey(x => x.ProductOfferingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
