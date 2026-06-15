using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Common;
using Infrastructure.DataAccessManager.EFCore.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Domain.Common.Constants;

namespace Infrastructure.DataAccessManager.EFCore.Configurations;

public class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        base.Configure(builder);

        builder.HasDiscriminator<CustomerKind>("CustomerType")
            .HasValue<IndividualCustomer>(CustomerKind.Individual)
            .HasValue<CorporateCustomer>(CustomerKind.Corporate);

        builder.Property(x => x.DisplayName).HasMaxLength(NameConsts.MaxLength).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(CodeConsts.MaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(DescriptionConsts.MaxLength);
        builder.Property(x => x.ContactEmail).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.PrimaryPhone).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.FaxNumber).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.Website).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.WhatsApp).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.LinkedIn).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.Facebook).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.Instagram).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.TwitterX).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.TikTok).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.CustomerGroupId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.CustomerCategoryId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.OrgUnitId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.BranchId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.StatusReasonCode).HasConversion<int>();
        builder.Property(x => x.StatusReasonNote).HasMaxLength(DescriptionConsts.MaxLength);

        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(p => p.Street).HasColumnName("Street").HasMaxLength(NameConsts.MaxLength);
            a.Property(p => p.City).HasColumnName("City").HasMaxLength(NameConsts.MaxLength);
            a.Property(p => p.State).HasColumnName("State").HasMaxLength(NameConsts.MaxLength);
            a.Property(p => p.ZipCode).HasColumnName("ZipCode").HasMaxLength(NameConsts.MaxLength);
            a.Property(p => p.Country).HasColumnName("Country").HasMaxLength(NameConsts.MaxLength);
        });

        builder.HasIndex(e => e.DisplayName);
        builder.HasIndex(e => e.AccountNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => e.PrimaryPhone);
        builder.HasIndex(e => e.OrgUnitId);
        builder.HasIndex(e => e.BranchId).HasFilter("[BranchId] IS NOT NULL");

        builder.HasOne(x => x.OrgUnit)
            .WithMany()
            .HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.SubscriberProfiles)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class IndividualCustomerConfiguration : IEntityTypeConfiguration<IndividualCustomer>
{
    public void Configure(EntityTypeBuilder<IndividualCustomer> builder)
    {
        builder.Property(x => x.NationalId).HasMaxLength(512).IsRequired()
            .HasConversion(v => FieldEncryptionScope.Encrypt(v), v => FieldEncryptionScope.Decrypt(v));
        builder.Property(x => x.NationalIdSearchHash).HasMaxLength(64);
        builder.Property(x => x.Nationality).HasMaxLength(64);
        builder.Property(x => x.Occupation).HasMaxLength(128);
        builder.Property(x => x.Gender).HasConversion<int>();

        builder.HasIndex(x => x.NationalIdSearchHash)
            .IsUnique()
            .HasFilter("[NationalIdSearchHash] IS NOT NULL AND [IsDeleted] = 0");
    }
}

public class CorporateCustomerConfiguration : IEntityTypeConfiguration<CorporateCustomer>
{
    public void Configure(EntityTypeBuilder<CorporateCustomer> builder)
    {
        builder.Property(x => x.CommercialRegistryNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.TaxNumber).HasMaxLength(32);
        builder.Property(x => x.AuthorizedSignatoryName).HasMaxLength(NameConsts.MaxLength);
        builder.Property(x => x.LegalStatus).HasConversion<int>();
        builder.Property(x => x.ParentCustomerId).HasMaxLength(IdConsts.MaxLength);
        builder.Property(x => x.BillingConsolidationMode).HasConversion<int>();

        builder.HasOne(x => x.ParentCustomer)
            .WithMany()
            .HasForeignKey(x => x.ParentCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CommercialRegistryNumber)
            .IsUnique()
            .HasFilter("[CommercialRegistryNumber] IS NOT NULL AND [IsDeleted] = 0");
    }
}
