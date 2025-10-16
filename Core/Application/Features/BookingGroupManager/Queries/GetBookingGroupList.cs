using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookingGroupManager.Queries;

public record GetBookingGroupListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBookingGroupListProfile : Profile
{
    public GetBookingGroupListProfile()
    {
        CreateMap<BookingGroup, GetBookingGroupListDto>();
    }
}

public class GetBookingGroupListResult
{
    public List<GetBookingGroupListDto>? Data { get; init; }
}

public class GetBookingGroupListRequest : IRequest<GetBookingGroupListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetBookingGroupListHandler : IRequestHandler<GetBookingGroupListRequest, GetBookingGroupListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBookingGroupListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBookingGroupListResult> Handle(GetBookingGroupListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .BookingGroup
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBookingGroupListDto>>(entities);

        return new GetBookingGroupListResult
        {
            Data = dtos
        };
    }


}



