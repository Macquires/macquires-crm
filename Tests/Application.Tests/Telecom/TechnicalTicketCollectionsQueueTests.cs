using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Application.Tests.Dashboard;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class TechnicalTicketCollectionsQueueTests
{
    [Fact]
    public async Task EnqueueDeviceInstallmentCollectionsAsync_creates_collections_ticket()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, opId) = await SeedDeviceSaleLineAsync(ctx);

        var service = BuildService(ctx);
        var op = await ctx.TelecomOperationRequest.AsNoTracking().FirstAsync(o => o.Id == opId);

        var ticket = await service.EnqueueDeviceInstallmentCollectionsAsync(
            op,
            "CNT-VAL-14",
            "قسط متأخر",
            CancellationToken.None);

        Assert.NotNull(ticket);
        Assert.Equal(TechnicalTicketCategory.Collections, ticket!.TicketCategory);
        Assert.Contains(opId, ticket.PayloadJson ?? "");

        var count = await ctx.TelecomTechnicalTicket.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task EnqueueDeviceInstallmentCollectionsAsync_is_idempotent_for_open_ticket()
    {
        await using var ctx = CreateContext();
        var (_, _, opId) = await SeedDeviceSaleLineAsync(ctx);
        var service = BuildService(ctx);
        var op = await ctx.TelecomOperationRequest.AsNoTracking().FirstAsync(o => o.Id == opId);

        var first = await service.EnqueueDeviceInstallmentCollectionsAsync(op, "CNT-1", "msg", CancellationToken.None);
        var second = await service.EnqueueDeviceInstallmentCollectionsAsync(op, "CNT-1", "msg", CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal(1, await ctx.TelecomTechnicalTicket.CountAsync());
    }

    private static TechnicalTicketQueueIngestionService BuildService(QueryContext ctx)
    {
        var uow = new CtxUnitOfWork(ctx);
        var ticketRepo = new EfCommandRepository<TelecomTechnicalTicket>(ctx.TelecomTechnicalTicket);
        var seqRepo = new EfCommandRepository<NumberSequence>(ctx.NumberSequence);
        var numberSequence = new NumberSequenceService(seqRepo, uow);
        return new TechnicalTicketQueueIngestionService(ticketRepo, uow, numberSequence, ctx);
    }

    private static async Task<(string ProfileId, string AssetId, string OperationId)> SeedDeviceSaleLineAsync(QueryContext ctx)
    {
        var customer = IndividualCustomer.Create(
            "عميل تقسيط",
            "ACC-DEV",
            "12345678999",
            PostalAddress.Empty,
            null,
            null,
            null,
            null);
        customer.Id = Guid.NewGuid().ToString();
        customer.IsDeleted = false;

        var profileId = Guid.NewGuid().ToString();
        var profile = new SubscriberProfile
        {
            Id = profileId,
            CustomerId = customer.Id,
            IsDeleted = false,
            OperationalStatus = SubscriberOperationalStatus.Active,
        };

        var assetId = Guid.NewGuid().ToString();
        var asset = new MsisdnAsset
        {
            Id = assetId,
            Msisdn = "0939111222",
            IsDeleted = false,
            SubscriberProfileId = profileId,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);

        var opId = Guid.NewGuid().ToString();
        var op = new TelecomOperationRequest
        {
            Id = opId,
            Number = "DEV-TKT-1",
            Kind = TelecomOperationKind.DeviceSale,
            Status = TelecomOperationStatus.Completed,
            SubscriberProfileId = profileId,
            MsisdnAssetId = assetId,
            IsDeleted = false,
        };

        ctx.Customer.Add(customer);
        ctx.SubscriberProfile.Add(profile);
        ctx.MsisdnAsset.Add(asset);
        ctx.TelecomOperationRequest.Add(op);
        await ctx.SaveChangesAsync();

        return (profileId, assetId, opId);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private sealed class CtxUnitOfWork(QueryContext ctx) : IUnitOfWork
    {
        public Task SaveAsync(CancellationToken cancellationToken = default) =>
            ctx.SaveChangesAsync(cancellationToken);

        public void Save() => ctx.SaveChanges();

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class EfCommandRepository<T>(DbSet<T> set) : ICommandRepository<T>
        where T : BaseEntity
    {
        public Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            set.Add(entity);
            return Task.CompletedTask;
        }

        public void Create(T entity) => set.Add(entity);

        public void Update(T entity) => set.Update(entity);

        public void Delete(T entity) => set.Remove(entity);

        public void Purge(T entity) => set.Remove(entity);

        public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            await set.FindAsync([id], cancellationToken);

        public T? Get(string id) => set.Find(id);

        public IQueryable<T> GetQuery() => set;
    }
}
