using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    State = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FaxNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerCategory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerGroup",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardWidget",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WidgetKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProviderKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PersonasAllowed = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GridSize = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    WidgetKind = table.Column<int>(type: "int", nullable: false),
                    RefreshIntervalSeconds = table.Column<int>(type: "int", nullable: true),
                    CtaUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CtaLabelAr = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CtaLabelEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidget", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceInventory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Imei = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReservedByOperationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoldAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileDocument",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GeneratedName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileImage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GeneratedName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileImage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoCity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Governorate = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoCity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSetting",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSetting", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstallmentPlan",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Months = table.Column<int>(type: "int", nullable: false),
                    MinDownPaymentPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    InterestRatePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinCreditScore = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationOutboxMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationOutboxMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBulkImportJob",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobStatus = table.Column<int>(type: "int", nullable: false),
                    JobType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    StoredFilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ProcessedRows = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBulkImportJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberSequence",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Suffix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastUsedCount = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => new { x.RoleName, x.PermissionKey });
                });

            migrationBuilder.CreateTable(
                name: "TelecomIntegrationLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Msisdn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IntegrationSystem = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ResponseStatusCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomIntegrationLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelecomPaymentTransaction",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentChannel = table.Column<int>(type: "int", nullable: false),
                    ServiceChannel = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomSubscriptionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Msisdn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GatewayReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    GatewayTransactionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoucherCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TelecomOperationRequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsReversal = table.Column<bool>(type: "bit", nullable: false),
                    OriginalPaymentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReversalReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReversedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomPaymentTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelecomSubscriptionTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomSubscriptionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelecomTechnicalTicket",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Msisdn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    TicketCategory = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToGroupId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OpenedByUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedAgentEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedByChannel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomTechnicalTicket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelecomValueAddedService",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MonthlyFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HlrCommandTemplate = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomValueAddedService", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Token",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Token", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAuditLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SummaryAr = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBulkImportError",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorMessageAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessageEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RawRowDataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBulkImportError", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBulkImportError_InventoryBulkImportJob_JobId",
                        column: x => x.JobId,
                        principalTable: "InventoryBulkImportJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelecomPaymentAuditLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomPaymentTransactionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: true),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GatewayReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomPaymentAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelecomPaymentAuditLog_TelecomPaymentTransaction_TelecomPaymentTransactionId",
                        column: x => x.TelecomPaymentTransactionId,
                        principalTable: "TelecomPaymentTransaction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UnitPrice = table.Column<double>(type: "float", nullable: true),
                    Physical = table.Column<bool>(type: "bit", nullable: true),
                    CompatibleSubscriptionTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServiceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_TelecomSubscriptionTypes_CompatibleSubscriptionTypeId",
                        column: x => x.CompatibleSubscriptionTypeId,
                        principalTable: "TelecomSubscriptionTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductOffering",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompatibleSubscriptionTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PaymentType = table.Column<int>(type: "int", nullable: true),
                    BillingCycleEnum = table.Column<int>(type: "int", nullable: true),
                    EligibilityRules = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssetCompatibility = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillingCycle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ServiceIdSocCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpeedQuotaLimitGb = table.Column<double>(type: "float", nullable: true),
                    VoiceMinutesLimit = table.Column<int>(type: "int", nullable: true),
                    ThrottlingPolicy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BadgeColor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOffering", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOffering_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductOffering_TelecomSubscriptionTypes_CompatibleSubscriptionTypeId",
                        column: x => x.CompatibleSubscriptionTypeId,
                        principalTable: "TelecomSubscriptionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PricePlan",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductOfferingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PricePerMinute = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PricePerMegabyte = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PricePerSms = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "SYP"),
                    ActivationFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidityDays = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricePlan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricePlan_ProductOffering_ProductOfferingId",
                        column: x => x.ProductOfferingId,
                        principalTable: "ProductOffering",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOfferingComponent",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductOfferingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ComponentType = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Quota = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuotaUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsUnlimited = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RequiresProductOfferingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOfferingComponent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOfferingComponent_ProductOffering_ProductOfferingId",
                        column: x => x.ProductOfferingId,
                        principalTable: "ProductOffering",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductOfferingComponent_ProductOffering_RequiresProductOfferingId",
                        column: x => x.RequiresProductOfferingId,
                        principalTable: "ProductOffering",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePictureName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PrimaryMenuPersona = table.Column<int>(type: "int", nullable: true),
                    ManagerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OrgUnitId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetUsers_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnit",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    ManagerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgUnit_AspNetUsers_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrgUnit_OrgUnit_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrgUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerKind = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReasonCode = table.Column<int>(type: "int", nullable: false),
                    StatusReasonNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    State = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PrimaryPhone = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FaxNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WhatsApp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LinkedIn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Facebook = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Instagram = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TwitterX = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TikTok = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CustomerGroupId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerCategoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrgUnitId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerType = table.Column<int>(type: "int", nullable: false),
                    CommercialRegistryNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ParentCustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillingConsolidationMode = table.Column<int>(type: "int", nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AuthorizedSignatoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LegalStatus = table.Column<int>(type: "int", nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    NationalIdSearchHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_CustomerCategory_CustomerCategoryId",
                        column: x => x.CustomerCategoryId,
                        principalTable: "CustomerCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Customer_CustomerGroup_CustomerGroupId",
                        column: x => x.CustomerGroupId,
                        principalTable: "CustomerGroup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Customer_Customer_ParentCustomerId",
                        column: x => x.ParentCustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customer_OrgUnit_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrgUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContact",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContact_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerIdentityDocument",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileDocumentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerIdentityDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerIdentityDocument_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerIdentityDocument_FileDocument_FileDocumentId",
                        column: x => x.FileDocumentId,
                        principalTable: "FileDocument",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SubscriberProfile",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceLineType = table.Column<int>(type: "int", nullable: false),
                    LanguagePreference = table.Column<int>(type: "int", nullable: false),
                    OperationalStatus = table.Column<int>(type: "int", nullable: false),
                    ActivationDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoyaltyPoints = table.Column<int>(type: "int", nullable: false),
                    LoyaltyTier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PostpaidCreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PrepaidBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ChurnRiskScore = table.Column<int>(type: "int", nullable: true),
                    MasterSubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriberProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriberProfile_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriberProfile_SubscriberProfile_MasterSubscriberProfileId",
                        column: x => x.MasterSubscriberProfileId,
                        principalTable: "SubscriberProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelecomMsisdnChangeLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomSubscriptionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MsisdnAssetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldMsisdn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NewMsisdn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OldSubscriptionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewSubscriptionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalSyncSuccess = table.Column<bool>(type: "bit", nullable: true),
                    ExternalSyncMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomMsisdnChangeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelecomMsisdnChangeLog_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MsisdnAsset",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Msisdn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PairedIccid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PairedImsi = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    PoolStatus = table.Column<int>(type: "int", nullable: false),
                    ReservedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReservedForCustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuarantineEndsUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IntendedSubscriptionTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MsisdnAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MsisdnAsset_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MsisdnAsset_SubscriberProfile_SubscriberProfileId",
                        column: x => x.SubscriberProfileId,
                        principalTable: "SubscriberProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MsisdnAsset_TelecomSubscriptionTypes_IntendedSubscriptionTypeId",
                        column: x => x.IntendedSubscriptionTypeId,
                        principalTable: "TelecomSubscriptionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimInventory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Iccid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SimType = table.Column<int>(type: "int", nullable: false),
                    Eid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ActivationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Imsi = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Pin1 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Puk1 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Pin2 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Puk2 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuarantineEndsUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimInventory_SubscriberProfile_SubscriberProfileId",
                        column: x => x.SubscriberProfileId,
                        principalTable: "SubscriberProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelecomSubscription",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MsisdnAssetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductOfferingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubscriptionTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentStatus = table.Column<int>(type: "int", nullable: false),
                    IsPrimaryLine = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelecomSubscription_MsisdnAsset_MsisdnAssetId",
                        column: x => x.MsisdnAssetId,
                        principalTable: "MsisdnAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomSubscription_ProductOffering_ProductOfferingId",
                        column: x => x.ProductOfferingId,
                        principalTable: "ProductOffering",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomSubscription_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomSubscription_SubscriberProfile_SubscriberProfileId",
                        column: x => x.SubscriberProfileId,
                        principalTable: "SubscriberProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelecomSubscription_TelecomSubscriptionTypes_SubscriptionTypeId",
                        column: x => x.SubscriptionTypeId,
                        principalTable: "TelecomSubscriptionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelecomOperationRequest",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentStatus = table.Column<int>(type: "int", nullable: false),
                    SubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SecondarySubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MsisdnAssetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SimInventoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductOfferingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriorProductId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriorProductOfferingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TargetOfferName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdentityDocumentStorageKey = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KycDocumentReferenceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActivationChannel = table.Column<int>(type: "int", nullable: false),
                    DealerCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BranchId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InitialDepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OverrideReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    KycVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceSubscriptionTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetSubscriptionTypeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GsmMigrationReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GsmEffectiveDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GsmCompatibilityStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TransferReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TakeOverObligationStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DepositTransferPolicy = table.Column<int>(type: "int", nullable: true),
                    ApprovalLevelRequired = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TakeOverEffectiveDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OldCustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewCustomerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriorSubscriberProfileId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReplacementReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsLostOrStolenReport = table.Column<bool>(type: "bit", nullable: false),
                    PriorSimInventoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriorMsisdnAssetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetMsisdnAssetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NumberChangeReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PremiumFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NumberChangeMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TerminationType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TerminationReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TerminationEffectiveDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalBillAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DepositSettlementAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DepositSettlementStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RetentionOfferOutcome = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeprovisionStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SuspensionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SuspensionStartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionEndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BarringLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AutoReconnectEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    BarStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PriorOperationalStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReconnectReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClearanceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SourceSuspensionOperationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FraudClearanceConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    FraudClearanceByUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReactivationAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProvisioningResult = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeviceInventoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceSaleType = table.Column<int>(type: "int", nullable: true),
                    InstallmentPlanId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceDownPaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeviceMonthlyInstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeviceCreditScoreSnapshot = table.Column<int>(type: "int", nullable: true),
                    DeviceInstallmentContractId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceFinancingDecision = table.Column<int>(type: "int", nullable: true),
                    DeviceOverrideReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeviceApprovalLevelRequired = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeviceFinancingNoteAr = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DeviceWarrantyStartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DepositBalanceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WalletBalanceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundSettlementStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RefundCbsReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RefundGatewayReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequiresDualApproval = table.Column<bool>(type: "bit", nullable: false),
                    CollectionAction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DunningStage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PriorDunningStage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OutstandingBalanceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CollectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WriteOffAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AgencyReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PaymentPlanMonths = table.Column<int>(type: "int", nullable: true),
                    NextDunningDueUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollectionNote = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CollectionSettlementStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SlaExpirationTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClaimedByUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedAgentEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomOperationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_DeviceInventory_DeviceInventoryId",
                        column: x => x.DeviceInventoryId,
                        principalTable: "DeviceInventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_InstallmentPlan_InstallmentPlanId",
                        column: x => x.InstallmentPlanId,
                        principalTable: "InstallmentPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_MsisdnAsset_MsisdnAssetId",
                        column: x => x.MsisdnAssetId,
                        principalTable: "MsisdnAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_ProductOffering_ProductOfferingId",
                        column: x => x.ProductOfferingId,
                        principalTable: "ProductOffering",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_SimInventory_SimInventoryId",
                        column: x => x.SimInventoryId,
                        principalTable: "SimInventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_SubscriberProfile_SecondarySubscriberProfileId",
                        column: x => x.SecondarySubscriberProfileId,
                        principalTable: "SubscriberProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TelecomOperationRequest_SubscriberProfile_SubscriberProfileId",
                        column: x => x.SubscriberProfileId,
                        principalTable: "SubscriberProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriberActiveService",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomSubscriptionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomValueAddedServiceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Msisdn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriberActiveService", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriberActiveService_TelecomSubscription_TelecomSubscriptionId",
                        column: x => x.TelecomSubscriptionId,
                        principalTable: "TelecomSubscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriberActiveService_TelecomValueAddedService_TelecomValueAddedServiceId",
                        column: x => x.TelecomValueAddedServiceId,
                        principalTable: "TelecomValueAddedService",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingIntegrationLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomOperationRequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TelecomPaymentTransactionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IntegrationTarget = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingIntegrationLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingIntegrationLog_TelecomOperationRequest_TelecomOperationRequestId",
                        column: x => x.TelecomOperationRequestId,
                        principalTable: "TelecomOperationRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingIntegrationLog_TelecomPaymentTransaction_TelecomPaymentTransactionId",
                        column: x => x.TelecomPaymentTransactionId,
                        principalTable: "TelecomPaymentTransaction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceInstallmentContract",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomOperationRequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DownPayment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditScoreSnapshot = table.Column<int>(type: "int", nullable: true),
                    DelinquencyStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CbsContractId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    WarrantyStartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstallmentPlanId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInstallmentContract", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceInstallmentContract_InstallmentPlan_InstallmentPlanId",
                        column: x => x.InstallmentPlanId,
                        principalTable: "InstallmentPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceInstallmentContract_TelecomOperationRequest_TelecomOperationRequestId",
                        column: x => x.TelecomOperationRequestId,
                        principalTable: "TelecomOperationRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelecomOperationAuditLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TelecomOperationRequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivationChannel = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DealerCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OverrideReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FieldChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelecomOperationAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelecomOperationAuditLog_TelecomOperationRequest_TelecomOperationRequestId",
                        column: x => x.TelecomOperationRequestId,
                        principalTable: "TelecomOperationRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceInstallmentScheduleLine",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceInstallmentContractId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInstallmentScheduleLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceInstallmentScheduleLine_DeviceInstallmentContract_DeviceInstallmentContractId",
                        column: x => x.DeviceInstallmentContractId,
                        principalTable: "DeviceInstallmentContract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FirstName",
                table: "AspNetUsers",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LastName",
                table: "AspNetUsers",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ManagerUserId",
                table: "AspNetUsers",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrgUnitId",
                table: "AspNetUsers",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName",
                table: "AspNetUsers",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingIntegrationLog_CorrelationId",
                table: "BillingIntegrationLog",
                column: "CorrelationId",
                unique: true,
                filter: "[CorrelationId] IS NOT NULL AND [Success] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BillingIntegrationLog_TelecomOperationRequestId_AttemptNumber",
                table: "BillingIntegrationLog",
                columns: new[] { "TelecomOperationRequestId", "AttemptNumber" },
                filter: "[TelecomOperationRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingIntegrationLog_TelecomPaymentTransactionId",
                table: "BillingIntegrationLog",
                column: "TelecomPaymentTransactionId",
                filter: "[TelecomPaymentTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_AccountNumber",
                table: "Customer",
                column: "AccountNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CommercialRegistryNumber",
                table: "Customer",
                column: "CommercialRegistryNumber",
                unique: true,
                filter: "[CommercialRegistryNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CustomerCategoryId",
                table: "Customer",
                column: "CustomerCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CustomerGroupId",
                table: "Customer",
                column: "CustomerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_DisplayName",
                table: "Customer",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_NationalIdSearchHash",
                table: "Customer",
                column: "NationalIdSearchHash",
                unique: true,
                filter: "[NationalIdSearchHash] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_OrgUnitId",
                table: "Customer",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_ParentCustomerId",
                table: "Customer",
                column: "ParentCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_PrimaryPhone",
                table: "Customer",
                column: "PrimaryPhone");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCategory_Name",
                table: "CustomerCategory",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContact_CustomerId",
                table: "CustomerContact",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContact_Name",
                table: "CustomerContact",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContact_Number",
                table: "CustomerContact",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerGroup_Name",
                table: "CustomerGroup",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerIdentityDocument_CustomerId_DocumentType_DocumentNumber",
                table: "CustomerIdentityDocument",
                columns: new[] { "CustomerId", "DocumentType", "DocumentNumber" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerIdentityDocument_FileDocumentId",
                table: "CustomerIdentityDocument",
                column: "FileDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidget_IsActive_SortOrder",
                table: "DashboardWidget",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidget_WidgetKey",
                table: "DashboardWidget",
                column: "WidgetKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInstallmentContract_ContractNumber",
                table: "DeviceInstallmentContract",
                column: "ContractNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInstallmentContract_InstallmentPlanId",
                table: "DeviceInstallmentContract",
                column: "InstallmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInstallmentContract_TelecomOperationRequestId",
                table: "DeviceInstallmentContract",
                column: "TelecomOperationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInstallmentScheduleLine_DeviceInstallmentContractId_Sequence",
                table: "DeviceInstallmentScheduleLine",
                columns: new[] { "DeviceInstallmentContractId", "Sequence" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventory_Imei",
                table: "DeviceInventory",
                column: "Imei",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventory_Status_BranchId",
                table: "DeviceInventory",
                columns: new[] { "Status", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileDocument_GeneratedName",
                table: "FileDocument",
                column: "GeneratedName");

            migrationBuilder.CreateIndex(
                name: "IX_FileDocument_OriginalName",
                table: "FileDocument",
                column: "OriginalName");

            migrationBuilder.CreateIndex(
                name: "IX_FileImage_GeneratedName",
                table: "FileImage",
                column: "GeneratedName");

            migrationBuilder.CreateIndex(
                name: "IX_FileImage_OriginalName",
                table: "FileImage",
                column: "OriginalName");

            migrationBuilder.CreateIndex(
                name: "IX_GeoCity_IsActive_SortOrder",
                table: "GeoCity",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_GeoCity_Name_Governorate",
                table: "GeoCity",
                columns: new[] { "Name", "Governorate" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_ExpiresAtUtc",
                table: "IdempotencyRecord",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_Scope_Key",
                table: "IdempotencyRecord",
                columns: new[] { "Scope", "Key" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlan_Code",
                table: "InstallmentPlan",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOutboxMessage_CorrelationId",
                table: "IntegrationOutboxMessage",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOutboxMessage_ProcessedAtUtc_OccurredAtUtc",
                table: "IntegrationOutboxMessage",
                columns: new[] { "ProcessedAtUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBulkImportError_JobId",
                table: "InventoryBulkImportError",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBulkImportError_JobId_RowNumber",
                table: "InventoryBulkImportError",
                columns: new[] { "JobId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBulkImportJob_CreatedAtUtc",
                table: "InventoryBulkImportJob",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBulkImportJob_CreatedById",
                table: "InventoryBulkImportJob",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBulkImportJob_JobStatus",
                table: "InventoryBulkImportJob",
                column: "JobStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBulkImportJob_JobType",
                table: "InventoryBulkImportJob",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_MsisdnAsset_IntendedSubscriptionTypeId",
                table: "MsisdnAsset",
                column: "IntendedSubscriptionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MsisdnAsset_Msisdn",
                table: "MsisdnAsset",
                column: "Msisdn",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MsisdnAsset_PoolStatus_CreatedAtUtc",
                table: "MsisdnAsset",
                columns: new[] { "PoolStatus", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MsisdnAsset_PoolStatus_Msisdn",
                table: "MsisdnAsset",
                columns: new[] { "PoolStatus", "Msisdn" },
                filter: "[IsDeleted] = 0 AND [PoolStatus] IN (0, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_MsisdnAsset_ProductId",
                table: "MsisdnAsset",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MsisdnAsset_SubscriberProfileId",
                table: "MsisdnAsset",
                column: "SubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequence_EntityName",
                table: "NumberSequence",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnit_IsActive",
                table: "OrgUnit",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnit_ManagerUserId",
                table: "OrgUnit",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnit_ParentId",
                table: "OrgUnit",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_PricePlan_ProductOfferingId",
                table: "PricePlan",
                column: "ProductOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CompatibleSubscriptionTypeId",
                table: "Product",
                column: "CompatibleSubscriptionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Name",
                table: "Product",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Number",
                table: "Product",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffering_Code",
                table: "ProductOffering",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffering_CompatibleSubscriptionTypeId",
                table: "ProductOffering",
                column: "CompatibleSubscriptionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffering_ProductId",
                table: "ProductOffering",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOfferingComponent_ProductOfferingId",
                table: "ProductOfferingComponent",
                column: "ProductOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOfferingComponent_RequiresProductOfferingId",
                table: "ProductOfferingComponent",
                column: "RequiresProductOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_SimInventory_Iccid",
                table: "SimInventory",
                column: "Iccid",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SimInventory_Status_CreatedAtUtc",
                table: "SimInventory",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SimInventory_SubscriberProfileId",
                table: "SimInventory",
                column: "SubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberActiveService_Msisdn",
                table: "SubscriberActiveService",
                column: "Msisdn");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberActiveService_TelecomSubscriptionId_TelecomValueAddedServiceId",
                table: "SubscriberActiveService",
                columns: new[] { "TelecomSubscriptionId", "TelecomValueAddedServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberActiveService_TelecomValueAddedServiceId",
                table: "SubscriberActiveService",
                column: "TelecomValueAddedServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberProfile_CustomerId",
                table: "SubscriberProfile",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberProfile_CustomerId_OperationalStatus",
                table: "SubscriberProfile",
                columns: new[] { "CustomerId", "OperationalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberProfile_MasterSubscriberProfileId",
                table: "SubscriberProfile",
                column: "MasterSubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomIntegrationLog_IntegrationSystem",
                table: "TelecomIntegrationLog",
                column: "IntegrationSystem");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomIntegrationLog_Msisdn",
                table: "TelecomIntegrationLog",
                column: "Msisdn",
                filter: "[Msisdn] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomIntegrationLog_OccurredAtUtc",
                table: "TelecomIntegrationLog",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomMsisdnChangeLog_CreatedAtUtc",
                table: "TelecomMsisdnChangeLog",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomMsisdnChangeLog_CustomerId",
                table: "TelecomMsisdnChangeLog",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomMsisdnChangeLog_MsisdnAssetId",
                table: "TelecomMsisdnChangeLog",
                column: "MsisdnAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationAuditLog_OccurredAtUtc",
                table: "TelecomOperationAuditLog",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationAuditLog_TelecomOperationRequestId",
                table: "TelecomOperationAuditLog",
                column: "TelecomOperationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_ActivationChannel",
                table: "TelecomOperationRequest",
                column: "ActivationChannel");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_BranchId",
                table: "TelecomOperationRequest",
                column: "BranchId",
                filter: "[BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_CorrelationId",
                table: "TelecomOperationRequest",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_DealerCode",
                table: "TelecomOperationRequest",
                column: "DealerCode",
                filter: "[DealerCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_DeviceInventoryId",
                table: "TelecomOperationRequest",
                column: "DeviceInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_InstallmentPlanId",
                table: "TelecomOperationRequest",
                column: "InstallmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_MsisdnAssetId",
                table: "TelecomOperationRequest",
                column: "MsisdnAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_Number",
                table: "TelecomOperationRequest",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_ProductId",
                table: "TelecomOperationRequest",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_ProductOfferingId",
                table: "TelecomOperationRequest",
                column: "ProductOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_SecondarySubscriberProfileId",
                table: "TelecomOperationRequest",
                column: "SecondarySubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_SimInventoryId",
                table: "TelecomOperationRequest",
                column: "SimInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomOperationRequest_SubscriberProfileId",
                table: "TelecomOperationRequest",
                column: "SubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomPaymentAuditLog_TelecomPaymentTransactionId_OccurredAtUtc",
                table: "TelecomPaymentAuditLog",
                columns: new[] { "TelecomPaymentTransactionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelecomPaymentTransaction_CorrelationId",
                table: "TelecomPaymentTransaction",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomPaymentTransaction_CustomerId",
                table: "TelecomPaymentTransaction",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomPaymentTransaction_GatewayReference",
                table: "TelecomPaymentTransaction",
                column: "GatewayReference",
                unique: true,
                filter: "[GatewayReference] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomPaymentTransaction_Msisdn_CreatedAtUtc",
                table: "TelecomPaymentTransaction",
                columns: new[] { "Msisdn", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelecomPaymentTransaction_Number",
                table: "TelecomPaymentTransaction",
                column: "Number",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomSubscription_MsisdnAssetId",
                table: "TelecomSubscription",
                column: "MsisdnAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomSubscription_ProductId",
                table: "TelecomSubscription",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomSubscription_ProductOfferingId",
                table: "TelecomSubscription",
                column: "ProductOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomSubscription_SubscriberProfileId",
                table: "TelecomSubscription",
                column: "SubscriberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomSubscription_SubscriptionTypeId",
                table: "TelecomSubscription",
                column: "SubscriptionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomSubscriptionTypes_Code",
                table: "TelecomSubscriptionTypes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomTechnicalTicket_Msisdn",
                table: "TelecomTechnicalTicket",
                column: "Msisdn");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomTechnicalTicket_Status",
                table: "TelecomTechnicalTicket",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomTechnicalTicket_TicketCategory",
                table: "TelecomTechnicalTicket",
                column: "TicketCategory");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomTechnicalTicket_TicketNumber",
                table: "TelecomTechnicalTicket",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelecomValueAddedService_IsActive",
                table: "TelecomValueAddedService",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TelecomValueAddedService_ServiceCode",
                table: "TelecomValueAddedService",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLog_ActorUserId",
                table: "UserAuditLog",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLog_OccurredAtUtc",
                table: "UserAuditLog",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLog_UserId",
                table: "UserAuditLog",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_OrgUnit_OrgUnitId",
                table: "AspNetUsers",
                column: "OrgUnitId",
                principalTable: "OrgUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrgUnit_AspNetUsers_ManagerUserId",
                table: "OrgUnit");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BillingIntegrationLog");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "CustomerContact");

            migrationBuilder.DropTable(
                name: "CustomerIdentityDocument");

            migrationBuilder.DropTable(
                name: "DashboardWidget");

            migrationBuilder.DropTable(
                name: "DeviceInstallmentScheduleLine");

            migrationBuilder.DropTable(
                name: "FileImage");

            migrationBuilder.DropTable(
                name: "GeoCity");

            migrationBuilder.DropTable(
                name: "GlobalSetting");

            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropTable(
                name: "IntegrationOutboxMessage");

            migrationBuilder.DropTable(
                name: "InventoryBulkImportError");

            migrationBuilder.DropTable(
                name: "NumberSequence");

            migrationBuilder.DropTable(
                name: "PricePlan");

            migrationBuilder.DropTable(
                name: "ProductOfferingComponent");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "SubscriberActiveService");

            migrationBuilder.DropTable(
                name: "TelecomIntegrationLog");

            migrationBuilder.DropTable(
                name: "TelecomMsisdnChangeLog");

            migrationBuilder.DropTable(
                name: "TelecomOperationAuditLog");

            migrationBuilder.DropTable(
                name: "TelecomPaymentAuditLog");

            migrationBuilder.DropTable(
                name: "TelecomTechnicalTicket");

            migrationBuilder.DropTable(
                name: "Token");

            migrationBuilder.DropTable(
                name: "UserAuditLog");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "FileDocument");

            migrationBuilder.DropTable(
                name: "DeviceInstallmentContract");

            migrationBuilder.DropTable(
                name: "InventoryBulkImportJob");

            migrationBuilder.DropTable(
                name: "TelecomSubscription");

            migrationBuilder.DropTable(
                name: "TelecomValueAddedService");

            migrationBuilder.DropTable(
                name: "TelecomPaymentTransaction");

            migrationBuilder.DropTable(
                name: "TelecomOperationRequest");

            migrationBuilder.DropTable(
                name: "DeviceInventory");

            migrationBuilder.DropTable(
                name: "InstallmentPlan");

            migrationBuilder.DropTable(
                name: "MsisdnAsset");

            migrationBuilder.DropTable(
                name: "ProductOffering");

            migrationBuilder.DropTable(
                name: "SimInventory");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "SubscriberProfile");

            migrationBuilder.DropTable(
                name: "TelecomSubscriptionTypes");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "CustomerCategory");

            migrationBuilder.DropTable(
                name: "CustomerGroup");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "OrgUnit");
        }
    }
}
