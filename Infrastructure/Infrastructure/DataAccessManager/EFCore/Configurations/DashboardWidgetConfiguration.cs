using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class DashboardWidgetConfiguration : BaseEntityConfiguration<DashboardWidget>
{
    public override void Configure(EntityTypeBuilder<DashboardWidget> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.WidgetKey).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.HasIndex(x => x.WidgetKey).IsUnique();
        builder.Property(x => x.TitleAr).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.TitleEn).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.Icon).HasMaxLength(64);
        builder.Property(x => x.ProviderKey).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.PersonasAllowed).HasMaxLength(256).IsRequired();
        builder.Property(x => x.GridSize).HasConversion<int>();
        builder.Property(x => x.WidgetKind).HasConversion<int>();
        builder.Property(x => x.CtaUrl).HasMaxLength(512);
        builder.Property(x => x.CtaLabelAr).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.CtaLabelEn).HasMaxLength(NameConsts.MaxLength);
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
