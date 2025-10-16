using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class BookingGroupSeeder
{
    private readonly ICommandRepository<BookingGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingGroupSeeder(
        ICommandRepository<BookingGroup> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var bookingGroups = new List<BookingGroup>
        {
            new BookingGroup { Name = "Vehicle" },
            new BookingGroup { Name = "Room" },
            new BookingGroup { Name = "Electronic" }
        };

        foreach (var bookingGroup in bookingGroups)
        {
            await _repository.CreateAsync(bookingGroup);
        }

        await _unitOfWork.SaveAsync();
    }
}


