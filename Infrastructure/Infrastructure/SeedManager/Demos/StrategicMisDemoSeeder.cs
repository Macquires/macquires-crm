using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Enriches demo data for <c>/Telecom/StrategicAnalytics</c>: branch-scoped customers,
/// resolved tickets (SLA), and MSISDN tier mix per region.
/// </summary>
public sealed class StrategicMisDemoSeeder
{
    private readonly DataContext _context;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly NumberSequenceService _numberSequence;
    private readonly IUnitOfWork _unitOfWork;

    public StrategicMisDemoSeeder(
        DataContext context,
        ICommandRepository<Customer> customerRepository,
        NumberSequenceService numberSequence,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _customerRepository = customerRepository;
        _numberSequence = numberSequence;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        await AssignOrgUnitsToCustomersAsync();
        await EnsureBranchCustomersAsync();
        await EnsureMsisdnTierMixAsync();
        await EnsureResolvedTicketsForSlaAsync();
    }

    private async Task AssignOrgUnitsToCustomersAsync()
    {
        var branches = await _context.OrgUnit
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Branch)
            .ToListAsync();

        if (branches.Count == 0)
        {
            return;
        }

        var defaultBranch = branches.FirstOrDefault(b => b.NameAr.Contains("المزة", StringComparison.Ordinal))
            ?? branches[0];

        var cityBranch = new Dictionary<string, OrgUnit>(StringComparer.OrdinalIgnoreCase)
        {
            ["دمشق"] = branches.FirstOrDefault(b => b.NameAr.Contains("المزة", StringComparison.Ordinal)) ?? defaultBranch,
            ["حلب"] = branches.FirstOrDefault(b => b.NameAr.Contains("حلب", StringComparison.Ordinal)) ?? defaultBranch,
            ["اللاذقية"] = branches.FirstOrDefault(b => b.NameAr.Contains("اللاذقية", StringComparison.Ordinal)) ?? defaultBranch,
            ["طرطوس"] = branches.FirstOrDefault(b => b.NameAr.Contains("طرطوس", StringComparison.Ordinal)) ?? defaultBranch,
            ["إدلب"] = branches.FirstOrDefault(b => b.NameAr.Contains("إدلب", StringComparison.Ordinal)) ?? defaultBranch,
        };

        var customers = await _context.Customer
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        var changed = false;
        foreach (var customer in customers)
        {
            if (!string.IsNullOrEmpty(customer.OrgUnitId))
            {
                continue;
            }

            var city = customer.Address.City;
            var branch = !string.IsNullOrEmpty(city) && cityBranch.TryGetValue(city, out var b) ? b : defaultBranch;
            customer.SetOrgUnitId(branch.Id);
            changed = true;
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task EnsureBranchCustomersAsync()
    {
        var branches = await _context.OrgUnit
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Branch)
            .ToListAsync();

        if (branches.Count == 0)
        {
            return;
        }

        var groups = await _context.CustomerGroup.Where(x => !x.IsDeleted).Select(x => x.Id).ToListAsync();
        var categories = await _context.CustomerCategory.Where(x => !x.IsDeleted).Select(x => x.Id).ToListAsync();
        if (groups.Count == 0 || categories.Count == 0)
        {
            return;
        }

        var branchCustomerCounts = await _context.Customer
            .Where(c => !c.IsDeleted && c.OrgUnitId != null)
            .GroupBy(c => c.OrgUnitId!)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BranchId, x => x.Count);

        var rnd = new Random(42);
        var added = false;

        var extras = new (string BranchKeyword, string City, string Name)[]
        {
            ("المزة", "دمشق", "عمر حمود — المزة"),
            ("أبو رمانة", "دمشق", "سلمى قاسم — أبو رمانة"),
            ("الحجاز", "دمشق", "فادي ناصر — الحجاز"),
            ("حلب", "حلب", "كريم الأسعد — حلب"),
            ("إدلب", "إدلب", "نور الهدى — إدلب"),
            ("اللاذقية", "اللاذقية", "لينا يوسف — اللاذقية"),
            ("طرطوس", "طرطوس", "بسام جولاني — طرطوس"),
        };

        foreach (var branch in branches)
        {
            branchCustomerCounts.TryGetValue(branch.Id, out var count);
            if (count >= 2)
            {
                continue;
            }

            var match = extras.FirstOrDefault(e => branch.NameAr.Contains(e.BranchKeyword, StringComparison.Ordinal));
            if (match == default)
            {
                continue;
            }

            var address = new PostalAddress("شارع تجاري", match.City, match.City, "10001", "سوريا");
            var account = _numberSequence.GenerateNumber(nameof(Customer), "", "CST");
            var phone = $"093{rnd.Next(1000000, 9999999)}";
            var email = $"strategic.{branch.Id[..Math.Min(8, branch.Id.Length)]}@syriatel-demo.local";

            var entity = IndividualCustomer.Create(
                match.Name,
                account,
                $"NID-{rnd.Next(100000, 999999)}",
                address,
                email,
                phone,
                groups[rnd.Next(groups.Count)],
                categories[rnd.Next(categories.Count)]);

            entity.SetOrgUnitId(branch.Id);
            await _customerRepository.CreateAsync(entity);
            added = true;
        }

        if (added)
        {
            await _unitOfWork.SaveAsync();
        }
    }

    private async Task EnsureMsisdnTierMixAsync()
    {
        var assets = await _context.MsisdnAsset
            .Where(m => !m.IsDeleted && m.PoolStatus == MsisdnPoolStatus.Active)
            .OrderBy(m => m.Msisdn)
            .Take(60)
            .ToListAsync();

        if (assets.Count == 0)
        {
            return;
        }

        var tiers = new[] { MsisdnCategory.Platinum, MsisdnCategory.Gold, MsisdnCategory.Silver, MsisdnCategory.Normal };
        for (var i = 0; i < assets.Count; i++)
        {
            assets[i].Category = tiers[i % tiers.Length];
        }

        await _context.SaveChangesAsync();
    }

    private async Task EnsureResolvedTicketsForSlaAsync()
    {
        var tickets = await _context.TelecomTechnicalTicket
            .Where(t => !t.IsDeleted && t.CustomerId != null)
            .OrderBy(t => t.CreatedAtUtc)
            .Take(12)
            .ToListAsync();

        if (tickets.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var changed = false;
        for (var i = 0; i < tickets.Count; i++)
        {
            var t = tickets[i];
            if (t.Status == TechnicalTicketStatus.Resolved && t.ResolvedAtUtc.HasValue)
            {
                continue;
            }

            t.Status = TechnicalTicketStatus.Resolved;
            t.CreatedAtUtc ??= now.AddDays(-3);
            var targetHours = t.Priority switch
            {
                TechnicalTicketPriority.Critical => 3,
                TechnicalTicketPriority.High => 6,
                _ => 12,
            };
            t.ResolvedAtUtc = t.CreatedAtUtc.Value.AddHours(targetHours);
            t.ResolvedByUserId ??= t.OpenedByUserId;
            changed = true;
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
        }
    }
}
