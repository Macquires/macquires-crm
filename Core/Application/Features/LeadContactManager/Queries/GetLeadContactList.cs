using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadContactManager.Queries;

public record GetLeadContactListDto
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
    public string? LeadId { get; init; }
    public string? LeadTitle { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetLeadContactListProfile : Profile
{
    public GetLeadContactListProfile()
    {
        CreateMap<LeadContact, GetLeadContactListDto>()
            .ForMember(
                dest => dest.LeadTitle,
                opt => opt.MapFrom(src => src.Lead != null ? src.Lead.Title : string.Empty)
            );
    }
}

public class GetLeadContactListResult
{
    public List<GetLeadContactListDto>? Data { get; init; }
}

public class GetLeadContactListRequest : IRequest<GetLeadContactListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetLeadContactListHandler : IRequestHandler<GetLeadContactListRequest, GetLeadContactListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetLeadContactListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetLeadContactListResult> Handle(GetLeadContactListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .LeadContact
            .Include(x => x.Lead)
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetLeadContactListDto>>(entities);

        return new GetLeadContactListResult
        {
            Data = dtos
        };
    }
}