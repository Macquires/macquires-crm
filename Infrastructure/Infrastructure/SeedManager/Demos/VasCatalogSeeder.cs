using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class VasCatalogSeeder
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomValueAddedService> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public VasCatalogSeeder(
        IQueryContext query,
        ICommandRepository<TelecomValueAddedService> repository,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        await EnsureCatalogAsync();
    }

    /// <summary>Upserts Syrian VAS catalog entries by <see cref="TelecomValueAddedService.ServiceCode"/>.</summary>
    public async Task EnsureCatalogAsync()
    {
        var existing = await _query.TelecomValueAddedService
            .Where(v => !v.IsDeleted)
            .ToListAsync();

        var byCode = existing
            .Where(v => !string.IsNullOrWhiteSpace(v.ServiceCode))
            .ToDictionary(v => v.ServiceCode, StringComparer.Ordinal);

        var added = false;

        foreach (var def in DemoSyrianTelecomCatalog.VasServices)
        {
            if (byCode.ContainsKey(def.ServiceCode))
            {
                continue;
            }

            await _repository.CreateAsync(new TelecomValueAddedService
            {
                ServiceCode = def.ServiceCode,
                NameAr = def.NameAr,
                NameEn = def.NameEn,
                Description = def.Description,
                MonthlyFee = def.MonthlyFee,
                IsActive = true,
                SortOrder = def.SortOrder,
                HlrCommandTemplate = DemoSyrianTelecomCatalog.HlrTemplate(def.ServiceCode),
            }, default);

            added = true;
        }

        if (added)
        {
            await _unitOfWork.SaveAsync(default);
        }
    }
}
