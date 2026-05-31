using Application.Common.Audit;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Audit;

public class UserAuditService : IUserAuditService
{
    private readonly DataContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAuditService(DataContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(UserAuditLogRequest entry, CancellationToken cancellationToken = default)
    {
        var ip = entry.IpAddress ?? ResolveClientIp();
        var row = new UserAuditLog
        {
            UserId = entry.UserId,
            ActorUserId = entry.ActorUserId,
            ActionType = entry.ActionType,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            SummaryAr = entry.SummaryAr,
            PayloadJson = entry.Payload == null ? null : UserAuditJsonSerializer.Serialize(entry.Payload),
            OccurredAtUtc = DateTime.UtcNow,
            IpAddress = ip,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = entry.ActorUserId,
            IsDeleted = false,
        };

        await _context.UserAuditLog.AddAsync(row, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string? ResolveClientIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
        {
            return null;
        }

        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return ctx.Connection.RemoteIpAddress?.ToString();
    }
}
