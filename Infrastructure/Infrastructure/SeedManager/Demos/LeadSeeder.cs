using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class LeadSeeder
{
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

        foreach (var stage in pipelineStageCounts)
        {
            for (int i = 0; i < stage.Value; i++)
            {
                var prospectingDate = GetRandomDate(dateStart, dateFinish);
                var closingEstimation = prospectingDate.AddDays(random.Next(30, 90));
                var closingActual = closingEstimation.AddDays(random.Next(-10, 11));

                var lead = new Lead
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(Lead), "", "LEA"),
                    Title = $"Lead from {prospectingDate:MMMM yyyy}",
                    Description = $"Lead description for {prospectingDate:MMMM yyyy}",
                    CompanyName = $"Company Name {random.Next(1000, 9999)}",
                    CompanyDescription = "Sample company description",
                    CompanyAddressStreet = "123 Main St",
                    CompanyAddressCity = "Anytown",
                    CompanyAddressState = "State",
                    CompanyAddressZipCode = "12345",
                    CompanyAddressCountry = "Country",
                    CompanyPhoneNumber = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    CompanyFaxNumber = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    CompanyEmail = $"info{random.Next(1000, 9999)}@company.com",
                    CompanyWebsite = $"www.company{random.Next(1000, 9999)}.com",
                    CompanyWhatsApp = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    CompanyLinkedIn = $"linkedin.com/company{random.Next(1000, 9999)}",
                    CompanyFacebook = $"facebook.com/company{random.Next(1000, 9999)}",
                    CompanyInstagram = $"instagram.com/company{random.Next(1000, 9999)}",
                    CompanyTwitter = $"twitter.com/company{random.Next(1000, 9999)}",
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
                    ClosingNote = "Sample closing note",
                    CampaignId = GetRandomValue(confirmedCampaigns, random),
                    SalesTeamId = GetRandomValue(salesTeamIds, random)
                };

                await _leadRepository.CreateAsync(lead);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private static DateTime GetRandomDate(DateTime startDate, DateTime endDate)
    {
        var range = (endDate - startDate).Days;
        return startDate.AddDays(new Random().Next(range));
    }

    private static string GetRandomValue(List<string> list, Random random)
    {
        return list[random.Next(list.Count)];
    }

    private static DateTime[] GetRandomDays(int year, int month, int count)
    {
        var random = new Random();
        var daysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).ToList();
        var selectedDays = new List<int>();

        for (int i = 0; i < count; i++)
        {
            int day = daysInMonth[random.Next(daysInMonth.Count)];
            selectedDays.Add(day);
            daysInMonth.Remove(day);
        }

        return selectedDays.Select(day => new DateTime(year, month, day)).ToArray();
    }
}