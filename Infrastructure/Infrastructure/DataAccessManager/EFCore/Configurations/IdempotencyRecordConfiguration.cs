using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecord");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.Scope, x.Key }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
