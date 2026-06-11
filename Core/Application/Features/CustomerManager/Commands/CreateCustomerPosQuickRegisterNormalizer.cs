using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Commands;

public static class CreateCustomerPosQuickRegisterNormalizer
{
    public static async Task ApplyAsync(
        CreateCustomerRequest request,
        IQueryContext query,
        CancellationToken cancellationToken = default)
    {
        if (!request.PosQuickRegister)
        {
            return;
        }

        request.Street = Coalesce(request.Street, "-");
        request.City = Coalesce(request.City, "دمشق");
        request.State = Coalesce(request.State, "دمشق");
        request.ZipCode = Coalesce(request.ZipCode, "00000");
        request.Country = Coalesce(request.Country, "SY");

        var kind = request.CustomerKind ?? CustomerKind.Individual;
        var identityKey = kind == CustomerKind.Corporate
            ? (request.CommercialRegistration ?? "corp").Trim()
            : (request.NationalId ?? "new").Trim();

        if (string.IsNullOrWhiteSpace(request.EmailAddress))
        {
            request.EmailAddress = $"pos+{identityKey}@subscriber.local";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerGroupId))
        {
            var groups = await query.CustomerGroup
                .AsNoTracking()
                .IsDeletedEqualTo(false)
                .OrderBy(g => g.Name)
                .ToListAsync(cancellationToken);

            var preferred = kind == CustomerKind.Corporate
                ? groups.FirstOrDefault(g => g.Name != null && g.Name.Contains("شركات", StringComparison.Ordinal))
                : groups.FirstOrDefault(g => g.Name != null && g.Name.Contains("أفراد", StringComparison.Ordinal));

            request.CustomerGroupId = preferred?.Id ?? groups.FirstOrDefault()?.Id;
        }

        if (string.IsNullOrWhiteSpace(request.CustomerCategoryId))
        {
            var categories = await query.CustomerCategory
                .AsNoTracking()
                .IsDeletedEqualTo(false)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var preferred = kind == CustomerKind.Corporate
                ? categories.FirstOrDefault(c => c.Name != null && c.Name.Contains("شركات", StringComparison.Ordinal))
                : categories.FirstOrDefault(c => c.Name != null && c.Name.Contains("أفراد", StringComparison.Ordinal));

            request.CustomerCategoryId = preferred?.Id ?? categories.FirstOrDefault()?.Id;
        }
    }

    private static string Coalesce(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
