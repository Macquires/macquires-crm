using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingManager.Commands;

public class CreateBookingResult
{
    public Booking? Data { get; init; }
}

public class CreateBookingRequest : IRequest<CreateBookingResult>
{
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
    public string? CreatedById { get; init; }
}

public class CreateBookingValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.BookingResourceId).NotEmpty();
    }
}

public class CreateBookingHandler : IRequestHandler<CreateBookingRequest, CreateBookingResult>
{
    private readonly ICommandRepository<Booking> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateBookingHandler(
        ICommandRepository<Booking> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
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

    public async Task<CreateBookingResult> Handle(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Booking();
        entity.CreatedById = request.CreatedById;

        entity.Subject = request.Subject;
        entity.Number = _numberSequenceService.GenerateNumber(nameof(Booking), "", "BOK");
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

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateBookingResult
        {
            Data = entity
        };
    }
}