using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetBillingIntegrationLogListDto
{
    public string? Id { get; init; }
    public string? TelecomOperationRequestId { get; init; }
    public string? OperationNumber { get; init; }
    public int AttemptNumber { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? IntegrationTarget { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBillingIntegrationLogListProfile : Profile
{
    public GetBillingIntegrationLogListProfile()
    {
        CreateMap<BillingIntegrationLog, GetBillingIntegrationLogListDto>()
            .ForMember(d => d.OperationNumber, o => o.MapFrom(s => s.TelecomOperationRequest != null ? s.TelecomOperationRequest.Number : string.Empty));
    }
}

public class GetBillingIntegrationLogListResult
{
    public List<GetBillingIntegrationLogListDto>? Data { get; init; }
}

public class GetBillingIntegrationLogListRequest : IRequest<GetBillingIntegrationLogListResult>
{
    public string? TelecomOperationRequestId { get; init; }
    public bool IsDeleted { get; init; }
}

public class GetBillingIntegrationLogListHandler : IRequestHandler<GetBillingIntegrationLogListRequest, GetBillingIntegrationLogListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBillingIntegrationLogListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBillingIntegrationLogListResult> Handle(GetBillingIntegrationLogListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.BillingIntegrationLog
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.TelecomOperationRequest)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.TelecomOperationRequestId))
            query = query.Where(x => x.TelecomOperationRequestId == request.TelecomOperationRequestId);

        var list = await query.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);
        return new GetBillingIntegrationLogListResult { Data = _mapper.Map<List<GetBillingIntegrationLogListDto>>(list) };
    }
}
