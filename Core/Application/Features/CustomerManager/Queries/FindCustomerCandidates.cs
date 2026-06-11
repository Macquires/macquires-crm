using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record FindCustomerCandidateDto
{
    public string Id { get; init; } = "";
    public string? Name { get; init; }
    public string? PhoneNumber { get; init; }
    public string? City { get; init; }
    public string? EmailAddress { get; init; }
    public int TelecomSubscriptionLineCount { get; init; }
}

public class FindCustomerCandidatesResult
{
    public List<FindCustomerCandidateDto> Data { get; init; } = new();
}

public class FindCustomerCandidatesRequest : IRequest<FindCustomerCandidatesResult>
{
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
}

public class FindCustomerCandidatesValidator : AbstractValidator<FindCustomerCandidatesRequest>
{
    public FindCustomerCandidatesValidator()
    {
        RuleFor(x => x)
            .Must(x =>
            {
                var n = (x.NationalId ?? string.Empty).Trim();
                var p = (x.Phone ?? string.Empty).Trim();
                return n.Length >= 2 || p.Length >= 2;
            })
            .WithMessage("أدخل رقم هوية (حرفين على الأقل) أو رقم جوال (حرفين على الأقل) للبحث.");
    }
}

public class FindCustomerCandidatesHandler : IRequestHandler<FindCustomerCandidatesRequest, FindCustomerCandidatesResult>
{
    private const int MaxResults = 20;
    private readonly IQueryContext _context;
    private readonly IFieldEncryptionService _encryption;
    private readonly ISubscriberAccessAuditService _subscriberAudit;
    private readonly INationalIdSearchHashBackfillService _nationalIdBackfill;

    public FindCustomerCandidatesHandler(
        IQueryContext context,
        IFieldEncryptionService encryption,
        ISubscriberAccessAuditService subscriberAudit,
        INationalIdSearchHashBackfillService nationalIdBackfill)
    {
        _context = context;
        _encryption = encryption;
        _subscriberAudit = subscriberAudit;
        _nationalIdBackfill = nationalIdBackfill;
    }

    public async Task<FindCustomerCandidatesResult> Handle(
        FindCustomerCandidatesRequest request,
        CancellationToken cancellationToken)
    {
        var national = (request.NationalId ?? string.Empty).Trim();
        var nationalOk = national.Length >= 2;
        var nationalHash = national.Length == 10 ? _encryption.ComputeSearchHash(national) : null;

        var phoneRaw = (request.Phone ?? string.Empty).Trim();
        var phoneCanon = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(phoneRaw);
        var phoneDigits = TelecomPhoneNormalizer.DigitsOnly(phoneRaw);
        if (phoneDigits.Length > 2 && phoneDigits.StartsWith("963", StringComparison.Ordinal) && phoneCanon == null)
        {
            phoneDigits = phoneDigits.Length >= 12 ? "0" + phoneDigits[3..] : phoneDigits;
        }

        var phoneOk = phoneCanon != null || phoneDigits.Length >= 2;
        if (!nationalOk && !phoneOk)
        {
            return new FindCustomerCandidatesResult();
        }

        var identityMatchIds = new HashSet<string>();
        if (nationalOk)
        {
            if (nationalHash != null)
            {
                var individualIds = await _context.Customer
                    .AsNoTracking()
                    .IsDeletedEqualTo(false)
                    .OfType<IndividualCustomer>()
                    .Where(c => c.NationalIdSearchHash == nationalHash)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);
                foreach (var id in individualIds)
                {
                    identityMatchIds.Add(id);
                }

                if (individualIds.Count == 0)
                {
                    var legacyId = await _nationalIdBackfill.TryResolveLegacyIndividualIdAsync(
                        national,
                        cancellationToken);
                    if (!string.IsNullOrEmpty(legacyId))
                    {
                        identityMatchIds.Add(legacyId);
                    }
                }
            }

            var corporateIds = await _context.Customer
                .AsNoTracking()
                .IsDeletedEqualTo(false)
                .OfType<CorporateCustomer>()
                .Where(c => c.CommercialRegistryNumber != null && c.CommercialRegistryNumber.Contains(national))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            foreach (var id in corporateIds)
            {
                identityMatchIds.Add(id);
            }
        }

        if (nationalOk && !phoneOk && identityMatchIds.Count == 0)
        {
            await _subscriberAudit.LogSearchAsync(
                "CustomerRegistry",
                national,
                null,
                null,
                [],
                cancellationToken);
            return new FindCustomerCandidatesResult();
        }

        var query = _context.Customer
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(c => c.SubscriberProfiles)
            .ThenInclude(sp => sp.Subscriptions)
            .ThenInclude(s => s.MsisdnAsset)
            .AsQueryable();

        if (nationalOk && phoneOk)
        {
            var phoneNeedle = phoneCanon ?? phoneDigits;
            var ids = identityMatchIds.ToList();
            if (ids.Count > 0)
            {
                query = query.Where(c =>
                    ids.Contains(c.Id)
                    || (c.PrimaryPhone != null && c.PrimaryPhone.Contains(phoneNeedle)));
            }
            else
            {
                query = query.Where(c => c.PrimaryPhone != null && c.PrimaryPhone.Contains(phoneNeedle));
            }
        }
        else if (nationalOk)
        {
            var ids = identityMatchIds.ToList();
            query = query.Where(c => ids.Contains(c.Id));
        }
        else
        {
            var phoneNeedle = phoneCanon ?? phoneDigits;
            query = query.Where(c => c.PrimaryPhone != null && c.PrimaryPhone.Contains(phoneNeedle));
        }

        var entities = await query.OrderByDescending(c => c.CreatedAtUtc).Take(MaxResults).ToListAsync(cancellationToken);

        var data = entities.Select(c => new FindCustomerCandidateDto
        {
            Id = c.Id,
            Name = c.DisplayName,
            PhoneNumber = c.PrimaryPhone,
            City = c.Address.City,
            EmailAddress = c.ContactEmail,
            TelecomSubscriptionLineCount = c.SubscriberProfiles
                .Where(sp => !sp.IsDeleted)
                .SelectMany(sp => sp.Subscriptions)
                .Count(s => !s.IsDeleted && s.MsisdnAsset != null)
        }).ToList();

        await _subscriberAudit.LogSearchAsync(
            "CustomerRegistry",
            nationalOk ? national : null,
            phoneOk ? (phoneCanon ?? phoneDigits) : null,
            null,
            data.Select(c => new SubscriberSearchMatchAuditDto
            {
                CustomerId = c.Id,
                Name = c.Name,
                PhoneOrMsisdn = c.PhoneNumber,
            }).ToList(),
            cancellationToken);

        return new FindCustomerCandidatesResult { Data = data };
    }
}
