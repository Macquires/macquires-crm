using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class BookingConfiguration : BaseEntityConfiguration<Booking>
{
    public override void Configure(EntityTypeBuilder<Booking> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Subject).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.StartTime).IsRequired(false);
        builder.Property(x => x.EndTime).IsRequired(false);
        builder.Property(x => x.StartTimezone).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.EndTimezone).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Location).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.IsAllDay).IsRequired(false);
        builder.Property(x => x.IsReadOnly).IsRequired(false);
        builder.Property(x => x.IsBlock).IsRequired(false);
        builder.Property(x => x.RecurrenceRule).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.RecurrenceID).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.FollowingID).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.RecurrenceException).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Status).IsRequired(false);
        builder.Property(x => x.BookingResourceId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Subject);
        builder.HasIndex(e => e.Number);
    }
}

