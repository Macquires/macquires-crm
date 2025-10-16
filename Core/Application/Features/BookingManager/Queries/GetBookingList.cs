using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookingManager.Queries;

public record GetBookingListDto
{
    public string? Id { get; init; }
    public string? Subject { get; init; }
    public string? Number { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public string? StartTimezone { get; init; }
    public string? EndTimezone { get; init; }
    public string? Location { get; init; }
    public string? Description { get; init; }
    public bool? IsAllDay { get; init; }
    public bool? IsReadOnly { get; init; }
    public bool? IsBlock { get; init; }
    public string? RecurrenceRule { get; init; }
    public string? RecurrenceID { get; init; }
    public string? FollowingID { get; init; }
    public string? RecurrenceException { get; init; }
    public string? StatusName { get; init; }
    public BookingStatus? Status { get; init; }
    public string? BookingResourceName { get; init; }
    public string? BookingResourceId { get; init; }
    public string? BookingGroupName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBookingListProfile : Profile
{
    public GetBookingListProfile()
    {
        CreateMap<Booking, GetBookingListDto>()
            .ForMember(
                dest => dest.BookingResourceName,
                opt => opt.MapFrom(src => src.BookingResource != null ? src.BookingResource.Name : string.Empty)
            )
            .ForMember(
                dest => dest.BookingGroupName,
                opt => opt.MapFrom(src => src.BookingResource!.BookingGroup != null ? src.BookingResource!.BookingGroup.Name : string.Empty)
            )
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetBookingListResult
{
    public List<GetBookingListDto>? Data { get; init; }
}

public class GetBookingListRequest : IRequest<GetBookingListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetBookingListHandler : IRequestHandler<GetBookingListRequest, GetBookingListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBookingListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBookingListResult> Handle(GetBookingListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Booking
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.BookingResource)
                .ThenInclude(x => x!.BookingGroup)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBookingListDto>>(entities);

        return new GetBookingListResult
        {
            Data = dtos
        };
    }


}



