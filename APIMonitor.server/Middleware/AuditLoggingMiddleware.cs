using System.Security.Claims;
using APIMonitor.server.Services.AuditLogService;

namespace APIMonitor.server.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly IServiceProvider provider;

    public AuditLoggingMiddleware(RequestDelegate next, IServiceProvider provider)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider)); 
    }

    public async Task Invoke(HttpContext context)
    {
        DateTime requestStartTime = DateTime.UtcNow;

        using (IServiceScope scope = provider.CreateScope())
        {
            IAuditLogService auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            await next(context);

            if (context.User.Identity?.IsAuthenticated == true)
            {
                int userId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                string action = $"{context.Request.Method} {context.Request.Path}";
                string details = $"Query: {context.Request.QueryString}";
                
                await auditLogService.LogActionAsync(userId, action, details, requestStartTime);
            }

        }
    }
}