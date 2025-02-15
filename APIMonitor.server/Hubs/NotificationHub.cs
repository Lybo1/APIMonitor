using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace APIMonitor.server.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public async Task SendNotification(string userId, string title, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
    }
    
    public override async Task OnConnectedAsync()
    {
        string userId = Context.UserIdentifier ?? throw new UnauthorizedAccessException("User ID missing.");
        
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = Context.UserIdentifier ?? string.Empty;
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
    }
}