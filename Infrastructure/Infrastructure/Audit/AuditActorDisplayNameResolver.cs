using Application.Common.Audit;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Audit;

public sealed class AuditActorDisplayNameResolver : IAuditActorDisplayNameResolver
{
    private readonly DataContext _context;

    public AuditActorDisplayNameResolver(DataContext context) => _context = context;

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Display = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
            })
            .ToDictionaryAsync(
                x => x.Id,
                x => string.IsNullOrWhiteSpace(x.Display) ? x.Id : x.Display,
                cancellationToken);
    }
}
