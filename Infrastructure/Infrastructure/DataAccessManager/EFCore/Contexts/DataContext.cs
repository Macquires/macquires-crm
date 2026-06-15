using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Common;
using Infrastructure.DataAccessManager.EFCore.Configurations;
using Infrastructure.DataAccessManager.EFCore.Converters;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class DataContext : IdentityDbContext<ApplicationUser>, IEntityDbSet
{
    private readonly IOperatorContext _operatorContext;

    public DataContext(
        DbContextOptions options,
        IOperatorContext operatorContext) : base(options)
    {
        _operatorContext = operatorContext;
    }

    private bool BypassBranchFilter => ResolveBypassBranchFilter(_operatorContext);

    private string? CurrentBranchId => _operatorContext.BranchId;

    private static bool ResolveBypassBranchFilter(IOperatorContext operatorContext)
    {
        if (!operatorContext.IsAuthenticated)
        {
            return false;
        }

        return operatorContext.Roles.Any(r =>
            string.Equals(r, TelecomEnterpriseRoleMatrix.RoleAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, TelecomEnterpriseRoleMatrix.RoleManagement, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, TelecomEnterpriseRoleMatrix.RoleOperationsManager, StringComparison.OrdinalIgnoreCase));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<bool>()
            .HaveConversion<NullableBitBooleanValueConverter>();
    }

    public DbSet<Token> Token { get; set; }
    public DbSet<Company> Company { get; set; }
    public DbSet<FileImage> FileImage { get; set; }
    public DbSet<FileDocument> FileDocument { get; set; }

    public DbSet<NumberSequence> NumberSequence { get; set; }
    public DbSet<CustomerGroup> CustomerGroup { get; set; }
    public DbSet<CustomerCategory> CustomerCategory { get; set; }
    public DbSet<GeoCity> GeoCity { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<CustomerIdentityDocument> CustomerIdentityDocument { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<CustomerContact> CustomerContact { get; set; }

    public DbSet<SubscriberProfile> SubscriberProfile { get; set; }
    public DbSet<MsisdnAsset> MsisdnAsset { get; set; }
    public DbSet<SimInventory> SimInventory { get; set; }
    public DbSet<TelecomSubscriptionTypeLookup> TelecomSubscriptionTypeLookup { get; set; }
    public DbSet<TelecomSubscription> TelecomSubscription { get; set; }
    public DbSet<TelecomOperationRequest> TelecomOperationRequest { get; set; }
    public DbSet<TelecomOperationAuditLog> TelecomOperationAuditLog { get; set; }
    public DbSet<InventoryBulkImportJob> InventoryBulkImportJob { get; set; }
    public DbSet<InventoryBulkImportError> InventoryBulkImportError { get; set; }
    public DbSet<BillingIntegrationLog> BillingIntegrationLog { get; set; }
    public DbSet<TelecomPaymentTransaction> TelecomPaymentTransaction { get; set; }
    public DbSet<TelecomPaymentAuditLog> TelecomPaymentAuditLog { get; set; }
    public DbSet<TelecomIntegrationLog> TelecomIntegrationLog { get; set; }
    public DbSet<TelecomMsisdnChangeLog> TelecomMsisdnChangeLog { get; set; }
    public DbSet<ProductOffering> ProductOffering { get; set; }
    public DbSet<ProductOfferingComponent> ProductOfferingComponent { get; set; }
    public DbSet<PricePlan> PricePlan { get; set; }
    public DbSet<DashboardWidget> DashboardWidget { get; set; }
    public DbSet<GlobalSetting> GlobalSetting { get; set; }
    public DbSet<OrgUnit> OrgUnit { get; set; }
    public DbSet<UserAuditLog> UserAuditLog { get; set; }
    public DbSet<RolePermission> RolePermission { get; set; }
    public DbSet<TelecomTechnicalTicket> TelecomTechnicalTicket { get; set; }
    public DbSet<TelecomValueAddedService> TelecomValueAddedService { get; set; }
    public DbSet<IntegrationOutboxMessage> IntegrationOutboxMessage { get; set; }
    public DbSet<IdempotencyRecord> IdempotencyRecord { get; set; }

    public DbSet<SubscriberActiveService> SubscriberActiveService { get; set; }
    public DbSet<DeviceInventory> DeviceInventory { get; set; }
    public DbSet<InstallmentPlan> InstallmentPlan { get; set; }
    public DbSet<DeviceInstallmentContract> DeviceInstallmentContract { get; set; }
    public DbSet<DeviceInstallmentScheduleLine> DeviceInstallmentScheduleLine { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
        modelBuilder.ApplyConfiguration(new TokenConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new FileImageConfiguration());
        modelBuilder.ApplyConfiguration(new FileDocumentConfiguration());

        modelBuilder.ApplyConfiguration(new NumberSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerGroupConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new GeoCityConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new IndividualCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CorporateCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerIdentityDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerContactConfiguration());

        modelBuilder.ApplyConfiguration(new SubscriberProfileConfiguration());
        modelBuilder.ApplyConfiguration(new MsisdnAssetConfiguration());
        modelBuilder.ApplyConfiguration(new SimInventoryConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomSubscriptionTypeLookupConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomOperationRequestConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomOperationAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryBulkImportJobConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryBulkImportErrorConfiguration());
        modelBuilder.ApplyConfiguration(new BillingIntegrationLogConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomPaymentTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomPaymentAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomIntegrationLogConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomMsisdnChangeLogConfiguration());
        modelBuilder.ApplyConfiguration(new ProductOfferingConfiguration());
        modelBuilder.ApplyConfiguration(new ProductOfferingComponentConfiguration());
        modelBuilder.ApplyConfiguration(new PricePlanConfiguration());
        modelBuilder.ApplyConfiguration(new DashboardWidgetConfiguration());
        modelBuilder.ApplyConfiguration(new GlobalSettingConfiguration());
        modelBuilder.ApplyConfiguration(new OrgUnitConfiguration());
        modelBuilder.ApplyConfiguration(new UserAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomTechnicalTicketConfiguration());
        modelBuilder.ApplyConfiguration(new TelecomValueAddedServiceConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriberActiveServiceConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceInventoryConfiguration());
        modelBuilder.ApplyConfiguration(new InstallmentPlanConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceInstallmentContractConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceInstallmentScheduleLineConfiguration());
        modelBuilder.ApplyConfiguration(new IntegrationOutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyRecordConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.GetRootType() != entityType)
            {
                continue;
            }

            var clrType = entityType.ClrType;
            if (clrType is null || !typeof(Domain.Common.IHasIsDeleted).IsAssignableFrom(clrType))
            {
                continue;
            }

            var configureMethod = typeof(DataContext)
                .GetMethod(nameof(ConfigureQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(clrType);
            configureMethod.Invoke(this, new object[] { modelBuilder });
        }

        modelBuilder.ApplyNullableBitAsBoolConvention();
    }

    private void ConfigureQueryFilter<T>(ModelBuilder modelBuilder) where T : class, Domain.Common.IHasIsDeleted
    {
        if (typeof(Domain.Common.IHasBranchId).IsAssignableFrom(typeof(T)))
        {
            modelBuilder.Entity<T>().HasQueryFilter(e =>
                !e.IsDeleted && MatchesBranchFilter(((Domain.Common.IHasBranchId)e).BranchId));
        }
        else
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }
    }

    private bool MatchesBranchFilter(string? entityBranchId)
    {
        if (BypassBranchFilter)
        {
            return true;
        }

        if (string.IsNullOrEmpty(CurrentBranchId))
        {
            return false;
        }

        return string.Equals(entityBranchId, CurrentBranchId, StringComparison.Ordinal);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyInitialRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyInitialRowVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyInitialRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyInitialRowVersions()
    {
        var seed = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            foreach (var property in entry.Properties.Where(p =>
                         p.Metadata.ClrType == typeof(byte[])
                         && string.Equals(p.Metadata.Name, "RowVersion", StringComparison.Ordinal)
                         && p.CurrentValue is null))
            {
                property.CurrentValue = seed;
            }
        }
    }
}
