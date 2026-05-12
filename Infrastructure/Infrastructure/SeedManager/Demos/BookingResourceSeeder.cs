// Demo-only — resources must match BookingGroup names from BookingGroupSeeder.
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
        var fleetGroup = await _groupRepository.GetQuery().Where(x => x.Name == "اسطول ميداني").SingleOrDefaultAsync();
        if (fleetGroup != null)
        {
            var fleetResources = new List<BookingResource>
            {
                new BookingResource { Name = "فان فني — دمشق 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "فان فني — دمشق 02", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "فان فني — حلب 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "فان فني — حمص 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "فان فني — الساحل 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "بيك أب فني — جنوب 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "بيك أب فني — شرق 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "فان تركيب FTTH — 01", BookingGroupId = fleetGroup.Id },
                new BookingResource { Name = "فان تركيب FTTH — 02", BookingGroupId = fleetGroup.Id }
            };

            foreach (var resource in fleetResources)
            {
                await _resourceRepository.CreateAsync(resource);
            }
        }

        var showroomGroup = await _groupRepository.GetQuery().Where(x => x.Name == "قاعات معارض").SingleOrDefaultAsync();
        if (showroomGroup != null)
        {
            var showroomResources = new List<BookingResource>
            {
                new BookingResource { Name = "قاعة عرض — دمشق الحجاز", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "قاعة عرض — حلب العزيزية", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "قاعة عرض — حمص المحطة", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "قاعة تدريب — اللاذقية", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "جناح B2B — دمشق", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "جناح B2B — حلب", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "استوديو بث — ديمو أونلاين", BookingGroupId = showroomGroup.Id },
                new BookingResource { Name = "غرفة اجتماعات — الطابق الثاني", BookingGroupId = showroomGroup.Id }
            };

            foreach (var resource in showroomResources)
            {
                await _resourceRepository.CreateAsync(resource);
            }
        }

        var kitGroup = await _groupRepository.GetQuery().Where(x => x.Name == "معدات عرض وتدريب").SingleOrDefaultAsync();
        if (kitGroup != null)
        {
            var kitResources = new List<BookingResource>
            {
                new BookingResource { Name = "طقم عرض 5G — متحرك", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "طقم عرض FTTH — ثابت", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "شاشة تفاعلية 75\"", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "راوترات تجريبية — كرتون ديمو", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "مجموعة Wingle للتجربة", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "جهاز قياس إشارة — Spectrum", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "كاميرا تسجيل جودة الخدمة", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "لابتوب مبيعات ميداني — 01", BookingGroupId = kitGroup.Id },
                new BookingResource { Name = "لابتوب مبيعات ميداني — 02", BookingGroupId = kitGroup.Id }
            };

            foreach (var resource in kitResources)
            {
                await _resourceRepository.CreateAsync(resource);
            }
        }

        await _unitOfWork.SaveAsync();
    }
}
