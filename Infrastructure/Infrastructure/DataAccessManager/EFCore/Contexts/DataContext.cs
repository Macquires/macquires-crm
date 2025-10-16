using Application.Common.Repositories;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Configurations;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;


public class DataContext : IdentityDbContext<ApplicationUser>, IEntityDbSet
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Token> Token { get; set; }
    public DbSet<Todo> Todo { get; set; }
    public DbSet<TodoItem> TodoItem { get; set; }
    public DbSet<Company> Company { get; set; }
    public DbSet<FileImage> FileImage { get; set; }
    public DbSet<FileDocument> FileDocument { get; set; }


    public DbSet<NumberSequence> NumberSequence { get; set; }
    public DbSet<CustomerGroup> CustomerGroup { get; set; }
    public DbSet<CustomerCategory> CustomerCategory { get; set; }
    public DbSet<VendorGroup> VendorGroup { get; set; }
    public DbSet<VendorCategory> VendorCategory { get; set; }
    public DbSet<Warehouse> Warehouse { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Vendor> Vendor { get; set; }
    public DbSet<UnitMeasure> UnitMeasure { get; set; }
    public DbSet<ProductGroup> ProductGroup { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<CustomerContact> CustomerContact { get; set; }
    public DbSet<VendorContact> VendorContact { get; set; }
    public DbSet<Tax> Tax { get; set; }
    public DbSet<SalesOrder> SalesOrder { get; set; }
    public DbSet<SalesOrderItem> SalesOrderItem { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrder { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItem { get; set; }
    public DbSet<InventoryTransaction> InventoryTransaction { get; set; }
    public DbSet<DeliveryOrder> DeliveryOrder { get; set; }
    public DbSet<GoodsReceive> GoodsReceive { get; set; }
    public DbSet<SalesReturn> SalesReturn { get; set; }
    public DbSet<PurchaseReturn> PurchaseReturn { get; set; }
    public DbSet<TransferIn> TransferIn { get; set; }
    public DbSet<TransferOut> TransferOut { get; set; }
    public DbSet<StockCount> StockCount { get; set; }
    public DbSet<NegativeAdjustment> NegativeAdjustment { get; set; }
    public DbSet<PositiveAdjustment> PositiveAdjustment { get; set; }
    public DbSet<Scrapping> Scrapping { get; set; }
    public DbSet<BookingGroup> BookingGroup { get; set; }
    public DbSet<BookingResource> BookingResource { get; set; }
    public DbSet<Booking> Booking { get; set; }
    public DbSet<ProgramManagerResource> ProgramManagerResource { get; set; }
    public DbSet<ProgramManager> ProgramManager { get; set; }


    public DbSet<Campaign> Campaign { get; set; }
    public DbSet<Budget> Budget { get; set; }
    public DbSet<Expense> Expense { get; set; }
    public DbSet<Lead> Lead { get; set; }
    public DbSet<LeadContact> LeadContact { get; set; }
    public DbSet<LeadActivity> LeadActivity { get; set; }
    public DbSet<SalesTeam> SalesTeam { get; set; }
    public DbSet<SalesRepresentative> SalesRepresentative { get; set; }
    public DbSet<PaymentMethod> PaymentMethod { get; set; }
    public DbSet<SalesQuotation> SalesQuotation { get; set; }
    public DbSet<SalesQuotationItem> SalesQuotationItem { get; set; }
    public DbSet<Invoice> Invoice { get; set; }
    public DbSet<CreditNote> CreditNote { get; set; }
    public DbSet<PaymentReceive> PaymentReceive { get; set; }
    public DbSet<PurchaseRequisition> PurchaseRequisition { get; set; }
    public DbSet<PurchaseRequisitionItem> PurchaseRequisitionItem { get; set; }
    public DbSet<Bill> Bill { get; set; }
    public DbSet<DebitNote> DebitNote { get; set; }
    public DbSet<PaymentDisburse> PaymentDisburse { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
        modelBuilder.ApplyConfiguration(new TokenConfiguration());
        modelBuilder.ApplyConfiguration(new TodoConfiguration());
        modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new FileImageConfiguration());
        modelBuilder.ApplyConfiguration(new FileDocumentConfiguration());


        modelBuilder.ApplyConfiguration(new NumberSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerGroupConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new VendorGroupConfiguration());
        modelBuilder.ApplyConfiguration(new VendorCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new WarehouseConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new VendorConfiguration());
        modelBuilder.ApplyConfiguration(new UnitMeasureConfiguration());
        modelBuilder.ApplyConfiguration(new ProductGroupConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerContactConfiguration());
        modelBuilder.ApplyConfiguration(new VendorContactConfiguration());
        modelBuilder.ApplyConfiguration(new TaxConfiguration());
        modelBuilder.ApplyConfiguration(new SalesOrderConfiguration());
        modelBuilder.ApplyConfiguration(new SalesOrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseOrderConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseOrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryOrderConfiguration());
        modelBuilder.ApplyConfiguration(new GoodsReceiveConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseReturnConfiguration());
        modelBuilder.ApplyConfiguration(new TransferInConfiguration());
        modelBuilder.ApplyConfiguration(new TransferOutConfiguration());
        modelBuilder.ApplyConfiguration(new StockCountConfiguration());
        modelBuilder.ApplyConfiguration(new NegativeAdjustmentConfiguration());
        modelBuilder.ApplyConfiguration(new PositiveAdjustmentConfiguration());
        modelBuilder.ApplyConfiguration(new ScrappingConfiguration());
        modelBuilder.ApplyConfiguration(new BookingGroupConfiguration());
        modelBuilder.ApplyConfiguration(new BookingResourceConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new ProgramManagerResourceConfiguration());
        modelBuilder.ApplyConfiguration(new ProgramManagerConfiguration());


        modelBuilder.ApplyConfiguration(new BudgetConfiguration());
        modelBuilder.ApplyConfiguration(new CampaignConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new LeadConfiguration());
        modelBuilder.ApplyConfiguration(new LeadContactConfiguration());
        modelBuilder.ApplyConfiguration(new LeadActivityConfiguration());
        modelBuilder.ApplyConfiguration(new SalesTeamConfiguration());
        modelBuilder.ApplyConfiguration(new SalesRepresentativeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentMethodConfiguration());
        modelBuilder.ApplyConfiguration(new SalesQuotationConfiguration());
        modelBuilder.ApplyConfiguration(new SalesQuotationItemConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new CreditNoteConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReceiveConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseRequisitionConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseRequisitionItemConfiguration());
        modelBuilder.ApplyConfiguration(new BillConfiguration());
        modelBuilder.ApplyConfiguration(new DebitNoteConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentDisburseConfiguration());

    }

}


