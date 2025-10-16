using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class PaymentReceiveConfiguration : BaseEntityConfiguration<PaymentReceive>
{
    public override void Configure(EntityTypeBuilder<PaymentReceive> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.InvoiceId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Number).HasMaxLength(CodeConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PaymentDate).IsRequired(false);
        builder.Property(x => x.PaymentMethodId).HasMaxLength(IdConsts.MaxLength).IsRequired(false);
        builder.Property(x => x.PaymentAmount).IsRequired(false);
        builder.Property(x => x.Status).IsRequired(false);

        builder.HasIndex(e => e.Number);
    }
}