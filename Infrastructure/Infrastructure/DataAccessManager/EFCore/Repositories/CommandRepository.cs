using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Common;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Repositories;


public class CommandRepository<T> : ICommandRepository<T> where T : BaseEntity
{
    protected readonly CommandContext _context;

    public CommandRepository(CommandContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAtUtc = DateTime.UtcNow;
        await _context.AddAsync(entity, cancellationToken);
    }

    public void Create(T entity)
    {
        entity.CreatedAtUtc = DateTime.UtcNow;
        _context.Add(entity);
    }

    public void Update(T entity)
    {
        entity.UpdatedAtUtc = DateTime.UtcNow;
        var entry = _context.Entry(entity);
        // Already-tracked entities: let change detection run — _context.Update() corrupts RowVersion originals.
        if (entry.State == EntityState.Detached)
        {
            _context.Update(entity);
        }
    }

    public void Delete(T entity)
    {
        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _context.Update(entity);
    }

    public void Purge(T entity)
    {
        _context.Remove(entity);
    }

    public virtual async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<T>()
            .IsDeletedEqualTo()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity;
    }

    public virtual async Task<T?> GetBypassingBranchScopeAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>()
            .IgnoreQueryFilters()
            .IsDeletedEqualTo()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual T? Get(string id)
    {
        var entity = _context.Set<T>()
            .IsDeletedEqualTo()
            .SingleOrDefault(x => x.Id == id);

        return entity;
    }

    public virtual IQueryable<T> GetQuery()
    {
        var query = _context.Set<T>().AsQueryable();

        return query;
    }


}
