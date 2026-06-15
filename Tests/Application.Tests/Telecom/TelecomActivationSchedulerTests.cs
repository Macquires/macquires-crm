using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.Activation;
using Application.Common.Telecom.HlrFailure;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public sealed class TelecomActivationSchedulerTests
{
    [Fact]
    public async Task DeferToScheduledAsync_transitions_operation_to_scheduled()
    {
        var operation = new TelecomOperationRequest
        {
            Id = "op-sched",
            Status = TelecomOperationStatus.Draft,
            Kind = TelecomOperationKind.Migration,
            MigrationEffectiveDateUtc = DateTime.UtcNow.AddDays(3),
        };

        var repo = new FakeRepo<TelecomOperationRequest>();
        repo.Create(operation);
        var orchestrator = new FakeOrchestrator();
        var scheduler = new TelecomActivationScheduler(orchestrator, repo, new FakeUnitOfWork());

        var result = await scheduler.DeferToScheduledAsync(operation, "actor-1", CancellationToken.None);

        Assert.False(result.IdempotentReplay);
        Assert.Equal(TelecomOperationStatus.Scheduled, operation.Status);
        Assert.Contains("جدولة", result.Message, StringComparison.Ordinal);
        Assert.Contains(TelecomOperationStatus.PendingDocuments, orchestrator.Transitions.Select(t => t.Status));
        Assert.Contains(TelecomOperationStatus.Confirmed, orchestrator.Transitions.Select(t => t.Status));
        Assert.Contains(TelecomOperationStatus.Scheduled, orchestrator.Transitions.Select(t => t.Status));
    }

    [Fact]
    public async Task HlrFailureLocalRevert_reconnect_rolls_back_profile_and_asset()
    {
        var profile = new SubscriberProfile
        {
            Id = "sp-1",
            OperationalStatus = SubscriberOperationalStatus.Active,
        };
        var asset = new MsisdnAsset
        {
            Id = "ms-1",
            Msisdn = "0931111111",
            PoolStatus = MsisdnPoolStatus.Active,
        };

        var profileRepo = new FakeRepo<SubscriberProfile>();
        profileRepo.Create(profile);
        var assetRepo = new FakeRepo<MsisdnAsset>();
        assetRepo.Create(asset);

        var service = new HlrFailureLocalRevertService(
            new FakeRepo<TelecomSubscription>(),
            assetRepo,
            new FakeRepo<SimInventory>(),
            profileRepo,
            new FakeRepo<TelecomOperationRequest>(),
            new FakeBindingCompensator(),
            new FakeUnitOfWork());

        var operation = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Reconnect,
            SubscriberProfileId = "sp-1",
            MsisdnAssetId = "ms-1",
        };

        var changed = await service.RevertLocalBindAsync(operation, "actor", CancellationToken.None);

        Assert.True(changed);
        Assert.Equal(SubscriberOperationalStatus.Suspended, profile.OperationalStatus);
        Assert.Equal(MsisdnPoolStatus.Suspended, asset.PoolStatus);
    }

    private sealed class FakeOrchestrator : ITelecomOperationOrchestrator
    {
        public List<(TelecomOperationStatus Status, string? Note)> Transitions { get; } = [];

        public Task TransitionAsync(
            TelecomOperationRequest operation,
            TelecomOperationStatus toStatus,
            string? actorUserId,
            string? note,
            CancellationToken cancellationToken)
        {
            Transitions.Add((toStatus, note));
            operation.Status = toStatus;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBindingCompensator : ISubscriptionBindingCompensator
    {
        public Task CompensateAsync(
            string subscriptionId,
            string msisdnAssetId,
            string simInventoryId,
            string subscriberProfileId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Save() { }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeRepo<T> : ICommandRepository<T>
        where T : BaseEntity
    {
        private readonly Dictionary<string, T> _items = new(StringComparer.Ordinal);

        public Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public void Create(T entity) => _items[entity.Id] = entity;

        public void Update(T entity) => _items[entity.Id] = entity;

        public void Delete(T entity) => _items.Remove(entity.Id);

        public void Purge(T entity) => _items.Remove(entity.Id);

        public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

        public T? Get(string id) => _items.TryGetValue(id, out var item) ? item : null;

        public IQueryable<T> GetQuery() => _items.Values.AsQueryable();
    }
}
