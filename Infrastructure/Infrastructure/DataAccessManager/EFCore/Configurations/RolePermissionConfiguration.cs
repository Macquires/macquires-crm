using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermission");
        builder.HasKey(x => new { x.RoleName, x.PermissionKey });
        builder.Property(x => x.RoleName).HasMaxLength(256);
        builder.Property(x => x.PermissionKey).HasMaxLength(128);
        builder.Property(x => x.GrantedById).HasMaxLength(450);
    }
}
