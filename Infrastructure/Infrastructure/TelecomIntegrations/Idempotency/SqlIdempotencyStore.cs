using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TelecomIntegrations.Idempotency;

public sealed class SqlIdempotencyStore : IIdempotencyStore
{
    private readonly ICommandRepository<IdempotencyRecord> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SqlIdempotencyStore(
        ICommandRepository<IdempotencyRecord> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> TryBeginAsync(
        string scope,
        string key,
        string? requestHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetQuery()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Scope == scope && x.Key == key, cancellationToken);

        if (existing != null && existing.ExpiresAtUtc > DateTime.UtcNow)
        {
            return string.IsNullOrEmpty(existing.ResponsePayload);
        }

        if (existing != null)
        {
            existing.IsDeleted = true;
            _repository.Update(existing);
        }

        await _repository.CreateAsync(new IdempotencyRecord
        {
            Scope = scope,
            Key = key,
            RequestHash = requestHash,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
        }, cancellationToken);

        try
        {
            await _unitOfWork.SaveAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task CompleteAsync(
        string scope,
        string key,
        string? responsePayload,
        CancellationToken cancellationToken = default)
    {
        var row = await _repository.GetQuery()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Scope == scope && x.Key == key, cancellationToken);

        if (row == null)
        {
            return;
        }

        row.ResponsePayload = responsePayload;
        _repository.Update(row);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task<string?> GetCompletedResponseAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetQuery()
            .Where(x => !x.IsDeleted && x.Scope == scope && x.Key == key && x.ExpiresAtUtc > DateTime.UtcNow)
            .Select(x => x.ResponsePayload)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
