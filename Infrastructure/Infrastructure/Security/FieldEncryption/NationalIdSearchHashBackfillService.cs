using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.FieldEncryption;

public sealed class NationalIdSearchHashBackfillService : INationalIdSearchHashBackfillService
{
    private readonly DataContext _context;
    private readonly IFieldEncryptionService _encryption;
    private readonly IUnitOfWork _unitOfWork;

    public NationalIdSearchHashBackfillService(
        DataContext context,
        IFieldEncryptionService encryption,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _encryption = encryption;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> BackfillAllMissingAsync(CancellationToken cancellationToken = default)
    {
        var reservedHashes = await LoadReservedHashesAsync(cancellationToken);

        var missing = await _context.Customer
            .OfType<IndividualCustomer>()
            .Where(c => !c.IsDeleted && (c.NationalIdSearchHash == null || c.NationalIdSearchHash == ""))
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var backfilled = 0;
        foreach (var customer in missing)
        {
            if (!TryAssignSearchHash(customer, reservedHashes))
            {
                continue;
            }

            backfilled++;
        }

        if (backfilled > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return backfilled;
    }

    public async Task<string?> TryResolveLegacyIndividualIdAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        var normalized = nationalId.Trim();
        if (normalized.Length != 10)
        {
            return null;
        }

        var reservedHashes = await LoadReservedHashesAsync(cancellationToken);

        var missing = await _context.Customer
            .OfType<IndividualCustomer>()
            .Where(c => !c.IsDeleted && (c.NationalIdSearchHash == null || c.NationalIdSearchHash == ""))
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var matches = missing
            .Where(c => string.Equals(c.NationalId?.Trim(), normalized, StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        foreach (var match in matches)
        {
            if (TryAssignSearchHash(match, reservedHashes))
            {
                await _unitOfWork.SaveAsync(cancellationToken);
                return match.Id;
            }
        }

        return matches[0].Id;
    }

    private async Task<HashSet<string>> LoadReservedHashesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Customer
            .OfType<IndividualCustomer>()
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.NationalIdSearchHash != null && c.NationalIdSearchHash != "")
            .Select(c => c.NationalIdSearchHash!)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet(StringComparer.Ordinal);
    }

    private bool TryAssignSearchHash(IndividualCustomer customer, HashSet<string> reservedHashes)
    {
        var nationalId = customer.NationalId?.Trim();
        if (string.IsNullOrEmpty(nationalId))
        {
            return false;
        }

        var hash = _encryption.ComputeSearchHash(nationalId);
        if (!reservedHashes.Add(hash))
        {
            return false;
        }

        customer.SetNationalIdSearchHash(hash);
        return true;
    }
}
