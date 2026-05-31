using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductOfferingConfiguration : BaseEntityConfiguration<ProductOffering>
{
    public override void Configure(EntityTypeBuilder<ProductOffering> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Code).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(x => x.CompatibleSubscriptionTypeId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.ValidFromUtc).IsRequired(false);
        builder.Property(x => x.ValidToUtc).IsRequired(false);
        builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();

        builder.Property(x => x.PaymentType).HasConversion<int?>();
        builder.Property(x => x.BillingCycleEnum).HasConversion<int?>();
        builder.Property(x => x.EligibilityRules).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.AssetCompatibility).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.BillingCycle).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.TaxCategory).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.ServiceIdSocCode).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.SpeedQuotaLimitGb).IsRequired(false);
        builder.Property(x => x.VoiceMinutesLimit).IsRequired(false);
        builder.Property(x => x.ThrottlingPolicy).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.IconClass).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.BadgeColor).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.ShortDescription).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasOne(x => x.CompatibleSubscriptionType)
            .WithMany()
            .HasForeignKey(x => x.CompatibleSubscriptionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ProductId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.HasIndex(x => x.ProductId);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
