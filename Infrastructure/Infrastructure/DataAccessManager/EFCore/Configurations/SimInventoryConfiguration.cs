using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Infrastructure.DataAccessManager.EFCore.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SimInventoryConfiguration : BaseEntityConfiguration<SimInventory>
{
    public override void Configure(EntityTypeBuilder<SimInventory> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Iccid).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SimType).HasConversion<int>();
        builder.Property(x => x.Eid).HasMaxLength(64);
        builder.Property(x => x.ActivationCode).HasMaxLength(64);
        builder.Property(x => x.Imsi).HasMaxLength(32);
        builder.Property(x => x.Pin1).HasMaxLength(256)
            .HasConversion(v => v == null ? null! : FieldEncryptionScope.Encrypt(v), v => v == null ? null : FieldEncryptionScope.Decrypt(v));
        builder.Property(x => x.Puk1).HasMaxLength(16);
        builder.Property(x => x.Pin2).HasMaxLength(256)
            .HasConversion(v => v == null ? null! : FieldEncryptionScope.Encrypt(v), v => v == null ? null : FieldEncryptionScope.Decrypt(v));
        builder.Property(x => x.Puk2).HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Iccid)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });

        builder.HasOne(x => x.SubscriberProfile)
            .WithMany(x => x.SimInventories)
            .HasForeignKey(x => x.SubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
