using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class GeoCityConfiguration : BaseEntityConfiguration<GeoCity>
{
    public override void Configure(EntityTypeBuilder<GeoCity> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Governorate).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.IsActive);
        builder.Property(x => x.SortOrder);

        builder.HasIndex(e => new { e.Name, e.Governorate });
        builder.HasIndex(e => new { e.IsActive, e.SortOrder });
    }
}
