namespace APIMonitor.server.Services.RateLimitService;

public interface IRateLimitService
{
    Task<bool> IsRateLimitedAsync(HttpContext context, string action);
}