using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Demo technical tickets always bound to real <see cref="TelecomSubscription"/> / MSISDN rows.
/// </summary>
public class TelecomTechnicalTicketSeeder
{
    private sealed record ActiveLine(
        string SubscriberProfileId,
        string? CustomerId,
        string Msisdn,
        string? CustomerDisplayName,
        string MsisdnAssetId,
        string? ProductId);

    private sealed record TicketBlueprint(
        TechnicalTicketIssueType IssueType,
        TechnicalTicketCategory TicketCategory,
        TechnicalTicketPriority Priority,
        string Title,
        string Summary,
        bool PreferHero);

    private static readonly TicketBlueprint[] Blueprints =
    [
        new(TechnicalTicketIssueType.Billing, TechnicalTicketCategory.Complaint, TechnicalTicketPriority.Critical,
            "شحن كاش والنت واقف", "أخي شحنت كاش والنت واقف عندي — الرصيد لم يُطبّق على CBS.", PreferHero: true),
        new(TechnicalTicketIssueType.Network, TechnicalTicketCategory.Complaint, TechnicalTicketPriority.Critical,
            "ضعف تغطية - قدسيا", "انقطاع متكرر 4G — مسح شبكة مطلوب.", PreferHero: false),
        new(TechnicalTicketIssueType.SimBlock, TechnicalTicketCategory.SimSwap, TechnicalTicketPriority.High,
            "طلب تبديل شريحة — معلّق", "تبديل SIM من المعرض بانتظار مزامنة HLR.", PreferHero: false),
        new(TechnicalTicketIssueType.Provisioning, TechnicalTicketCategory.PackageMigration, TechnicalTicketPriority.Medium,
            "ترحيل باقة MGR — معلّق", "ترحيل باقة من Customer 360 بانتظار تسوية CBS.", PreferHero: false),
        new(TechnicalTicketIssueType.Provisioning, TechnicalTicketCategory.LineActivation, TechnicalTicketPriority.High,
            "تفعيل خط معلّق", "تفعيل خط جديد بانتظار مزامنة الشبكة.", PreferHero: false),
    ];

    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomTechnicalTicket> _repository;
    private readonly NumberSequenceService _numberSequence;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomTechnicalTicketSeeder(
        IQueryContext query,
        ICommandRepository<TelecomTechnicalTicket> repository,
        NumberSequenceService numberSequence,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _repository = repository;
        _numberSequence = numberSequence;
        _unitOfWork = unitOfWork;
    }

    public Task GenerateDataAsync() => EnsureDemoTicketsAsync();

    /// <summary>Creates or repairs tickets so every row links to an active subscription MSISDN.</summary>
    public async Task EnsureDemoTicketsAsync()
    {
        var lines = await LoadActiveLinesAsync();
        if (lines.Count == 0)
        {
            return;
        }

        var systemUserId = "system-seed";
        var existing = await _repository.GetQuery().Where(t => !t.IsDeleted).OrderBy(t => t.CreatedAtUtc).ToListAsync();
        var usedLineKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < Blueprints.Length; i++)
        {
            var blueprint = Blueprints[i];
            var line = PickLine(lines, blueprint, usedLineKeys);
            usedLineKeys.Add(line.Msisdn);

            if (i < existing.Count)
            {
                await ApplyLineToTicketAsync(existing[i], line, blueprint, systemUserId);
                continue;
            }

            await _repository.CreateAsync(BuildTicket(line, blueprint, systemUserId));
        }

        for (var i = Blueprints.Length; i < existing.Count; i++)
        {
            var line = lines[i % lines.Count];
            await ApplyLineToTicketAsync(existing[i], line, Blueprints[i % Blueprints.Length], systemUserId);
        }

