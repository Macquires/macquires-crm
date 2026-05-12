// Demo-only — regional & channel teams for Syria Telecom demo.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class SalesTeamSeeder
{
    private readonly ICommandRepository<SalesTeam> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SalesTeamSeeder(
        ICommandRepository<SalesTeam> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var salesTeams = new List<SalesTeam>
        {
            new SalesTeam { Name = "فرع دمشق — مراكز الخدمة" },
            new SalesTeam { Name = "فرع حلب — المعارض والتجزئة" },
            new SalesTeam { Name = "فرع حمص والوسط" },
            new SalesTeam { Name = "فرع الساحل — اللاذقية وطرطوس" },
            new SalesTeam { Name = "قناة الأعمال — B2B" },
            new SalesTeam { Name = "كول سنتر — مبيعات ومتابعة" },
            new SalesTeam { Name = "فرع الجنوب — درعا والسويداء" },
            new SalesTeam { Name = "فرع الشرقي — دير الزور والحسكة" },
            new SalesTeam { Name = "فرع الساحلي — طرطوس" },
            new SalesTeam { Name = "فرع العاصمة — تجزئة فاخرة" }
        };

        foreach (var salesTeam in salesTeams)
        {
            await _repository.CreateAsync(salesTeam);
        }

        await _unitOfWork.SaveAsync();
    }
}
