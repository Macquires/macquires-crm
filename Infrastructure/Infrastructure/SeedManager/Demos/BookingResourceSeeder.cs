using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class BookingResourceSeeder
{
    private readonly ICommandRepository<BookingResource> _resourceRepository;
    private readonly ICommandRepository<BookingGroup> _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingResourceSeeder(
        ICommandRepository<BookingResource> resourceRepository,
        ICommandRepository<BookingGroup> groupRepository,
        IUnitOfWork unitOfWork
    )
    {
        _resourceRepository = resourceRepository;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var vehicleGroup = await _groupRepository.GetQuery().Where(x => x.Name == "Vehicle").SingleOrDefaultAsync();
        if (vehicleGroup != null)
        {
            var vehicleResources = new List<BookingResource>
            {
                new BookingResource { Name = "Audi 01", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "Audi 02", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "Audi 03", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "BMW 01", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "BMW 02", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "BMW 03", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "Lexus 01", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "Lexus 02", BookingGroupId = vehicleGroup.Id },
                new BookingResource { Name = "Lexus 03", BookingGroupId = vehicleGroup.Id }
            };

            foreach (var resource in vehicleResources)
            {
                await _resourceRepository.CreateAsync(resource);
            }
        }

        var roomGroup = await _groupRepository.GetQuery().Where(x => x.Name == "Room").SingleOrDefaultAsync();
        if (roomGroup != null)
        {
            var roomResources = new List<BookingResource>
            {
                new BookingResource { Name = "Room One", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Room Two", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Room Three", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Conference One", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Conference Two", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Conference Three", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Studio One", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Studio Two", BookingGroupId = roomGroup.Id },
                new BookingResource { Name = "Studio Three", BookingGroupId = roomGroup.Id }
            };

            foreach (var resource in roomResources)
            {
                await _resourceRepository.CreateAsync(resource);
            }
        }

        var electronicGroup = await _groupRepository.GetQuery().Where(x => x.Name == "Electronic").SingleOrDefaultAsync();
        if (electronicGroup != null)
        {
            var electronicResources = new List<BookingResource>
            {
                new BookingResource { Name = "Epson Projector", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Sony Projector", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Bose Speaker", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "JBL Speaker", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Microsoft Webcam", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Logitech Webcam", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Google Chromecast", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Apple TV", BookingGroupId = electronicGroup.Id },
                new BookingResource { Name = "Samsung Monitor 49", BookingGroupId = electronicGroup.Id }
            };

            foreach (var resource in electronicResources)
            {
                await _resourceRepository.CreateAsync(resource);
            }
        }

        await _unitOfWork.SaveAsync();
    }
}


