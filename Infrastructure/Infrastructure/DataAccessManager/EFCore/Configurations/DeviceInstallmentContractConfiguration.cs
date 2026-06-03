using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class DeviceInstallmentContractConfiguration : BaseEntityConfiguration<DeviceInstallmentContract>
{
    public override void Configure(EntityTypeBuilder<DeviceInstallmentContract> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TelecomOperationRequestId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.ContractNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.DownPayment).HasPrecision(18, 2);
        builder.Property(x => x.MonthlyAmount).HasPrecision(18, 2);
        builder.Property(x => x.DelinquencyStatus).HasMaxLength(64);
        builder.Property(x => x.CbsContractId).HasMaxLength(128);
        builder.Property(x => x.InstallmentPlanId).HasMaxLength(IdConsts.MaxLength);

        builder.HasIndex(x => x.ContractNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.TelecomOperationRequestId);

        builder.HasOne(x => x.InstallmentPlan)
            .WithMany()
            .HasForeignKey(x => x.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
