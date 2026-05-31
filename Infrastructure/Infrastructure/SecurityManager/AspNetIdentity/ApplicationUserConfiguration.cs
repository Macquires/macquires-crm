using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.SecurityManager.AspNetIdentity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(NameConsts.MaxLength)
            .IsRequired(false);

        builder.Property(u => u.LastName)
            .HasMaxLength(NameConsts.MaxLength)
            .IsRequired(false);

        builder.Property(u => u.ProfilePictureName)
            .HasMaxLength(NameConsts.MaxLength)
            .IsRequired(false);

        builder.Property(u => u.IsDeleted)
            .IsRequired(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired(false);

        builder.Property(u => u.CreatedById)
            .HasMaxLength(UserIdConsts.MaxLength)
            .IsRequired(false);

        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);

        builder.Property(u => u.UpdatedById)
            .HasMaxLength(UserIdConsts.MaxLength)
            .IsRequired(false);

        builder.Property(u => u.PrimaryMenuPersona)
            .HasConversion<int?>()
            .IsRequired(false);

        builder.Property(u => u.ManagerUserId)
            .HasMaxLength(UserIdConsts.MaxLength)
            .IsRequired(false);

        builder.Property(u => u.OrgUnitId)
            .HasMaxLength(IdConsts.MaxLength)
            .IsRequired(false);

        builder.HasOne(u => u.Manager)
            .WithMany()
            .HasForeignKey(u => u.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.OrgUnit)
            .WithMany()
            .HasForeignKey(u => u.OrgUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => u.ManagerUserId);
        builder.HasIndex(u => u.OrgUnitId);

        builder.HasIndex(u => u.UserName);
        builder.HasIndex(u => u.Email);
        builder.HasIndex(u => u.FirstName);
        builder.HasIndex(u => u.LastName);
    }
}

