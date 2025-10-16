using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class BookingSeeder
{
    private readonly ICommandRepository<Booking> _bookingRepository;
    private readonly ICommandRepository<BookingResource> _bookingResourceRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public BookingSeeder(
        ICommandRepository<Booking> bookingRepository,
        ICommandRepository<BookingResource> bookingResourceRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _bookingRepository = bookingRepository;
        _bookingResourceRepository = bookingResourceRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        int bookingStatusLength = Enum.GetNames(typeof(BookingStatus)).Length;

        var dateEnd = DateTime.Now.AddMonths(6);
        var dateStart = dateEnd.AddMonths(-6);

        var bookingResources = await _bookingResourceRepository
            .GetQuery()
            .Select(x => x.Id)
            .ToListAsync();

        var dummyLocations = new List<string>
        {
            "123 Main St, New York",
            "456 Elm St, Los Angeles",
            "789 Pine St, Chicago",
            "101 Maple St, Houston",
            "202 Oak St, Phoenix",
            "303 Birch St, Philadelphia",
            "404 Cedar St, San Antonio",
            "505 Walnut St, San Diego",
            "606 Aspen St, Dallas",
            "707 Spruce St, San Jose"
        };


        for (DateTime date = dateStart; date < dateEnd; date = date.AddMonths(1))
        {
            DateTime[] transactionDates = GenerateRandomDays(date.Year, date.Month, 12);
            foreach (DateTime transDate in transactionDates)
            {
                TimeSpan randomTime = TimeSpan.FromHours(random.Next(8, 12));
                DateTime startTime = transDate.Date.Add(randomTime);

                var number = _numberSequenceService.GenerateNumber(nameof(Booking), "", "BOK");

                var booking = new Booking
                {
                    Subject = number,
                    Number = number,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(random.Next(2, 12)),
                    BookingResourceId = GetRandomValue(bookingResources, random),
                    Status = (BookingStatus)random.Next(0, bookingStatusLength),
                    Location = GetRandomValue(dummyLocations, random)
                };

                await _bookingRepository.CreateAsync(booking);
            }

            await _unitOfWork.SaveAsync();
        }
    }

    private static DateTime[] GenerateRandomDays(int year, int month, int count)
    {
        var random = new Random();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return Enumerable.Range(1, count)
            .Select(_ => new DateTime(year, month, random.Next(1, daysInMonth + 1)))
            .ToArray();
    }

    private static T GetRandomValue<T>(List<T> list, Random random)
    {
        return list[random.Next(list.Count)];
    }
}
