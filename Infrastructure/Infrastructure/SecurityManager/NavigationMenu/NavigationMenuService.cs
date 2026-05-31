using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using TechnicalTicketStatus = Domain.Enums.TechnicalTicketStatus;

namespace Infrastructure.SecurityManager.NavigationMenu;

public class NavigationMenuService : INavigationMenuService
{
    private readonly IQueryContext _query;

    public NavigationMenuService(IQueryContext query) => _query = query;

    public List<MenuNavigationTreeNodeDto> GetMenuForRoles(
        IReadOnlyList<string> roleNames,
        TelecomMenuPersona? previewPersona = null,
        IReadOnlySet<string>? permissionKeys = null)
    {
        var nodes = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode();

        if (previewPersona.HasValue)
        {
            return NavigationTreeStructure.ApplyPersonaMenuFilter(roleNames, nodes, previewPersona);
        }

        return NavigationTreeStructure.ApplyTelecomWorkspaceMenuFilter(
            roleNames,
            permissionKeys ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            nodes,
            previewPersona);
    }

    public async Task<MenuBadgesDto> GetMenuBadgesAsync(CancellationToken cancellationToken = default)
    {
        var pendingOps = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                o => o.Status == TelecomOperationStatus.PendingDocuments
                    || o.Status == TelecomOperationStatus.Confirmed,
                cancellationToken);

        var bulkActive = await _query.InventoryBulkImportJob.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                j => j.JobStatus == InventoryBulkImportJobStatus.Pending
                    || j.JobStatus == InventoryBulkImportJobStatus.Processing,
                cancellationToken);

        var overdueCutoff = DateTime.UtcNow.AddHours(-4);
        var overdueTickets = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                t => (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress)
                    && t.Priority == TechnicalTicketPriority.Critical
                    && t.CreatedAtUtc != null
                    && t.CreatedAtUtc < overdueCutoff,
                cancellationToken);

        var openTechnicalTickets = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                t => t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress,
                cancellationToken);

        return new MenuBadgesDto
        {
            PendingOperations = pendingOps,
            OverdueTickets = overdueTickets,
            OpenTechnicalTickets = openTechnicalTickets,
            BulkImportActive = bulkActive,
        };
    }
}
