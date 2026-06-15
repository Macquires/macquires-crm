using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

[Authorize]
public sealed class ExecutiveAlertsHub : Hub
{
    public Task JoinExecutiveMis() => Groups.AddToGroupAsync(Context.ConnectionId, "executive-mis");
}
