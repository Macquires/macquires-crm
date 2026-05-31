using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class InventoryBulkImportErrorConfiguration : BaseEntityConfiguration<InventoryBulkImportError>
{
    public override void Configure(EntityTypeBuilder<InventoryBulkImportError> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.JobId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Identifier).HasMaxLength(128);
        builder.Property(x => x.ErrorMessageAr).HasMaxLength(2000);
        builder.Property(x => x.ErrorMessageEn).HasMaxLength(2000);
        builder.Property(x => x.RawRowDataJson).HasMaxLength(4000);

        builder.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => new { x.JobId, x.RowNumber });
    }
}
