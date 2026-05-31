using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomIntegrationLogConfiguration : BaseEntityConfiguration<TelecomIntegrationLog>
{
    public override void Configure(EntityTypeBuilder<TelecomIntegrationLog> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Msisdn).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.OperationName).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.RequestPayload).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.ResponsePayload).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.ResponseStatusCode).HasMaxLength(64);
        builder.Property(x => x.IntegrationSystem).HasConversion<int>();

        builder.HasIndex(x => x.Msisdn).HasFilter("[Msisdn] IS NOT NULL");
        builder.HasIndex(x => x.IntegrationSystem);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}
