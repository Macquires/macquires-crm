using Application.Common.Integrations;
using Application.Common.Telecom;
using Application.Common.Telecom.Billing;
using Application.Common.Telecom.HlrFailure;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class TelecomHlrFailureCompensatorTests
{
    [Fact]
    public async Task CompensateAsync_reconnect_failure_returns_rollback_message()
    {
        var billing = new FakeBillingRoutingOrchestrator(success: true);
        var compensator = new TelecomHlrFailureCompensator(
            billing,
            new HlrFailureLocalRevertService(
                new FakeRepo<TelecomSubscription>(),
                new FakeRepo<MsisdnAsset>(),
                new FakeRepo<SimInventory>(),
                new FakeRepo<SubscriberProfile>(),
                new FakeRepo<TelecomOperationRequest>(),
                new FakeBindingCompensator(),
                new FakeUnitOfWork()));

        var operation = new TelecomOperationRequest
        {
            Id = "op-1",
            Kind = TelecomOperationKind.Reconnect,
            SubscriberProfileId = "sp-1",
            MsisdnAssetId = "ms-1",
        };

        var lineContext = new TelecomLineProvisionContext(
            Msisdn: "0931111111",
            Iccid: null,
            Imsi: null,
            ProductServiceCode: null,
            SubscriptionTypeCode: "POSTPAID",
            InitialDeposit: null);

        var result = await compensator.CompensateAsync(
            operation,
            lineContext,
            "user-1",
            "HLR timeout",
            CancellationToken.None);

        Assert.True(result.CbsReversed);
        Assert.Contains("VAL-09-ROLLBACK", result.MessageAr);
    }

    private sealed class FakeBillingRoutingOrchestrator(bool success) : IBillingRoutingOrchestrator
    {
        public BillingRoutingDecision ResolveProvisionRouting(
            TelecomOperationKind kind,
            string? subscriptionTypeCode,
            string? sourceSubscriptionTypeCode = null,
            string? targetSubscriptionTypeCode = null) =>
            BillingLineTypeResolver.ResolveProvisionRouting(
                kind,
                subscriptionTypeCode,
                sourceSubscriptionTypeCode,
                targetSubscriptionTypeCode);

        public bool RoutesPrimaryThroughIn(TelecomOperationKind kind, string? subscriptionTypeCode) =>
            BillingLineTypeResolver.IsPrepaid(subscriptionTypeCode);

        public string IntegrationFalloutSource(TelecomOperationKind kind, string? subscriptionTypeCode) =>
            "CBS";

        public Task<BillingProvisionResult> CompensateProvisionAsync(
            TelecomOperationRequest operation,
            TelecomLineProvisionContext lineContext,
            BillingProvisionRequest billingRequest,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BillingProvisionResult(success, success ? "reversed" : "failed"));

        public Task<BillingProvisionResult> ProvisionAsync(
            TelecomOperationRequest operation,
            TelecomLineProvisionContext lineContext,
            BillingProvisionRequest billingRequest,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BillingProvisionResult(true, "ok"));

        public Task<BillingRechargeResult> RechargeAsync(
            string? subscriptionTypeCode,
            BillingRechargeRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BillingRechargeResult(true, "ok"));

        public Task<BillingRechargeResult> ReverseRechargeAsync(
            string? subscriptionTypeCode,
            BillingReverseRechargeRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BillingRechargeResult(true, "ok"));
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

    private sealed class FakeUnitOfWork : Application.Common.Repositories.IUnitOfWork
    {
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Save() { }

        public Task<Application.Common.Repositories.IUnitOfWorkTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeRepo<T> : Application.Common.Repositories.ICommandRepository<T>
        where T : Domain.Common.BaseEntity
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
