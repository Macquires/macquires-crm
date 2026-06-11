using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

[Authorize]
public sealed class IntegrationLiveHub : Hub
{
    public Task JoinIntegrationConsole() => Groups.AddToGroupAsync(Context.ConnectionId, "integration-console");
}
