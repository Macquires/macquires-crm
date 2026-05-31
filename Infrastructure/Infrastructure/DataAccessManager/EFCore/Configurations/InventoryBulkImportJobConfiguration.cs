using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class InventoryBulkImportJobConfiguration : BaseEntityConfiguration<InventoryBulkImportJob>
{
    public override void Configure(EntityTypeBuilder<InventoryBulkImportJob> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.JobStatus).HasConversion<int>();
        builder.Property(x => x.JobType).HasConversion<int>();
        builder.Property(x => x.FileName).HasMaxLength(512);
        builder.Property(x => x.StoredFilePath).HasMaxLength(1024);
        builder.Property(x => x.ErrorSummary).HasMaxLength(DescriptionConsts.MaxLength);
        builder.HasIndex(x => x.JobStatus);
        builder.HasIndex(x => x.JobType);
        builder.HasIndex(x => x.CreatedById);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
