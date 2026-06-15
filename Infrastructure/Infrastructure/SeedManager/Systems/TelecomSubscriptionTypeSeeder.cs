using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Systems;

/// <summary>
/// Seeds canonical Prepaid / Postpaid / Hybrid rows required by FK on <see cref="TelecomSubscription"/>.
/// EF migrations create the table only; reference data must be inserted at runtime.
/// </summary>
public sealed class TelecomSubscriptionTypeSeeder
{
    private readonly DataContext _context;

    public TelecomSubscriptionTypeSeeder(DataContext context) => _context = context;

    public async Task EnsureReferenceDataAsync()
    {
        var existingIds = await _context.TelecomSubscriptionTypeLookup
            .IgnoreQueryFilters()
            .Select(t => t.Id)
            .ToHashSetAsync();

        var seedRows = new (string Id, string Code, string NameAr, string NameEn, string Color, int SortOrder, bool IsDefault)[]
        {
            (TelecomSubscriptionTypeWellKnownIds.Prepaid, "PREPAID", "مسبق الدفع", "Prepaid", "#0d6efd", 1, true),
            (TelecomSubscriptionTypeWellKnownIds.Postpaid, "POSTPAID", "آجل الدفع", "Postpaid", "#198754", 2, false),
            (TelecomSubscriptionTypeWellKnownIds.Hybrid, "HYBRID", "هجين", "Hybrid", "#6f42c1", 3, false),
        };

        var added = false;
        foreach (var row in seedRows)
        {
            if (existingIds.Contains(row.Id))
            {
                continue;
            }

            await _context.TelecomSubscriptionTypeLookup.AddAsync(new TelecomSubscriptionTypeLookup
            {
                Id = row.Id,
                Code = row.Code,
                NameAr = row.NameAr,
                NameEn = row.NameEn,
                DisplayColor = row.Color,
                SortOrder = row.SortOrder,
                IsActive = true,
                IsDefault = row.IsDefault,
                CreatedAtUtc = DateTime.UtcNow,
            });
            added = true;
        }

        if (added)
        {
            await _context.SaveChangesAsync();
        }
    }
}
