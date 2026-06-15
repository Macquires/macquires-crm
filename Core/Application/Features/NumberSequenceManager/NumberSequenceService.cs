using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.NumberSequenceManager;

public class NumberSequenceService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ICommandRepository<NumberSequence> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public NumberSequenceService(
        ICommandRepository<NumberSequence> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    [Obsolete("Use GenerateNumberAsync to avoid sync-over-async.")]
    public string GenerateNumber(string entityName, string prefix, string suffix, bool useDate = true, int padding = 4)
        => GenerateNumberAsync(entityName, prefix, suffix, useDate, padding).GetAwaiter().GetResult();

    public async Task<string> GenerateNumberAsync(
        string entityName,
        string prefix,
        string suffix,
        bool useDate = true,
        int padding = 4,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entityName))
        {
            throw new ArgumentException("Parameter entityName must not be null", nameof(entityName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var sequence = await GetNumberSequenceAsync(entityName, prefix, suffix, cancellationToken);
            if (sequence != null)
            {
                sequence.LastUsedCount++;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                sequence = await InsertNumberSequenceAsync(entityName, prefix, suffix, cancellationToken);
            }

            var count = sequence.LastUsedCount?.ToString().PadLeft(padding, '0') ?? "1".PadLeft(padding, '0');
            var datePart = useDate ? DateTime.Now.ToString("yyyyMMdd") : "";
            return $"{prefix}{count}{datePart}{suffix}";
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<NumberSequence?> GetNumberSequenceAsync(
        string entityName,
        string prefix,
        string suffix,
        CancellationToken cancellationToken)
    {
        return await _repository.GetQuery()
            .FirstOrDefaultAsync(
                ns => ns.EntityName == entityName && ns.Prefix == prefix && ns.Suffix == suffix,
                cancellationToken);
    }

    private async Task<NumberSequence> InsertNumberSequenceAsync(
        string entityName,
        string prefix,
        string suffix,
        CancellationToken cancellationToken)
    {
        var newSequence = new NumberSequence
        {
            EntityName = entityName,
            Prefix = prefix,
            Suffix = suffix,
            LastUsedCount = 1,
        };

        _repository.Create(newSequence);
        await _unitOfWork.SaveAsync(cancellationToken);
        return newSequence;
    }
}
