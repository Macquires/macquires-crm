using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class TelecomTechnicalTicketConfiguration : BaseEntityConfiguration<TelecomTechnicalTicket>
{
    public override void Configure(EntityTypeBuilder<TelecomTechnicalTicket> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.TicketNumber).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(x => x.Msisdn).HasMaxLength(LengthConsts.S).IsRequired();
        builder.Property(x => x.CustomerId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.SubscriberProfileId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.BranchId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.AssignedToGroupId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.OpenedByUserId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Notes).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedByChannel).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TicketCategory).IsRequired();
        builder.Property(x => x.AssignedAgentEmail).HasMaxLength(128).IsRequired(false);
        builder.Property(x => x.ClaimedAt).IsRequired(false);

        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.HasIndex(x => x.TicketCategory);
        builder.HasIndex(x => x.Msisdn);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.BranchId).HasFilter("[BranchId] IS NOT NULL");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
