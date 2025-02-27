using System.Diagnostics;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace APIMonitor.server.Services.ApiScannerService;

public class ApiScannerService : IApiScannerService
{
    private readonly HttpClient httpClient;
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ApiScannerService> logger;

    private static readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
            (response, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"🔄 Retry {retryCount} after {timeSpan.TotalSeconds} seconds.");
            });

    private static readonly AsyncTimeoutPolicy<HttpResponseMessage> timeoutPolicy = Policy
        .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));

    private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,  // Allow 3 failures before breaking
            durationOfBreak: TimeSpan.FromSeconds(20),  // Wait 20 seconds before trying again
            onBreak: (response, breakDelay) =>
            {
                Console.WriteLine($"🚨 Circuit breaker OPEN for {breakDelay.TotalSeconds} seconds due to failures.");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit breaker RESET: API is now responsive.");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("⚠️ Circuit breaker HALF-OPEN: Testing API health.");
            });

    
    public ApiScannerService(HttpClient httpClient, ApplicationDbContext dbContext, IMemoryCache memoryCache, ILogger<ApiScannerService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ScanApisAsync()
    {
        List<string> apiUrls = await dbContext.ApiEndpoints
            .Where(api => api.IsActive)
            .Select(api => api.Url)
            .ToListAsync();

        if (apiUrls.Count == 0)
        {
            logger.LogWarning("⚠️ No active APIs found to scan.");
            return;
        }

        logger.LogInformation($"🚀 Starting batch scan for {apiUrls.Count} APIs...");

        List<Task<ApiMetrics>> scanTasks = apiUrls
            .Where(apiUrl => !memoryCache.TryGetValue(apiUrl, out _)) 
            .Select(apiUrl => ScanSingleApiAsync(apiUrl))
            .ToList();

        try
        {
            await Task.WhenAll(scanTasks);
            
            logger.LogInformation($"✅ Batch API scan completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error during batch API scan: {ex.Message}");
        }
    }

    public async Task<ApiMetrics> ScanSingleApiAsync(string apiUrl)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            throw new ArgumentException("API URL cannot be empty", nameof(apiUrl));
        }
        
        if (memoryCache.TryGetValue(apiUrl, out ApiMetrics? cachedMetrics))
        {
            logger.LogInformation($"📌 Returning cached metrics for {apiUrl}.");
            
            return cachedMetrics!;
        }
        
        Stopwatch stopWatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        bool success = false;
        string responseBody = string.Empty;

        try
        {
            response = await circuitBreakerPolicy.ExecuteAsync(() =>
                retryPolicy.ExecuteAsync(() =>
                    timeoutPolicy.ExecuteAsync(() => httpClient.GetAsync(apiUrl))));
            
            stopWatch.Stop();
            responseBody = await response.Content.ReadAsStringAsync();
            success = response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            stopWatch.Stop();
            logger.LogError($"❌ Error scanning {apiUrl}: {e.Message}");
        }

        ApiRequestLog apiRequestLog = new()
        {
            IpAddress = "N/A",
            HttpMethod = "GET",
            StatusCode = response?.StatusCode.GetHashCode() ?? 500,
            ResponseTime = stopWatch.Elapsed,
            Endpoint = apiUrl,
            RequestPayload = "N/A",
            ResponsePayload = responseBody
        };

        await dbContext.ApiRequestLogs.AddAsync(apiRequestLog);
        await dbContext.SaveChangesAsync();

        ApiMetrics apiMetrics = new()
        {
            TotalRequests = 1,
            RequestsPerMinute = 1,
            AverageResponseTime = stopWatch.Elapsed,
            ErrorsCount = success ? 0 : 1,
        };
        
        memoryCache.Set(apiUrl, apiMetrics, TimeSpan.FromMinutes(5));
        
        await dbContext.ApiMetrics.AddAsync(apiMetrics);
        await dbContext.SaveChangesAsync();

        return apiMetrics;
    }
}