        await _unitOfWork.SaveAsync();
    }

    private static ActiveLine PickLine(
        IReadOnlyList<ActiveLine> lines,
        TicketBlueprint blueprint,
        ISet<string> usedMsisdns)
    {
        if (blueprint.PreferHero)
        {
            var hero = lines.FirstOrDefault(l =>
                !usedMsisdns.Contains(l.Msisdn)
                && (l.Msisdn == TelecomDemoMsisdn.Hero
                    || (l.CustomerDisplayName?.Contains("سعدون", StringComparison.Ordinal) == true)));
            if (hero != null)
            {
                return hero;
            }
        }

        return lines.First(l => !usedMsisdns.Contains(l.Msisdn));
    }

    private async Task<List<ActiveLine>> LoadActiveLinesAsync()
    {
        return await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s =>
                s.MsisdnAssetId != null
                && s.MsisdnAsset != null
                && !s.MsisdnAsset.IsDeleted
                && s.MsisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
            .OrderByDescending(s => s.IsPrimaryLine)
            .ThenBy(s => s.MsisdnAsset!.Msisdn)
            .Select(s => new ActiveLine(
                s.SubscriberProfileId,
                s.SubscriberProfile != null ? s.SubscriberProfile.CustomerId : null,
                s.MsisdnAsset!.Msisdn,
                s.SubscriberProfile != null && s.SubscriberProfile.Customer != null
                    ? s.SubscriberProfile.Customer.DisplayName
                    : null,
                s.MsisdnAssetId!,
                s.ProductId))
            .ToListAsync();
    }

    private TelecomTechnicalTicket BuildTicket(ActiveLine line, TicketBlueprint blueprint, string systemUserId)
    {
        var summary = blueprint.Summary.Replace("\"", "'", StringComparison.Ordinal);
        return new TelecomTechnicalTicket
        {
            CreatedById = systemUserId,
            TicketNumber = _numberSequence.GenerateNumber(nameof(TelecomTechnicalTicket), "", "TT"),
            Msisdn = line.Msisdn,
            CustomerId = line.CustomerId,
            SubscriberProfileId = line.SubscriberProfileId,
            IssueType = blueprint.IssueType,
            TicketCategory = blueprint.TicketCategory,
            Priority = blueprint.Priority,
            Status = TechnicalTicketStatus.Open,
            Notes = blueprint.Title,
            PayloadJson = $"{{\"summary\":\"{summary}\",\"msisdnAssetId\":\"{line.MsisdnAssetId}\"}}",
            OpenedByUserId = systemUserId,
            CreatedByChannel = TechnicalTicketCreatedByChannel.CallCenterAgent,
        };
    }

    private async Task ApplyLineToTicketAsync(
        TelecomTechnicalTicket ticket,
        ActiveLine line,
        TicketBlueprint blueprint,
        string systemUserId)
    {
        var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(line.Msisdn) ?? line.Msisdn;
        var needsLink =
            string.IsNullOrEmpty(ticket.SubscriberProfileId)
            || string.IsNullOrEmpty(ticket.CustomerId)
            || ticket.Msisdn != canonical;

        if (!needsLink && ticket.IssueType == blueprint.IssueType)
        {
            return;
        }

        ticket.Msisdn = canonical;
        ticket.SubscriberProfileId = line.SubscriberProfileId;
        ticket.CustomerId = line.CustomerId;
        ticket.IssueType = blueprint.IssueType;
        ticket.Priority = blueprint.Priority;
        ticket.Notes = blueprint.Title;
        ticket.PayloadJson =
            $"{{\"summary\":\"{blueprint.Summary.Replace("\"", "'", StringComparison.Ordinal)}\",\"msisdnAssetId\":\"{line.MsisdnAssetId}\"}}";
        ticket.UpdatedById = systemUserId;
        if (ticket.Status == TechnicalTicketStatus.Resolved)
        {
            ticket.Status = TechnicalTicketStatus.Open;
            ticket.ResolvedAtUtc = null;
            ticket.ResolvedByUserId = null;
        }

        _repository.Update(ticket);
        await Task.CompletedTask;
    }
}

/// <summary>Canonical demo MSISDNs aligned with <see cref="TelecomSyriatelSeeder"/>.</summary>
public static class TelecomDemoMsisdn
{
    public const string Hero = "0939000001";
    public const string DebtSubscriber = "0939000002";
    /// <summary>Secondary hero line — suspended (Fraud) for RCN §9 demo.</summary>
    public const string ReconnectFraudDemo = "0939000091";
}
