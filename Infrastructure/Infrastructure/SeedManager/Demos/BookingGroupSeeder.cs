// Demo-only — booking groups for field fleet, showrooms, demo kits (Syria Telecom narrative).
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
            new BookingGroup { Name = "اسطول ميداني" },
            new BookingGroup { Name = "قاعات معارض" },
            new BookingGroup { Name = "معدات عرض وتدريب" }
        };

        foreach (var bookingGroup in bookingGroups)
        {
            await _repository.CreateAsync(bookingGroup);
        }

        await _unitOfWork.SaveAsync();
    }
}
