using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class OrgUnitConfiguration : BaseEntityConfiguration<OrgUnit>
{
    public override void Configure(EntityTypeBuilder<OrgUnit> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(256);
        builder.Property(x => x.Kind).HasConversion<int>().HasDefaultValue(Domain.Enums.OrgUnitKind.Branch);
        builder.Property(x => x.ParentId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.ManagerUserId).HasMaxLength(450);
        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.ParentId);
        builder.HasIndex(x => x.ManagerUserId);
        builder.HasIndex(x => x.IsActive);
    }
}
