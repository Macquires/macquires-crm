using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SalesRepresentativeConfiguration : BaseEntityConfiguration<SalesRepresentative>
{
    public override void Configure(EntityTypeBuilder<SalesRepresentative> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.JobTitle).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.EmployeeNumber).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PhoneNumber).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.EmailAddress).HasMaxLength(EmailConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.SalesTeamId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Number);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.JobTitle);
    }
}