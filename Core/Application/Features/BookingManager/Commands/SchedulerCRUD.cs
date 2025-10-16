using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookingManager.Commands;

public class SchedulerCRUDResult
{
    public List<Booking>? Data { get; init; }
    public int Count { get; init; }
}
public class FilterParams
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class SchedulerCRUDRequest : IRequest<SchedulerCRUDResult>
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public FilterParams? Params { get; set; }
    public string? Action { get; init; }
    public string? Key { get; init; }
    public BookingModel? Value { get; init; }
    public BookingModel[]? Added { get; init; }
    public BookingModel[]? Changed { get; init; }
    public BookingModel[]? Deleted { get; init; }
}

public class BookingModel
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
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
    public BookingStatus? Status { get; set; }
}

public class SchedulerCRUDValidator : AbstractValidator<SchedulerCRUDRequest>
{
    public SchedulerCRUDValidator()
    {
    }
}

public class SchedulerCRUDHandler : IRequestHandler<SchedulerCRUDRequest, SchedulerCRUDResult>
{
    private readonly ICommandRepository<Booking> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public SchedulerCRUDHandler(
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


    public async Task<SchedulerCRUDResult> Handle(SchedulerCRUDRequest request, CancellationToken cancellationToken = default)
    {
        if (request?.Action == "insert" || (request?.Action == "batch" && request?.Added?.Count() > 0))
        {
            var value = (request.Action == "insert") ? request.Value : request?.Added?[0];
            var entity = MapToEntity(value);
            entity.Number = _numberSequenceService.GenerateNumber(nameof(Booking), "", "BOK");
            entity.Description = entity.Subject;
            entity.Subject = entity.Number;
            entity.Status ??= BookingStatus.Draft;

            await _repository.CreateAsync(entity, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        if (request?.Action == "update" || (request?.Action == "batch" && request?.Changed?.Count() > 0))
        {
            var value = (request.Action == "update") ? request.Value : request?.Changed?[0];
            var entity = await _repository.GetAsync(value?.Id ?? "", cancellationToken);
            if (entity != null)
            {
                UpdateEntity(entity, value);
                _repository.Update(entity);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }

        if (request?.Action == "remove" || (request?.Action == "batch" && request?.Deleted?.Count() > 0))
        {
            var key = request?.Deleted?[0].Id;
            var entity = await _repository.GetAsync(key ?? "", cancellationToken);
            if (entity != null)
            {
                _repository.Delete(entity);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }

        var allData = await _repository
            .GetQuery()
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .ToListAsync(cancellationToken);

        return new SchedulerCRUDResult
        {
            Data = allData.ToList(),
            Count = allData.Count()
        };
    }

    private Booking MapToEntity(BookingModel? model)
    {
        return new Booking
        {
            Subject = model?.Subject,
            StartTime = ConvertToLocalTime(model?.StartTime ?? DateTime.UtcNow, model?.StartTimezone),
            EndTime = ConvertToLocalTime(model?.EndTime ?? DateTime.UtcNow, model?.EndTimezone),
            StartTimezone = model?.StartTimezone,
            EndTimezone = model?.EndTimezone,
            Location = model?.Location,
            Description = model?.Description,
            IsAllDay = model?.IsAllDay,
            IsReadOnly = model?.IsReadOnly,
            IsBlock = model?.IsBlock,
            RecurrenceRule = model?.RecurrenceRule,
            RecurrenceID = model?.RecurrenceID,
            FollowingID = model?.FollowingID,
            RecurrenceException = model?.RecurrenceException,
            BookingResourceId = model?.BookingResourceId,
            Status = model?.Status
        };
    }

    private void UpdateEntity(Booking entity, BookingModel? model)
    {
        entity.Subject = model?.Subject;
        entity.StartTime = ConvertToLocalTime(model?.StartTime ?? DateTime.UtcNow, model?.StartTimezone);
        entity.EndTime = ConvertToLocalTime(model?.EndTime ?? DateTime.UtcNow, model?.EndTimezone);
        entity.StartTimezone = model?.StartTimezone;
        entity.EndTimezone = model?.EndTimezone;
        entity.Location = model?.Location;
        entity.Description = model?.Description;
        entity.IsAllDay = model?.IsAllDay;
        entity.IsReadOnly = model?.IsReadOnly;
        entity.IsBlock = model?.IsBlock;
        entity.RecurrenceRule = model?.RecurrenceRule;
        entity.RecurrenceID = model?.RecurrenceID;
        entity.FollowingID = model?.FollowingID;
        entity.RecurrenceException = model?.RecurrenceException;
        entity.BookingResourceId = model?.BookingResourceId;
        entity.Status = model?.Status;
    }
}
