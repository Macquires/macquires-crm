using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadContactManager.Queries;

public record GetLeadContactByLeadIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? FullName { get; init; }
    public string? Description { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressState { get; init; }
    public string? AddressZipCode { get; init; }
    public string? AddressCountry { get; init; }
    public string? PhoneNumber { get; init; }
    public string? FaxNumber { get; init; }
    public string? MobileNumber { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? WhatsApp { get; init; }
    public string? LinkedIn { get; init; }
    public string? Facebook { get; init; }
    public string? Twitter { get; init; }
    public string? Instagram { get; init; }
    public string? AvatarName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetLeadContactByLeadIdListProfile : Profile
{
    public GetLeadContactByLeadIdListProfile()
    {
        CreateMap<LeadContact, GetLeadContactByLeadIdListDto>();
    }
}

public class GetLeadContactByLeadIdListResult
{
    public List<GetLeadContactByLeadIdListDto>? Data { get; init; }
}

public class GetLeadContactByLeadIdListRequest : IRequest<GetLeadContactByLeadIdListResult>
{
    public string? LeadId { get; init; }
}

public class GetLeadContactByLeadIdListHandler : IRequestHandler<GetLeadContactByLeadIdListRequest, GetLeadContactByLeadIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetLeadContactByLeadIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetLeadContactByLeadIdListResult> Handle(GetLeadContactByLeadIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .LeadContact
            .AsNoTracking()
            .IsDeletedEqualTo(false) // Assuming contacts aren't soft-deleted or this method filters out deleted contacts
            .Where(x => x.LeadId == request.LeadId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetLeadContactByLeadIdListDto>>(entities);

        return new GetLeadContactByLeadIdListResult
        {
            Data = dtos
        };
    }
}