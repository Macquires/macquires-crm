using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
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

    public FindCustomerCandidatesHandler(
        IQueryContext context,
        IFieldEncryptionService encryption,
        ISubscriberAccessAuditService subscriberAudit)
    {
        _context = context;
        _encryption = encryption;
        _subscriberAudit = subscriberAudit;
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
            query = query.Where(c =>
                (nationalHash != null
                 && EF.Property<CustomerKind>(c, nameof(Customer.CustomerKind)) == CustomerKind.Individual
                 && EF.Property<string>(c, nameof(IndividualCustomer.NationalIdSearchHash)) == nationalHash)
                || (EF.Property<CustomerKind>(c, nameof(Customer.CustomerKind)) == CustomerKind.Corporate
                    && EF.Property<string>(c, nameof(CorporateCustomer.CommercialRegistryNumber)) != null
                    && EF.Property<string>(c, nameof(CorporateCustomer.CommercialRegistryNumber)).Contains(national))
                || (c.PrimaryPhone != null && c.PrimaryPhone.Contains(phoneNeedle)));
        }
        else if (nationalOk)
        {
            query = query.Where(c =>
                (nationalHash != null
                 && EF.Property<CustomerKind>(c, nameof(Customer.CustomerKind)) == CustomerKind.Individual
                 && EF.Property<string>(c, nameof(IndividualCustomer.NationalIdSearchHash)) == nationalHash)
                || (EF.Property<CustomerKind>(c, nameof(Customer.CustomerKind)) == CustomerKind.Corporate
                    && EF.Property<string>(c, nameof(CorporateCustomer.CommercialRegistryNumber)) != null
                    && EF.Property<string>(c, nameof(CorporateCustomer.CommercialRegistryNumber)).Contains(national)));
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
