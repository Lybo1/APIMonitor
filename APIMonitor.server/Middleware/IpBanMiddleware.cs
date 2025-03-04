using APIMonitor.server.Services.ThreatDetectionService;

namespace APIMonitor.server.Middleware;

public class IpBanMiddleware
{
    private readonly RequestDelegate next;
    private readonly IServiceProvider serviceProvider;

    public IpBanMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task Invoke(HttpContext context)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IThreatDetectionService threatDetectionService = scope.ServiceProvider.GetRequiredService<IThreatDetectionService>();
            bool isBlocked = await threatDetectionService.IsIpBlocked();

            if (isBlocked)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Your IP has been temporarily banned due to suspicious activity.");
                return;
            }
        }
        
        await next(context);
    }
}