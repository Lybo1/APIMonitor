using APIMonitor.server.Services.ThreatDetectionService;

namespace APIMonitor.server.Middleware;

public class IpBanMiddleware
{
    private readonly RequestDelegate next;
    private readonly IThreatDetectionService threatDetectionService;

    public IpBanMiddleware(RequestDelegate next, IThreatDetectionService threatDetectionService)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.threatDetectionService = threatDetectionService ?? throw new ArgumentNullException(nameof(threatDetectionService));
    }

    public async Task Invoke(HttpContext context)
    {
        bool isBlocked = await threatDetectionService.IsIpBlocked();

        if (isBlocked)
        {
            context.Response.StatusCode = 403;

            await context.Response.WriteAsync("Your IP has been temporarily banned due to suspicious activity.");
            
            return;
        }
        
        await next(context);
    }
}