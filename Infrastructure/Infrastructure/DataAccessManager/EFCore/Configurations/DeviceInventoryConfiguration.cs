using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class DeviceInventoryConfiguration : BaseEntityConfiguration<DeviceInventory>
{
    public override void Configure(EntityTypeBuilder<DeviceInventory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Imei).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(64);
        builder.Property(x => x.ListPrice).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.BranchId).HasMaxLength(64);
        builder.Property(x => x.ReservedByOperationId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Imei).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Status, x.BranchId });
    }
}
