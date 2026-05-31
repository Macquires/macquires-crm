using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SubscriberActiveServiceConfiguration : BaseEntityConfiguration<SubscriberActiveService>
{
    public override void Configure(EntityTypeBuilder<SubscriberActiveService> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TelecomSubscriptionId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.TelecomValueAddedServiceId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.Msisdn).HasMaxLength(32).IsRequired();

        builder.HasIndex(x => new { x.TelecomSubscriptionId, x.TelecomValueAddedServiceId }).IsUnique();
        builder.HasIndex(x => x.Msisdn);

        builder.HasOne(x => x.TelecomSubscription)
            .WithMany()
            .HasForeignKey(x => x.TelecomSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TelecomValueAddedService)
            .WithMany(x => x.ActiveSubscriptions)
            .HasForeignKey(x => x.TelecomValueAddedServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
