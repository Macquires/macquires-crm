using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class CustomerIdentityDocumentConfiguration : BaseEntityConfiguration<CustomerIdentityDocument>
{
    public override void Configure(EntityTypeBuilder<CustomerIdentityDocument> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.CustomerId).HasMaxLength(IdConsts.MaxLength).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<int>();
        builder.Property(x => x.DocumentNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FileDocumentId).HasMaxLength(IdConsts.MaxLength);

        builder.HasIndex(x => new { x.CustomerId, x.DocumentType, x.DocumentNumber })
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Customer)
            .WithMany(c => c.IdentityDocuments)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FileDocument)
            .WithMany()
            .HasForeignKey(x => x.FileDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
