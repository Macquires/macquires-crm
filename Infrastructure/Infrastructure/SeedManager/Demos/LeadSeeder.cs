// Demo-only — B2B pipeline data for Syria Telecom demo (fictional companies).
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class LeadSeeder
{
    private static readonly string[] SyrianCompanyNames =
    [
        "مخابز الشام المتحدة", "شركة القاطع للاستيراد", "مجمع حمص الصناعي للبلاستيك", "فندق الأمويين دمشق",
        "جامعة اليرموك الخاصة", "مستشفى المحبة", "شركة الفرات للأسمدة", "مؤسسة البادية للنقل",
        "سوبرماركت النجمة الذهبية", "شركة الياسمين للاتصالات الصغيرة", "مخبز أهل الشام", "صيدلية الشفاء",
        "شركة البناء الحديث", "مؤسسة غيث للمقاولات", "معمل حلب للنسيج", "شركة الساحل للسياحة",
        "مخازن حماة المركزية", "شركة دمشق للأغذية", "مجموعة الكرامة التجارية", "شركة النور للتأمين",
        "مؤسسة الشهداء للتعليم", "شركة الوفاء للخدمات", "معمل اللاذقية للمشروبات", "شركة درعا للزراعة",
        "مؤسسة الفرات للطاقة", "شركة طرطوس للملاحة", "مجمع دير الزور التجاري", "شركة الحسكة للحبوب",
        "مؤسسة السويداء للرخام", "شركة القنيطرة للخدمات", "مجموعة إدلب الغذائية", "شركة الرقة للتجارة"
    ];

    private static readonly string[] Streets =
    [
        "شارع بغداد", "كورنيش المزة", "طريق المطار", "شارع الجامعة", "سوق الحميدية",
        "شارع العزيزية", "طريق حلب الدولي", "الوعر الصناعي", "المشروع السابع", "الكورنيش البحري"
    ];

    private static readonly string[] Cities =
    [
        "دمشق", "حلب", "حمص", "اللاذقية", "حماة", "طرطوس", "درعا", "دير الزور", "الحسكة", "السويداء"
    ];

    private readonly ICommandRepository<Lead> _leadRepository;
    private readonly ICommandRepository<Campaign> _campaignRepository;
    private readonly ICommandRepository<SalesTeam> _salesTeamRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public LeadSeeder(
        ICommandRepository<Lead> leadRepository,
        ICommandRepository<Campaign> campaignRepository,
        ICommandRepository<SalesTeam> salesTeamRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _leadRepository = leadRepository;
        _campaignRepository = campaignRepository;
        _salesTeamRepository = salesTeamRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-11).Year, dateFinish.AddMonths(-11).Month, 1);
        var confirmedCampaigns = await _campaignRepository.GetQuery()
            .Where(c => c.Status == CampaignStatus.Confirmed)
            .Select(c => c.Id)
            .ToListAsync();

        var salesTeamIds = await _salesTeamRepository.GetQuery()
            .Select(st => st.Id)
            .ToListAsync();

        var pipelineStageCounts = new Dictionary<PipelineStage, int>
        {
            { PipelineStage.Prospecting, 80 },
            { PipelineStage.Qualification, 70 },
            { PipelineStage.NeedAnalysis, 60 },
            { PipelineStage.Proposal, 50 },
            { PipelineStage.Negotiation, 40 },
            { PipelineStage.DecisionMaking, 30 },
            { PipelineStage.Closed, 15 }
        };

        var leadIndex = 0;

        foreach (var stage in pipelineStageCounts)
        {
            for (int i = 0; i < stage.Value; i++)
            {
                var prospectingDate = GetRandomDate(dateStart, dateFinish, random);
                var closingEstimation = prospectingDate.AddDays(random.Next(30, 90));
                var closingActual = closingEstimation.AddDays(random.Next(-10, 11));

                var companyName = SyrianCompanyNames[leadIndex % SyrianCompanyNames.Length];
                leadIndex++;

                var city = Cities[random.Next(Cities.Length)];
                var street = Streets[random.Next(Streets.Length)];
                var prefix = random.Next(2) == 0 ? "093" : "099";
                var mobile = $"{prefix}{random.Next(1000000, 9999999)}";

                var lead = new Lead
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(Lead), "", "LEA"),
                    Title = $"فرصة أعمال — {companyName}",
                    Description = $"متابعة عرض سوريا تيليكوم: خطوط وبيانات وأجهزة — مرحلة {stage.Key} — تاريخ أول تواصل {prospectingDate:yyyy-MM-dd}.",
                    CompanyName = companyName,
                    CompanyDescription = "عميل أعمال محتمل ضمن بيئة العرض التجريبية لسوريا تيليكوم.",
                    CompanyAddressStreet = street,
                    CompanyAddressCity = city,
                    CompanyAddressState = "سوريا",
                    CompanyAddressZipCode = $"{1000 + random.Next(8000)}",
                    CompanyAddressCountry = "سوريا",
                    CompanyPhoneNumber = mobile,
                    CompanyFaxNumber = $"011{random.Next(1000000, 9999999)}",
                    CompanyEmail = $"info{random.Next(100, 999)}@syriatelecom-lead.demo",
                    CompanyWebsite = $"https://lead-{random.Next(1000, 9999)}.syriatelecom-demo.local",
                    CompanyWhatsApp = mobile,
                    CompanyLinkedIn = "linkedin.com/syriatelecom-demo",
                    CompanyFacebook = "facebook.com/syriatelecom-demo",
                    CompanyInstagram = "instagram.com/syriatelecom-demo",
                    CompanyTwitter = "twitter.com/syriatelecom-demo",
                    DateProspecting = prospectingDate,
                    DateClosingEstimation = closingEstimation,
                    DateClosingActual = closingActual,
                    AmountTargeted = 10000 * Math.Ceiling((random.NextDouble() * 89) + 1),
                    AmountClosed = 10000 * Math.Ceiling((random.NextDouble() * 89) + 1),
                    BudgetScore = 10.0 * Math.Ceiling(random.NextDouble() * 10),
                    AuthorityScore = 10.0 * Math.Ceiling(random.NextDouble() * 10),
                    NeedScore = 10.0 * Math.Ceiling(random.NextDouble() * 10),
                    TimelineScore = 10.0 * Math.Ceiling(random.NextDouble() * 10),
                    PipelineStage = stage.Key,
                    ClosingStatus = (ClosingStatus)random.Next(0, Enum.GetNames(typeof(ClosingStatus)).Length),
                    ClosingNote = "ملاحظة إغلاق ديمو — مرتبطة بحملات سوريا تيليكوم التجريبية.",
                    CampaignId = GetRandomValue(confirmedCampaigns, random),
                    SalesTeamId = GetRandomValue(salesTeamIds, random)
                };

                await _leadRepository.CreateAsync(lead);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private static DateTime GetRandomDate(DateTime startDate, DateTime endDate, Random random)
    {
        var range = (endDate - startDate).Days;
        if (range <= 0) return startDate;
        return startDate.AddDays(random.Next(range));
    }

    private static string GetRandomValue(List<string> list, Random random)
    {
        return list[random.Next(list.Count)];
    }
}
