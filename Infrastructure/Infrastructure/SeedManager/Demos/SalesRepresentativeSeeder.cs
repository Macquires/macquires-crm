// Demo-only — Arabic rep roster tied to Syria Telecom sales teams.
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class SalesRepresentativeSeeder
{
    private static readonly string[] ArabicFirstNames =
    [
        "رنين", "ليث", "نورة", "طارق", "منار", "سامي", "هبة", "كرم", "لينا", "بشار",
        "دانة", "فادي", "ميس", "زيد", "رنا", "عادل", "سلمى", "يامن", "غادة", "مازن"
    ];

    private readonly ICommandRepository<SalesRepresentative> _salesRepRepository;
    private readonly ICommandRepository<SalesTeam> _salesTeamRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public SalesRepresentativeSeeder(
        ICommandRepository<SalesRepresentative> salesRepRepository,
        ICommandRepository<SalesTeam> salesTeamRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _salesRepRepository = salesRepRepository;
        _salesTeamRepository = salesTeamRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var salesTeams = await _salesTeamRepository.GetQuery().ToListAsync();
        var nameIndex = 0;

        foreach (var team in salesTeams)
        {
            for (int i = 1; i <= 5; i++)
            {
                var first = ArabicFirstNames[nameIndex % ArabicFirstNames.Length];
                nameIndex++;
                var job = i == 1 ? "مدير مبيعات فرع" : "ممثل مبيعات — خطوط وبيانات";
                var prefix = random.Next(2) == 0 ? "093" : "099";

                var salesRep = new SalesRepresentative
                {
                    Name = $"{first} — {team.Name}",
                    Number = _numberSequenceService.GenerateNumber(nameof(SalesRepresentative), "", "SR"),
                    JobTitle = job,
                    EmployeeNumber = $"ST-EMP-{random.Next(10000, 99999)}",
                    PhoneNumber = $"{prefix}{random.Next(1000000, 9999999)}",
                    EmailAddress = $"rep.{nameIndex:D4}@syriatelecom-demo.local",
                    Description = $"مندوب مبيعات ضمن {team.Name} — تغطية خطوط وباقات سوريا تيليكوم (ديمو).",
                    SalesTeamId = team.Id
                };

                await _salesRepRepository.CreateAsync(salesRep);
            }
        }

        await _unitOfWork.SaveAsync();
    }
}
