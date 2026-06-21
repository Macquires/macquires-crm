namespace Application.Common.Audit;

public interface IAuditActorDisplayNameResolver
{
    Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}
