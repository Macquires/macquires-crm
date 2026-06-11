using Application.Common.Repositories;
using Application.Features.GeoCityManager.Commands;
using Application.Features.GeoCityManager.Queries;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Administration;

using Application.Tests.TestSupport;

public class GeoCityManagerTests
{
    [Fact]
    public void CreateGeoCityValidator_rejects_empty_name()
    {
        var validator = new CreateGeoCityValidator();
        var result = validator.Validate(new CreateGeoCityRequest { Name = "", Governorate = "دمشق" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateGeoCityValidator_rejects_empty_governorate()
    {
        var validator = new CreateGeoCityValidator();
        var result = validator.Validate(new CreateGeoCityRequest { Name = "دمشق", Governorate = "" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateGeoCityHandler_persists_city()
    {
        await using var ctx = CreateContext();
        var repo = new EfCommandRepository<GeoCity>(ctx.GeoCity);
        var handler = new CreateGeoCityHandler(repo, new CtxUnitOfWork(ctx));

        var result = await handler.Handle(
            new CreateGeoCityRequest
            {
                Name = "حلب",
                Governorate = "حلب",
                IsActive = true,
                SortOrder = 2,
                CreatedById = "admin",
            },
            CancellationToken.None);

        Assert.NotNull(result.Data?.Id);
        Assert.Single(await ctx.GeoCity.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task GetGeoCityList_ActiveOnly_returns_active_cities_only()
    {
        await using var ctx = CreateContext();
        ctx.GeoCity.AddRange(
            new GeoCity { Name = "دمشق", Governorate = "دمشق", IsActive = true, SortOrder = 1 },
            new GeoCity { Name = "قديمة", Governorate = "حلب", IsActive = false, SortOrder = 99 });
        await ctx.SaveChangesAsync();

        var mapper = CreateMapper();
        var handler = new GetGeoCityListHandler(mapper, ctx);
        var result = await handler.Handle(new GetGeoCityListRequest { ActiveOnly = true }, CancellationToken.None);

        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("دمشق", result.Data![0].Name);
    }

    [Fact]
    public async Task GetGeoCityList_orders_by_sort_then_name()
    {
        await using var ctx = CreateContext();
        ctx.GeoCity.AddRange(
            new GeoCity { Name = "حلب", Governorate = "حلب", IsActive = true, SortOrder = 2 },
            new GeoCity { Name = "دمشق", Governorate = "دمشق", IsActive = true, SortOrder = 1 });
        await ctx.SaveChangesAsync();

        var mapper = CreateMapper();
        var handler = new GetGeoCityListHandler(mapper, ctx);
        var result = await handler.Handle(new GetGeoCityListRequest(), CancellationToken.None);

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("دمشق", result.Data[0].Name);
        Assert.Equal("حلب", result.Data[1].Name);
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            c => c.AddProfile<GetGeoCityListProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

    private static QueryContext CreateContext()
    {
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
