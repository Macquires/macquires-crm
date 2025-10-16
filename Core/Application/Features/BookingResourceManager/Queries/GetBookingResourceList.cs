using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookingResourceManager.Queries;

public record GetBookingResourceListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? BookingGroupId { get; init; }
    public string? BookingGroupName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBookingResourceListProfile : Profile
{
    public GetBookingResourceListProfile()
    {
        CreateMap<BookingResource, GetBookingResourceListDto>()
            .ForMember(
                dest => dest.BookingGroupName,
                opt => opt.MapFrom(src => src.BookingGroup != null ? src.BookingGroup.Name : string.Empty)
            );

    }
}

public class GetBookingResourceListResult
{
    public List<GetBookingResourceListDto>? Data { get; init; }
}

public class GetBookingResourceListRequest : IRequest<GetBookingResourceListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetBookingResourceListHandler : IRequestHandler<GetBookingResourceListRequest, GetBookingResourceListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBookingResourceListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBookingResourceListResult> Handle(GetBookingResourceListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .BookingResource
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.BookingGroup)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBookingResourceListDto>>(entities);

        return new GetBookingResourceListResult
        {
            Data = dtos
        };
    }


}



