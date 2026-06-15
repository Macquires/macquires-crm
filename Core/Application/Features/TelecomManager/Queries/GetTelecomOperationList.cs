using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetTelecomOperationListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public TelecomOperationKind? Kind { get; init; }
    public string? KindName { get; init; }
    public TelecomOperationStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public TelecomDocumentStatus? DocumentStatus { get; init; }
    public string? DocumentStatusName { get; init; }
    public string? SubscriberName { get; init; }
    public string? SecondarySubscriberName { get; init; }
    public string? Msisdn { get; init; }
    public bool HasIdentityDocument { get; init; }
    public string? TargetOfferName { get; init; }
    public string? TransferReason { get; init; }
    public string? ReplacementReason { get; init; }
    public bool IsLostOrStolenReport { get; init; }
    public string? ApprovalLevelRequired { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? ScheduledEffectiveDateUtc { get; init; }
}

public class GetTelecomOperationListProfile : Profile
{
    public GetTelecomOperationListProfile()
    {
        CreateMap<TelecomOperationRequest, GetTelecomOperationListDto>()
            .ForMember(d => d.SubscriberName, o => o.MapFrom(s => s.SubscriberProfile != null && s.SubscriberProfile.Customer != null
                ? s.SubscriberProfile.Customer.DisplayName
                : string.Empty))
            .ForMember(d => d.SecondarySubscriberName, o => o.MapFrom(s =>
                s.SecondarySubscriberProfile != null && s.SecondarySubscriberProfile.Customer != null
                    ? s.SecondarySubscriberProfile.Customer.DisplayName
                    : string.Empty))
            .ForMember(d => d.Msisdn, o => o.MapFrom(s => s.MsisdnAsset != null ? s.MsisdnAsset.Msisdn : string.Empty))
            .ForMember(d => d.HasIdentityDocument, o => o.MapFrom(s => !string.IsNullOrEmpty(s.IdentityDocumentStorageKey)))
            .ForMember(d => d.KindName, o => o.MapFrom(s => s.Kind.ToString()))
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.DocumentStatusName, o => o.MapFrom(s => s.DocumentStatus.ToString()));
    }
}

public class GetTelecomOperationListResult
{
    public List<GetTelecomOperationListDto>? Data { get; init; }
}

public class GetTelecomOperationListRequest : IRequest<GetTelecomOperationListResult>, IRequireAnyPermission
{
    public bool IsDeleted { get; init; }
    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.OperationsViewAny;
}

public class GetTelecomOperationListHandler : IRequestHandler<GetTelecomOperationListRequest, GetTelecomOperationListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetTelecomOperationListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetTelecomOperationListResult> Handle(GetTelecomOperationListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.TelecomOperationRequest
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .Include(x => x.SecondarySubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .Include(x => x.MsisdnAsset)
            .OrderByDescending(x => x.CreatedAtUtc);

        var list = await query.ToListAsync(cancellationToken);
        var mapped = _mapper.Map<List<GetTelecomOperationListDto>>(list);
        var data = mapped.Select(d =>
        {
            var src = list.First(x => x.Id == d.Id);
            return d with
            {
                KindName = TelecomOperationLabels.KindLabelAr(src.Kind),
                StatusName = TelecomOperationLabels.StatusLabelAr(src.Status),
                DocumentStatusName = src.DocumentStatus.ToString(),
                TransferReason = src.TransferReason,
                ReplacementReason = src.ReplacementReason,
                IsLostOrStolenReport = src.IsLostOrStolenReport,
                ApprovalLevelRequired = src.ApprovalLevelRequired,
                ScheduledEffectiveDateUtc = TelecomOperationSchedulePolicy.ResolveEffectiveDateUtc(src),
            };
        }).ToList();

        return new GetTelecomOperationListResult { Data = data };
    }
}
