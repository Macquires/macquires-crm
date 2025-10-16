using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingManager.Commands;

public class UpdateBookingResult
{
    public Booking? Data { get; set; }
}

public class UpdateBookingRequest : IRequest<UpdateBookingResult>
{
    public string? Id { get; init; }
    public string? Subject { get; init; }
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
    public string? Status { get; init; }
    public string? BookingResourceId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateBookingValidator : AbstractValidator<UpdateBookingRequest>
{
    public UpdateBookingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.BookingResourceId).NotEmpty();
    }
}

public class UpdateBookingHandler : IRequestHandler<UpdateBookingRequest, UpdateBookingResult>
{
    private readonly ICommandRepository<Booking> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookingHandler(
        ICommandRepository<Booking> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public DateTime ConvertToLocalTime(DateTime utcTime, string? timezone)
    {
        if (string.IsNullOrEmpty(timezone))
        {
            return utcTime;
        }

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
    }

    public async Task<UpdateBookingResult> Handle(UpdateBookingRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Subject = request.Subject;
        entity.StartTime = ConvertToLocalTime(request.StartTime ?? DateTime.UtcNow, request.StartTimezone);
        entity.EndTime = ConvertToLocalTime(request.EndTime ?? DateTime.UtcNow, request.EndTimezone);
        entity.StartTimezone = request.StartTimezone;
        entity.EndTimezone = request.EndTimezone;
        entity.Location = request.Location;
        entity.Description = request.Description;
        entity.IsAllDay = request.IsAllDay;
        entity.IsReadOnly = request.IsReadOnly;
        entity.IsBlock = request.IsBlock;
        entity.RecurrenceRule = request.RecurrenceRule;
        entity.RecurrenceID = request.RecurrenceID;
        entity.FollowingID = request.FollowingID;
        entity.RecurrenceException = request.RecurrenceException;
        entity.Status = (BookingStatus)int.Parse(request.Status!);
        entity.BookingResourceId = request.BookingResourceId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateBookingResult
        {
            Data = entity
        };
    }
}

