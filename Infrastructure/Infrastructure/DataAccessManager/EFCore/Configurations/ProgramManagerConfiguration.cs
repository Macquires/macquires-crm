using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class ProgramManagerConfiguration : BaseEntityConfiguration<ProgramManager>
{
    public override void Configure(EntityTypeBuilder<ProgramManager> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Title).HasMaxLength(NameConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Summary).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Status).IsRequired(false);
        builder.Property(x => x.Priority).IsRequired(false);
        builder.Property(x => x.ProgramManagerResourceId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);

        builder.HasIndex(e => e.Title);
        builder.HasIndex(e => e.Number);
    }
}

