using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BillingIntegrationLogConfiguration : BaseEntityConfiguration<BillingIntegrationLog>
{
    public override void Configure(EntityTypeBuilder<BillingIntegrationLog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TelecomOperationRequestId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(DescriptionConsts.MaxLength).IsRequired();
        builder.Property(x => x.IntegrationTarget).HasMaxLength(NameConsts.MaxLength).IsRequired(false);

        builder.HasOne(x => x.TelecomOperationRequest)
            .WithMany()
            .HasForeignKey(x => x.TelecomOperationRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TelecomOperationRequestId, x.AttemptNumber });
    }
}
