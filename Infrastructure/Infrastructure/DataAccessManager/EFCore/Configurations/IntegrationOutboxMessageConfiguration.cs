using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public sealed class IntegrationOutboxMessageConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
    {
        builder.ToTable("IntegrationOutboxMessage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc });
        builder.HasIndex(x => x.CorrelationId).HasFilter("[CorrelationId] IS NOT NULL");
    }
}
