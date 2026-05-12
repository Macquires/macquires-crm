// Demo-only — vendor contacts for Syria Telecom supply chain demo.
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class VendorContactSeeder
{
    private static readonly string[] ArabicFirstNames =
    [
        "رامي", "سمر", "علي", "لين", "حسام", "رنا",
        "مروان", "ديانا", "وليد", "نور", "تيم", "يارا"
    ];

    private static readonly string[] ArabicLastNames =
    [
        "الشامي", "البحري", "القاسمي", "الزهيري", "العمري", "السعدي",
        "الخطيب", "المصري", "الأنصاري", "الغني", "الحموي", "النابلسي"
    ];

    private static readonly string[] JobTitles =
    [
        "مدير حساب", "مهندس حلول", "منسق لوجستي", "مدير مبيعات B2B", "أخصائي عقود",
        "مدير مشتريات", "مهندس شبكات", "مسؤول تسليم", "مدير مالي", "منسق دعم فني"
    ];

    private readonly ICommandRepository<VendorContact> _vendorContactRepository;
    private readonly ICommandRepository<Vendor> _vendorRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public VendorContactSeeder(
        ICommandRepository<VendorContact> vendorContactRepository,
        ICommandRepository<Vendor> vendorRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _vendorContactRepository = vendorContactRepository;
        _vendorRepository = vendorRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var vendorIds = await _vendorRepository.GetQuery().Select(x => x.Id).ToListAsync();

        var vendorContacts = new List<VendorContact>();

        foreach (var vendorId in vendorIds)
        {
            for (int i = 0; i < 3; i++)
            {
                var firstName = GetRandomString(ArabicFirstNames, random);
                var lastName = GetRandomString(ArabicLastNames, random);
                var prefix = random.Next(2) == 0 ? "093" : "099";

                vendorContacts.Add(new VendorContact
                {
                    Name = $"{firstName} {lastName}",
                    Number = _numberSequenceService.GenerateNumber(nameof(VendorContact), "", "VC"),
                    VendorId = vendorId,
                    JobTitle = GetRandomString(JobTitles, random),
                    EmailAddress = $"vendor.contact{random.Next(1000, 9999)}@syriatelecom-demo.local",
                    PhoneNumber = $"{prefix}{random.Next(1000000, 9999999)}"
                });
            }
        }

        foreach (var contact in vendorContacts)
        {
            await _vendorContactRepository.CreateAsync(contact);
        }

        await _unitOfWork.SaveAsync();
    }

    private static string GetRandomString(string[] array, Random random)
    {
        return array[random.Next(array.Length)];
    }
}
