using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomValueAddedServiceConfiguration : BaseEntityConfiguration<TelecomValueAddedService>
{
    public override void Configure(EntityTypeBuilder<TelecomValueAddedService> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ServiceCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(255).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.MonthlyFee).HasPrecision(18, 2);
        builder.Property(x => x.HlrCommandTemplate).HasMaxLength(512).IsRequired();

        builder.HasIndex(x => x.ServiceCode).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
