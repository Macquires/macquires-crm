// Demo-only — fictional Syria Telecom supply-chain partners (not real companies).
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class VendorSeeder
{
    private readonly ICommandRepository<Vendor> _vendorRepository;
    private readonly ICommandRepository<VendorGroup> _groupRepository;
    private readonly ICommandRepository<VendorCategory> _categoryRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public VendorSeeder(
        ICommandRepository<Vendor> vendorRepository,
        ICommandRepository<VendorGroup> groupRepository,
        ICommandRepository<VendorCategory> categoryRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _vendorRepository = vendorRepository;
        _groupRepository = groupRepository;
        _categoryRepository = categoryRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var groups = (await _groupRepository.GetQuery().ToListAsync()).Select(x => x.Id).ToArray();
        var categories = (await _categoryRepository.GetQuery().ToListAsync()).Select(x => x.Id).ToArray();
        var cityRows = new (string City, string Street, string State)[]
        {
            ("دمشق", "شارع بغداد — مجمع التجار", "دمشق"),
            ("دمشق", "المزة — طريق الجامعة", "دمشق"),
            ("حلب", "الجميلية — سوق الإلكترونيات", "حلب"),
            ("حمص", "الوعر — شارع الصناعة", "حمص"),
            ("اللاذقية", "الغنيمة — المنطقة الحرة", "اللاذقية"),
            ("حماة", "طريق حلب", "حماة"),
            ("طرطوس", "الكورنيش البحري", "طرطوس"),
            ("درعا", "الساحة — مجمع الأعمال", "درعا")
        };

        var random = new Random();

        var vendors = new List<Vendor>
        {
            new Vendor { Name = "هواوي — مكتب تمثيل سوريا (ديمو)" },
            new Vendor { Name = "ZTE الشرق الأوسط — فرع دمشق" },
            new Vendor { Name = "إريكسون — شريك تقني (ديمو)" },
            new Vendor { Name = "شركة الشام للوجستيات والتوزيع" },
            new Vendor { Name = "مؤسسة البركة لتوريد الأبراج والهياكل" },
            new Vendor { Name = "شركة الفجر لأنظمة الطاقة الاحتياطية" },
            new Vendor { Name = "مجموعة النور لمعدات الـ FTTH" },
            new Vendor { Name = "شركة بردى لخدمات المواقع الميدانية" },
            new Vendor { Name = "مؤسسة قاسيون لتركيبات الشبكة الداخلية" },
            new Vendor { Name = "شركة يافا لحلول الدفع الإلكتروني" },
            new Vendor { Name = "مكتب حماة للاستيراد والتخليص" },
            new Vendor { Name = "شركة البادية لنقل الشحنات السريعة" },
            new Vendor { Name = "مؤسسة غوطة شرقية للمقاولات الخفيفة" },
            new Vendor { Name = "شركة كاسل لكابلات النحاس والألياف" },
            new Vendor { Name = "مجموعة الفرات لتجهيزات غرف السيرفر" },
            new Vendor { Name = "شركة الساحل لصيانة أبراج الجيل الرابع" },
            new Vendor { Name = "مؤسسة تدمر للمعدات الصناعية" },
            new Vendor { Name = "شركة الخابور لخدمات الـ B2B" }
        };

        foreach (var vendor in vendors)
        {
            vendor.Number = _numberSequenceService.GenerateNumber(nameof(Vendor), "", "VND");
            vendor.VendorGroupId = GetRandomValue(groups, random);
            vendor.VendorCategoryId = GetRandomValue(categories, random);

            var row = cityRows[random.Next(cityRows.Length)];
            vendor.City = row.City;
            vendor.Street = row.Street;
            vendor.State = row.State;
            vendor.ZipCode = $"{1000 + random.Next(8000)}";

            var prefix = random.Next(2) == 0 ? "093" : "099";
            vendor.PhoneNumber = $"{prefix}{random.Next(1000000, 9999999)}";
            vendor.EmailAddress = $"vendor{random.Next(100, 999)}@syriatelecom-demo.local";

            await _vendorRepository.CreateAsync(vendor);
        }

        await _unitOfWork.SaveAsync();
    }

    private static T GetRandomValue<T>(T[] array, Random random)
    {
        return array[random.Next(array.Length)];
    }
}
