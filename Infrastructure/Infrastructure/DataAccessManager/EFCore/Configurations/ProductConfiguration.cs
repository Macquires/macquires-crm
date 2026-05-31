using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProductConfiguration : BaseEntityConfiguration<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.ServiceCode).HasMaxLength(CodeConsts.MaxLength);
        builder.Property(x => x.CompatibleSubscriptionTypeId).HasMaxLength(IdConsts.MaxLength);

        builder.HasOne(x => x.CompatibleSubscriptionTypeLookup)
            .WithMany()
            .HasForeignKey(x => x.CompatibleSubscriptionTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Number);
        builder.HasIndex(e => e.CompatibleSubscriptionTypeId);
    }
}
