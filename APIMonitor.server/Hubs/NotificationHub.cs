using Microsoft.AspNetCore.SignalR;

namespace APIMonitor.server.Hubs;

public class NotificationHub : Hub
{
    public async Task SendNotification(string userId, string title, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
    }
}