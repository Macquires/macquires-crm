// Demo-only — keep Name "unit" for ProductSeeder lookup; Arabic descriptions for UI.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class UnitMeasureSeeder
{
    private readonly ICommandRepository<UnitMeasure> _unitMeasureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnitMeasureSeeder(
        ICommandRepository<UnitMeasure> unitMeasureRepository,
        IUnitOfWork unitOfWork
    )
    {
        _unitMeasureRepository = unitMeasureRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var unitMeasures = new List<UnitMeasure>
        {
            new UnitMeasure { Name = "m", Description = "متر — أعمال مدنية / كابلات" },
            new UnitMeasure { Name = "kg", Description = "كيلوغرام — توريدات" },
            new UnitMeasure { Name = "hour", Description = "ساعة — خدمات ميدانية" },
            new UnitMeasure { Name = "unit", Description = "وحدة — خطوط وباقات وأجهزة (ديمو)" },
            new UnitMeasure { Name = "pcs", Description = "قطعة — ملحقات" }
        };

        foreach (var unitMeasure in unitMeasures)
        {
            await _unitMeasureRepository.CreateAsync(unitMeasure);
        }

        await _unitOfWork.SaveAsync();
    }
}
