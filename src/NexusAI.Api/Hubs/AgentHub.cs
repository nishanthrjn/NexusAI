using Microsoft.AspNetCore.SignalR;

namespace NexusAI.Api.Hubs;

public class AgentHub : Hub
{
    // Clients subscribe to a session by calling JoinSession
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}
