using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SubscriberProfileConfiguration : BaseEntityConfiguration<SubscriberProfile>
{
    public override void Configure(EntityTypeBuilder<SubscriberProfile> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.CustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.ServiceLineType).HasConversion<int>();
        builder.Property(x => x.LanguagePreference).HasConversion<int>();
        builder.Property(x => x.OperationalStatus).HasConversion<int>();
        builder.Property(x => x.LoyaltyTier).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.PostpaidCreditLimit).HasPrecision(18, 2);
        builder.Property(x => x.PrepaidBalance).HasPrecision(18, 2);
        builder.Property(x => x.MasterSubscriberProfileId).HasMaxLength(IdConsts.MaxLength);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.CustomerId, x.OperationalStatus });

        builder.HasOne(x => x.Customer)
            .WithMany(c => c.SubscriberProfiles)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MasterSubscriberProfile)
            .WithMany(x => x.ChildProfiles)
            .HasForeignKey(x => x.MasterSubscriberProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
