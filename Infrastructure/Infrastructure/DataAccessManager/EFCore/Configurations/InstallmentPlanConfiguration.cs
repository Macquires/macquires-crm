using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class InstallmentPlanConfiguration : BaseEntityConfiguration<InstallmentPlan>
{
    public override void Configure(EntityTypeBuilder<InstallmentPlan> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MinDownPaymentPercent).HasPrecision(5, 2);
        builder.Property(x => x.InterestRatePercent).HasPrecision(5, 2);

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
