using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomSubscriptionTypeLookupConfiguration : BaseEntityConfiguration<TelecomSubscriptionTypeLookup>
{
    public override void Configure(EntityTypeBuilder<TelecomSubscriptionTypeLookup> builder)
    {
        base.Configure(builder);

        builder.ToTable("TelecomSubscriptionTypes");

        builder.Property(x => x.Code).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.DisplayColor).HasMaxLength(32).IsRequired(false);
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDefault).HasDefaultValue(false).IsRequired();

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
