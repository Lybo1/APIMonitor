namespace APIMonitor.server.Middleware;

public class RequestInfoMiddleware
{
    private readonly RequestDelegate next;

    public RequestInfoMiddleware(RequestDelegate next)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task Invoke(HttpContext context)
    {
        string? userIp = context.Connection.RemoteIpAddress?.ToString();
        string? userAgent = context.Request.Headers["User-Agent"].ToString();
        
        context.Items["UserIP"] = userIp;
        context.Items["User-Agent"] = userAgent;
        
        await next(context);
    }
}