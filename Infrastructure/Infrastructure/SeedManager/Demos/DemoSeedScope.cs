using Application.Common.CQS.Queries;
using Application.Common.Telecom;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Central demo seed scope: <see cref="SystemActor"/>, branch RLS alignment, and national showcase anchors.
/// </summary>
public static class DemoSeedScope
{
    public const string SystemActor = "system-seed";

    public static void StampCustomer(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CreatedById))
        {
            customer.CreatedById = SystemActor;
        }
    }

    public static void ApplyProfileScope(SubscriberProfile profile, Customer customer)
    {
        profile.BranchId = customer.BranchId;
        if (string.IsNullOrWhiteSpace(profile.CreatedById))
        {
            profile.CreatedById = SystemActor;
        }
    }

    public static void ApplyProfileScope(SubscriberProfile profile, string? branchId)
    {
        profile.BranchId = branchId;
        if (string.IsNullOrWhiteSpace(profile.CreatedById))
        {
            profile.CreatedById = SystemActor;
        }
    }

    public static void ApplyMsisdnScope(MsisdnAsset asset, Customer customer)
    {
        asset.BranchId = customer.BranchId;
        if (string.IsNullOrWhiteSpace(asset.CreatedById))
        {
            asset.CreatedById = SystemActor;
        }
    }

    public static void ApplyMsisdnScope(MsisdnAsset asset, string? branchId)
    {
        asset.BranchId = branchId;
        if (string.IsNullOrWhiteSpace(asset.CreatedById))
        {
            asset.CreatedById = SystemActor;
        }
    }

    public static void ApplySimScope(SimInventory sim, string? branchId)
    {
        sim.BranchId = branchId;
        if (string.IsNullOrWhiteSpace(sim.CreatedById))
        {
            sim.CreatedById = SystemActor;
        }
    }

    public static void ApplyOperationScope(TelecomOperationRequest operation, string? branchId)
    {
        operation.BranchId = branchId;
        if (string.IsNullOrWhiteSpace(operation.CreatedById))
        {
            operation.CreatedById = SystemActor;
        }
    }

    public static void ApplyTicketScope(TelecomTechnicalTicket ticket, string? branchId)
    {
        ticket.BranchId = branchId;
        if (string.IsNullOrWhiteSpace(ticket.CreatedById))
        {
            ticket.CreatedById = SystemActor;
        }
    }

    public static async Task<string?> ResolveCustomerBranchIdAsync(DataContext context, string customerId) =>
        await ResolveCustomerBranchIdCoreAsync(context.Customer, customerId);

    public static async Task<string?> ResolveCustomerBranchIdAsync(IQueryContext query, string customerId) =>
        await ResolveCustomerBranchIdCoreAsync(query.Customer, customerId);

    private static async Task<string?> ResolveCustomerBranchIdCoreAsync(
        IQueryable<Customer> customers,
        string customerId) =>
        await customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.BranchId)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Idempotent branch alignment — safe on every startup.
    /// </summary>
    public static Task ReconcileBranchScopeAsync(DataContext context) =>
        context.Database.ExecuteSqlRawAsync(
            """
            SET QUOTED_IDENTIFIER ON;
            """ + ClearNationalDemoAnchorSql + CascadeNullBranchFromCustomerSql + SyncTelecomBranchScopeSql + SyncBillingLogBranchSql);

    /// <summary>
    /// Full demo reconcile including <see cref="SystemActor"/> stamps — run after demo seed only.
    /// </summary>
    public static Task ReconcileAllAsync(DataContext context) =>
        context.Database.ExecuteSqlRawAsync(
            """
            SET QUOTED_IDENTIFIER ON;
            """ + ClearNationalDemoAnchorSql + CascadeNullBranchFromCustomerSql + SyncTelecomBranchScopeSql + StampSystemActorSql + SyncBillingLogBranchSql);

    public static Task ReconcileTelecomBranchScopeAsync(DataContext context) =>
        ReconcileBranchScopeAsync(context);

    public static async Task<int> CountBranchScopeDriftAsync(DataContext context) =>
        await context.Database
            .SqlQueryRaw<int>(
                """
                SELECT
                    (SELECT COUNT(*)
                     FROM dbo.SubscriberProfile sp
                     INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
                     WHERE sp.IsDeleted = 0 AND c.IsDeleted = 0
                       AND ((c.BranchId IS NULL AND sp.BranchId IS NOT NULL)
                            OR (c.BranchId IS NOT NULL AND (sp.BranchId IS NULL OR sp.BranchId <> c.BranchId))))
                  + (SELECT COUNT(*)
                     FROM dbo.TelecomTechnicalTicket t
                     INNER JOIN dbo.Customer c ON c.Id = t.CustomerId
                     WHERE t.IsDeleted = 0 AND c.IsDeleted = 0 AND t.Status <> 3
                       AND ((c.BranchId IS NULL AND t.BranchId IS NOT NULL)
                            OR (c.BranchId IS NOT NULL AND (t.BranchId IS NULL OR t.BranchId <> c.BranchId))))
                AS [Value]
                """)
            .FirstOrDefaultAsync();

    private const string NationalMsisdnInList =
        "N'0939000000', N'0939000001', N'0939000002', N'0939000003', N'0939000005', N'0939000091'";

    private const string NationalCustomerPredicate =
        """
        c.PrimaryPhone IN (N'0939000000', N'0939000001', N'0939000002', N'0939000003', N'0939000005', N'0939000091')
                OR c.DisplayName LIKE N'%سعدون الشامي%'
                OR c.DisplayName LIKE N'%مازن المديون%'
        """;

    private static readonly string ClearNationalDemoAnchorSql =
        """
        UPDATE c
        SET OrgUnitId = NULL, BranchId = NULL
        FROM dbo.Customer c
        WHERE c.IsDeleted = 0
          AND (
        """
        + NationalCustomerPredicate +
        """
              );

        UPDATE sp
        SET BranchId = NULL
        FROM dbo.SubscriberProfile sp
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND (
        """
        + NationalCustomerPredicate +
        """
              );

        UPDATE m
        SET BranchId = NULL
        FROM dbo.MsisdnAsset m
        WHERE m.IsDeleted = 0
          AND m.Msisdn IN (
        """
        + NationalMsisdnInList +
        """
          );

        UPDATE sim
        SET BranchId = NULL
        FROM dbo.SimInventory sim
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = sim.SubscriberProfileId
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE sim.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND (
        """
        + NationalCustomerPredicate +
        """
              );

        UPDATE op
        SET BranchId = NULL
        FROM dbo.TelecomOperationRequest op
        INNER JOIN dbo.MsisdnAsset m ON m.Id = op.MsisdnAssetId
        WHERE op.IsDeleted = 0
          AND m.IsDeleted = 0
          AND m.Msisdn IN (
        """
        + NationalMsisdnInList +
        """
          );

        UPDATE t
        SET BranchId = NULL
        FROM dbo.TelecomTechnicalTicket t
        WHERE t.IsDeleted = 0
          AND t.Msisdn IN (
        """
        + NationalMsisdnInList +
        """
          );

        UPDATE t
        SET BranchId = NULL
        FROM dbo.TelecomTechnicalTicket t
        INNER JOIN dbo.Customer c ON c.Id = t.CustomerId
        WHERE t.IsDeleted = 0
          AND c.IsDeleted = 0
          AND (
        """
        + NationalCustomerPredicate +
        """
              );

        UPDATE bil
        SET BranchId = NULL
        FROM dbo.BillingIntegrationLog bil
        INNER JOIN dbo.TelecomOperationRequest op ON bil.TelecomOperationRequestId = op.Id
        INNER JOIN dbo.MsisdnAsset m ON m.Id = op.MsisdnAssetId
        WHERE bil.IsDeleted = 0
          AND op.IsDeleted = 0
          AND m.IsDeleted = 0
          AND m.Msisdn IN (
        """
        + NationalMsisdnInList +
        """
          );
        """;

    private const string CascadeNullBranchFromCustomerSql = """
        UPDATE sp
        SET BranchId = NULL
        FROM dbo.SubscriberProfile sp
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NULL
          AND sp.BranchId IS NOT NULL;

        UPDATE m
        SET BranchId = NULL
        FROM dbo.MsisdnAsset m
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = m.SubscriberProfileId
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE m.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NULL
          AND m.BranchId IS NOT NULL;

        UPDATE sim
        SET BranchId = NULL
        FROM dbo.SimInventory sim
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = sim.SubscriberProfileId
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE sim.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NULL
          AND sim.BranchId IS NOT NULL;

        UPDATE op
        SET BranchId = NULL
        FROM dbo.TelecomOperationRequest op
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = op.SubscriberProfileId
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE op.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NULL
          AND op.BranchId IS NOT NULL;

        UPDATE t
        SET BranchId = NULL
        FROM dbo.TelecomTechnicalTicket t
        INNER JOIN dbo.Customer c ON c.Id = t.CustomerId
        WHERE t.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NULL
          AND t.BranchId IS NOT NULL;

        UPDATE bil
        SET BranchId = NULL
        FROM dbo.BillingIntegrationLog bil
        INNER JOIN dbo.TelecomOperationRequest op ON bil.TelecomOperationRequestId = op.Id
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = op.SubscriberProfileId
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE bil.IsDeleted = 0
          AND op.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NULL
          AND bil.BranchId IS NOT NULL;
        """;

    private const string SyncTelecomBranchScopeSql = """
        UPDATE sp
        SET BranchId = c.BranchId
        FROM dbo.SubscriberProfile sp
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NOT NULL
          AND (sp.BranchId IS NULL OR sp.BranchId <> c.BranchId);

        UPDATE m
        SET BranchId = c.BranchId
        FROM dbo.MsisdnAsset m
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = m.SubscriberProfileId
        INNER JOIN dbo.Customer c ON c.Id = sp.CustomerId
        WHERE m.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NOT NULL
          AND (m.BranchId IS NULL OR m.BranchId <> c.BranchId);

        UPDATE sim
        SET BranchId = sp.BranchId
        FROM dbo.SimInventory sim
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = sim.SubscriberProfileId
        WHERE sim.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND sp.BranchId IS NOT NULL
          AND (sim.BranchId IS NULL OR sim.BranchId <> sp.BranchId);

        UPDATE op
        SET BranchId = sp.BranchId
        FROM dbo.TelecomOperationRequest op
        INNER JOIN dbo.SubscriberProfile sp ON sp.Id = op.SubscriberProfileId
        WHERE op.IsDeleted = 0
          AND sp.IsDeleted = 0
          AND sp.BranchId IS NOT NULL
          AND (op.BranchId IS NULL OR op.BranchId <> sp.BranchId);

        UPDATE t
        SET BranchId = c.BranchId
        FROM dbo.TelecomTechnicalTicket t
        INNER JOIN dbo.Customer c ON c.Id = t.CustomerId
        WHERE t.IsDeleted = 0
          AND c.IsDeleted = 0
          AND c.BranchId IS NOT NULL
          AND t.Status <> 3
          AND (t.BranchId IS NULL OR t.BranchId <> c.BranchId);
        """;

    private const string StampSystemActorSql = """
        UPDATE dbo.Customer SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.SubscriberProfile SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.MsisdnAsset SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.SimInventory SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.TelecomSubscription SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.TelecomOperationRequest SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.TelecomTechnicalTicket SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        UPDATE dbo.CustomerContact SET CreatedById = N'system-seed' WHERE IsDeleted = 0 AND (CreatedById IS NULL OR CreatedById = N'');
        """;

    private const string SyncBillingLogBranchSql = """
        UPDATE bil
        SET BranchId = op.BranchId
        FROM dbo.BillingIntegrationLog bil
        INNER JOIN dbo.TelecomOperationRequest op ON bil.TelecomOperationRequestId = op.Id
        WHERE bil.IsDeleted = 0
          AND op.BranchId IS NOT NULL
          AND (bil.BranchId IS NULL OR bil.BranchId <> op.BranchId);

        UPDATE bil
        SET BranchId = pt.BranchId
        FROM dbo.BillingIntegrationLog bil
        INNER JOIN dbo.TelecomPaymentTransaction pt ON bil.TelecomPaymentTransactionId = pt.Id
        WHERE bil.IsDeleted = 0
          AND pt.BranchId IS NOT NULL
          AND (bil.BranchId IS NULL OR bil.BranchId <> pt.BranchId);

        UPDATE bil
        SET BranchId = NULL
        FROM dbo.BillingIntegrationLog bil
        INNER JOIN dbo.TelecomOperationRequest op ON bil.TelecomOperationRequestId = op.Id
        WHERE bil.IsDeleted = 0
          AND op.BranchId IS NULL
          AND bil.BranchId IS NOT NULL;
        """;

}
