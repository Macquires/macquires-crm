using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class DeviceInstallmentScheduleLineConfiguration : BaseEntityConfiguration<DeviceInstallmentScheduleLine>
{
    public override void Configure(EntityTypeBuilder<DeviceInstallmentScheduleLine> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.DeviceInstallmentContractId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.PaymentReference).HasMaxLength(128);

        builder.HasIndex(x => new { x.DeviceInstallmentContractId, x.Sequence }).IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.DeviceInstallmentContract)
            .WithMany(x => x.ScheduleLines)
            .HasForeignKey(x => x.DeviceInstallmentContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
