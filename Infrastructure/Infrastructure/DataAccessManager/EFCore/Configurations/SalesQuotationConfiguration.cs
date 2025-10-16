using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class SalesQuotationConfiguration : BaseEntityConfiguration<SalesQuotation>
{
    public override void Configure(EntityTypeBuilder<SalesQuotation> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.QuotationDate).IsRequired(false);
        builder.Property(x => x.QuotationStatus).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.CustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.TaxId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.BeforeTaxAmount).IsRequired(false);
        builder.Property(x => x.TaxAmount).IsRequired(false);
        builder.Property(x => x.AfterTaxAmount).IsRequired(false);

        builder.HasIndex(e => e.Number);
    }
}