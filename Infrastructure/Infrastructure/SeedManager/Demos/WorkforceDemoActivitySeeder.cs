using Application.Common.Audit;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Enriches demo workforce analytics for Executive Command Center — operations, tickets, and audit
/// attributed to showroom / back-office / call-center personas.
/// </summary>
public sealed class WorkforceDemoActivitySeeder
{
    private const string SystemActor = "system-seed";

    private readonly DataContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkforceDemoActivitySeeder(DataContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task GenerateDataAsync()
    {
        var showroom = await _userManager.FindByEmailAsync("st-showroom@syriatelecom-demo.local");
        var backOffice = await _userManager.FindByEmailAsync("st-backoffice@syriatelecom-demo.local");
        var callCenter = await _userManager.FindByEmailAsync("st-callcenter@syriatelecom-demo.local");

        if (showroom == null && backOffice == null && callCenter == null)
        {
            return;
        }

        await TouchOnlinePresenceAsync(showroom, backOffice, callCenter);
        await AttributeOperationsAsync(showroom, backOffice);
        await AttributeTicketsAsync(callCenter, backOffice);
        await EnsureWorkforceAuditAsync(showroom, backOffice, callCenter);
        await _context.SaveChangesAsync();
    }

    private async Task TouchOnlinePresenceAsync(
        ApplicationUser? showroom,
        ApplicationUser? backOffice,
        ApplicationUser? callCenter)
    {
        var now = DateTime.UtcNow;
        foreach (var user in new[] { showroom, backOffice, callCenter })
        {
            if (user == null)
            {
                continue;
            }

            user.LastActivityAtUtc = now.AddMinutes(-Random.Shared.Next(1, 20));
            await _userManager.UpdateAsync(user);
        }
    }

    private async Task AttributeOperationsAsync(ApplicationUser? showroom, ApplicationUser? backOffice)
    {
        if (showroom == null && backOffice == null)
        {
            return;
        }

        var ops = await _context.TelecomOperationRequest
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(40)
            .ToListAsync();

        for (var i = 0; i < ops.Count; i++)
        {
            var actor = i % 3 == 0 ? backOffice : showroom;
            if (actor == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(ops[i].CreatedById) || ops[i].CreatedById == SystemActor)
            {
                ops[i].CreatedById = actor.Id;
                ops[i].UpdatedById = actor.Id;
            }

            if (i % 5 == 0 && ops[i].Status == TelecomOperationStatus.Completed)
            {
                ops[i].ClaimedByUserId = actor.Id;
            }
        }
    }

    private async Task AttributeTicketsAsync(ApplicationUser? callCenter, ApplicationUser? backOffice)
    {
        if (callCenter == null && backOffice == null)
        {
            return;
        }

        var tickets = await _context.TelecomTechnicalTicket
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(25)
            .ToListAsync();

        foreach (var (ticket, index) in tickets.Select((t, i) => (t, i)))
        {
            if (callCenter == null)
            {
                continue;
            }

            var isAgentFacing =
                string.Equals(ticket.CreatedByChannel, TechnicalTicketCreatedByChannel.CallCenterAgent, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ticket.CreatedByChannel, TechnicalTicketCreatedByChannel.CustomerCareVoiceAi, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ticket.CreatedByChannel, TechnicalTicketCreatedByChannel.ShowroomAgent, StringComparison.OrdinalIgnoreCase);

            if (isAgentFacing
                && !string.IsNullOrWhiteSpace(ticket.CustomerId)
                && index < 12)
            {
                ticket.OpenedByUserId = callCenter.Id;
            }

            if (backOffice != null
                && ticket.Status == TechnicalTicketStatus.Resolved
                && string.IsNullOrEmpty(ticket.ResolvedByUserId))
            {
                ticket.ResolvedByUserId = backOffice.Id;
                ticket.ResolvedAtUtc ??= DateTime.UtcNow.AddHours(-index);
            }
        }
    }

    private async Task EnsureWorkforceAuditAsync(
        ApplicationUser? showroom,
        ApplicationUser? backOffice,
        ApplicationUser? callCenter)
    {
        var marker = "workforce-demo-activity";
        var exists = await _context.UserAuditLog.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.PayloadJson != null && x.PayloadJson.Contains(marker));

        if (exists)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var entries = new List<(ApplicationUser User, string Action, string Summary)>();

        if (showroom != null)
        {
            entries.Add((showroom, UserAuditActionTypes.TelecomOperationConfirmed, "تفعيل خط جديد — معرض المزة"));
            entries.Add((showroom, UserAuditActionTypes.TelecomOperationConfirmed, "تأكيد عملية بيع جهاز"));
        }

        if (backOffice != null)
        {
            entries.Add((backOffice, UserAuditActionTypes.TelecomOperationConfirmed, "اعتماد عملية باك أوفيس"));
            entries.Add((backOffice, UserAuditActionTypes.StrategicReportViewed, "مراجعة تقرير MIS فرع طرطوس"));
        }

        if (callCenter != null)
        {
            entries.Add((callCenter, UserAuditActionTypes.CustomerViewed, "اطلاع على ملف مشترك — مركز الاتصال"));
            entries.Add((callCenter, UserAuditActionTypes.BackOfficeTicketClaimed, "فتح تذكرة دعم فني"));
        }

        var i = 0;
        foreach (var (user, action, summary) in entries)
        {
            _context.UserAuditLog.Add(new UserAuditLog
            {
                ActorUserId = user.Id,
                ActionType = action,
                SummaryAr = summary,
                PayloadJson = $"{{\"marker\":\"{marker}\"}}",
                OccurredAtUtc = now.AddHours(-i * 3),
                CreatedAtUtc = now.AddHours(-i * 3),
                IsDeleted = false,
            });
            i++;
        }
    }
}
