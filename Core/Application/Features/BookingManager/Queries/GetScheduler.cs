using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookingManager.Queries;

public record GetSchedulerDto
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public string? Number { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? StartTimezone { get; set; }
    public string? EndTimezone { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsBlock { get; set; }
    public string? RecurrenceRule { get; set; }
    public string? RecurrenceID { get; set; }
    public string? FollowingID { get; set; }
    public string? RecurrenceException { get; set; }
    public string? BookingResourceId { get; set; }
    public string? BookingGroupId { get; set; }
    public BookingStatus? Status { get; set; }
}

public class GetSchedulerProfile : Profile
{
    public GetSchedulerProfile()
    {
        CreateMap<Booking, GetSchedulerDto>();
    }
}

public class GetSchedulerResult
{
    public List<GetSchedulerDto>? Data { get; init; }
}

public class GetSchedulerRequest : IRequest<GetSchedulerResult>
{
}


public class GetSchedulerHandler : IRequestHandler<GetSchedulerRequest, GetSchedulerResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSchedulerHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSchedulerResult> Handle(GetSchedulerRequest request, CancellationToken cancellationToken)
    {
        var entities = await _context.Booking
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.BookingResource)
                .ThenInclude(x => x!.BookingGroup)
            .Select(x => new GetSchedulerDto
            {
                Id = x.Id,
                Subject = x.Subject,
                Number = x.Number,
                StartTime = x.StartTime ?? DateTime.Now,
                EndTime = x.EndTime ?? DateTime.Now,
                StartTimezone = x.StartTimezone,
                EndTimezone = x.EndTimezone,
                Location = x.Location,
                Description = x.Description,
                IsReadOnly = x.IsReadOnly ?? false,
                IsAllDay = x.IsAllDay ?? false,
                IsBlock = x.IsBlock ?? false,
                RecurrenceRule = x.RecurrenceRule,
                RecurrenceID = x.RecurrenceID,
                FollowingID = x.FollowingID,
                RecurrenceException = x.RecurrenceException,
                BookingResourceId = x.BookingResourceId,
                BookingGroupId = x.BookingResource!.BookingGroupId,
                Status = x.Status
            })
            .ToListAsync();


        return new GetSchedulerResult
        {
            Data = entities
        };
    }


}



