using System.Security.Claims;
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
        string userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID missing.");
        
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await Clients.Caller.SendAsync("Connected", "Welcome! You are in the notification hub.");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
    }
}