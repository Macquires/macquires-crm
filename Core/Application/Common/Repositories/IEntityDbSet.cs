using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Repositories;

public interface IEntityDbSet
{
    DbSet<Token> Token { get; set; }
    DbSet<Company> Company { get; set; }
    DbSet<FileImage> FileImage { get; set; }
    DbSet<FileDocument> FileDocument { get; set; }

    DbSet<NumberSequence> NumberSequence { get; set; }
    DbSet<CustomerGroup> CustomerGroup { get; set; }
    DbSet<CustomerCategory> CustomerCategory { get; set; }
    DbSet<Customer> Customer { get; set; }
    DbSet<CustomerIdentityDocument> CustomerIdentityDocument { get; set; }
    DbSet<Product> Product { get; set; }
    DbSet<CustomerContact> CustomerContact { get; set; }

    DbSet<SubscriberProfile> SubscriberProfile { get; set; }
    DbSet<MsisdnAsset> MsisdnAsset { get; set; }
    DbSet<SimInventory> SimInventory { get; set; }
    DbSet<TelecomSubscriptionTypeLookup> TelecomSubscriptionTypeLookup { get; set; }
    DbSet<TelecomSubscription> TelecomSubscription { get; set; }
    DbSet<TelecomOperationRequest> TelecomOperationRequest { get; set; }
    DbSet<TelecomOperationAuditLog> TelecomOperationAuditLog { get; set; }
    DbSet<InventoryBulkImportJob> InventoryBulkImportJob { get; set; }
    DbSet<InventoryBulkImportError> InventoryBulkImportError { get; set; }
    DbSet<BillingIntegrationLog> BillingIntegrationLog { get; set; }
    DbSet<TelecomPaymentTransaction> TelecomPaymentTransaction { get; set; }
    DbSet<TelecomPaymentAuditLog> TelecomPaymentAuditLog { get; set; }
    DbSet<TelecomIntegrationLog> TelecomIntegrationLog { get; set; }
    DbSet<TelecomMsisdnChangeLog> TelecomMsisdnChangeLog { get; set; }
    DbSet<ProductOffering> ProductOffering { get; set; }
    DbSet<ProductOfferingComponent> ProductOfferingComponent { get; set; }
    DbSet<PricePlan> PricePlan { get; set; }
    DbSet<DashboardWidget> DashboardWidget { get; set; }
    DbSet<GlobalSetting> GlobalSetting { get; set; }
    DbSet<OrgUnit> OrgUnit { get; set; }
    DbSet<UserAuditLog> UserAuditLog { get; set; }
    DbSet<RolePermission> RolePermission { get; set; }

    DbSet<TelecomTechnicalTicket> TelecomTechnicalTicket { get; set; }
    DbSet<TelecomValueAddedService> TelecomValueAddedService { get; set; }
    DbSet<SubscriberActiveService> SubscriberActiveService { get; set; }
    DbSet<DeviceInventory> DeviceInventory { get; set; }
    DbSet<InstallmentPlan> InstallmentPlan { get; set; }
    DbSet<DeviceInstallmentContract> DeviceInstallmentContract { get; set; }
    DbSet<DeviceInstallmentScheduleLine> DeviceInstallmentScheduleLine { get; set; }
}
