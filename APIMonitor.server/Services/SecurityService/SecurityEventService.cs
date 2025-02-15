using APIMonitor.server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace APIMonitor.server.Services.SecurityService;
 
public class SecurityEventService : ISecurityEventService
{
    public event Action<string, string, string>? SuspiciousLoginAttempt;
    
    private readonly IHubContext<SecurityHub> hubContext;

    public SecurityEventService(IHubContext<SecurityHub> hubContext)
    {
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public void TriggerSuspiciousLogin(string userId, string ipAddress, string userAgent)
    {
        SuspiciousLoginAttempt?.Invoke(userId, ipAddress, userAgent);
        
        hubContext.Clients.User(userId).SendAsync("SuspiciousLoginAlert", new 
        {
            message = "We detected a login attempt from a new device.",
            ipAddress,
            userAgent
        });
    }
}