using System.Diagnostics;
using System.Net;
using System.Net.Http;
using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.Bcpg;
using PacketDotNet;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SharpPcap;
using Packet = PacketDotNet.Packet;

namespace APIMonitor.server.Services.ApiScannerService;

public class ApiScannerService : IApiScannerService
{
    private readonly HttpClient httpClient;
    private readonly ICaptureDevice captureDevice;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ApiScannerService> logger;
    private readonly IMapper mapper;
    private readonly Dictionary<string , ApiMetrics> metrics = new();
    private readonly List<PacketInfo> packets = new();
    private readonly object packetLock = new();
    private readonly IHubContext<NotificationHub> hubContext;
    
    public ApiScannerService(
        HttpClient httpClient,
        ICaptureDevice captureDevice,
        IMemoryCache memoryCache,
        ILogger<ApiScannerService> logger,
        IMapper mapper,
        IHubContext<NotificationHub> hubContext)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.captureDevice = captureDevice ?? throw new ArgumentNullException(nameof(captureDevice));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        
        this.captureDevice.OnPacketArrival += CaptureDevice_OnPacketArrival;
    }

    private void CaptureDevice_OnPacketArrival(object sender, PacketCapture e)
    {
        RawCapture? rawPacket = e.GetPacket();
        Packet? packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
        
        EthernetPacket? ethernetPacket = packet.Extract<EthernetPacket>();
        IPPacket? ipPacket = packet.Extract<IPPacket>();
        TcpPacket? tcpPacket = packet.Extract<TcpPacket>();

        PacketInfo packetInfo = new()
        {
            SourceIp = ipPacket?.SourceAddress.ToString() ?? "Unknown",
            DestinationIp = ipPacket?.DestinationAddress.ToString() ?? "Unknown",
            SourceMac = ethernetPacket.SourceHardwareAddress?.ToString() ?? "Unknown",
            DestinationMac = ethernetPacket.DestinationHardwareAddress?.ToString() ?? "Unknown",
            Protocol = ipPacket?.Protocol.ToString() ?? "Unknown",
            Length = rawPacket.Data.Length,
            Timestamp = rawPacket.Timeval.Date,
            PayloadPreview = tcpPacket != null && tcpPacket.PayloadData.Length > 0 ? BitConverter.ToString(tcpPacket.PayloadData.Take(16).ToArray()) : "No payload"
        };

        lock (packetLock)
        {
            packets.Add(packetInfo);
        }
        
        logger.LogDebug($"Captured packet: {packetInfo.SourceIp} -> {packetInfo.DestinationIp}, {packetInfo.Length} bytes");
    }

    public async Task<(ApiMetrics Metrics, HttpStatusCode StatusCode, string ResponseSnippet)> ScanSingleApiAsync(string apiUrl, string? method = "GET", string? apiKey = null, string? userId = null, bool forceRefresh = false)
    {
        if (!metrics.TryGetValue(apiUrl, out ApiMetrics? metric))
        {
            metric = new ApiMetrics
            {
                Endpoint = apiUrl,
                TimeStamp = DateTime.UtcNow,
                TotalRequests = 0,
                RequestsPerMinute = 1,
                ErrorsCount = 0
            };
            
            metrics[apiUrl] = metric;
        }
        
        if (!forceRefresh && memoryCache.TryGetValue(apiUrl, out ApiScanResult? cachedResult))
        {
            logger.LogInformation($"Returning cached result for {apiUrl}");
                
            return (metric, HttpStatusCode.OK, cachedResult!.BodySnippet);
        }

        metric.TotalRequests++;
        
        Stopwatch stopwatch = Stopwatch.StartNew();

        HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method!), apiUrl);
    
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
        }
        
        if (!string.IsNullOrEmpty(userId))
        {
            request.Headers.Add("X-User-Id", userId);
        }

        HttpResponseMessage? response = null;

        lock (packetLock)
        {
            packets.Clear();
        }
        
        captureDevice.Open();
        captureDevice.Filter = $"host {new Uri(apiUrl).Host}";
        captureDevice.StartCapture();

        try
        {
            AsyncTimeoutPolicy<HttpResponseMessage>? timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
            AsyncRetryPolicy? retryPolicy = Policy.Handle<Exception>().RetryAsync(3, onRetry: (ex, retryCount) => logger.LogWarning($"Retry {retryCount} for {apiUrl} due to {ex.Message}"));
            
            AsyncCircuitBreakerPolicy? circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1),
                    onBreak: (ex, breakDelay) => logger.LogError($"Circuit breaker opened for {breakDelay} due to {ex.Message}"),
                    onReset: () => logger.LogInformation("Circuit breaker reset"));

            response = await circuitBreakerPolicy.ExecuteAsync(
                () => retryPolicy.ExecuteAsync(
                    () => timeoutPolicy.ExecuteAsync(async () =>
                        await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))));

            stopwatch.Stop();
            
            string responseBody = await response.Content.ReadAsStringAsync();

            ApiScanResult detailedResult = new ApiScanResult
            {
                Status = $"{(int)response.StatusCode} {response.StatusCode}",
                Latency = new LatencyMetrics
                {
                    DnsResolution = "N/A", 
                    Connect = "N/A",       
                    TotalRequest = $"{stopwatch.Elapsed.TotalMilliseconds:F2}ms"
                },
                Headers = response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}").Take(3).ToList(),
                BodySnippet = responseBody.Length > 0 ? responseBody.Substring(0, Math.Min(100, responseBody.Length)) + "..." : "No body",
                Health = GetHealthStatus(stopwatch.Elapsed.TotalMilliseconds),
                ColorHint = GetColorHint(stopwatch.Elapsed.TotalMilliseconds),
                Packets = new List<PacketInfo>(packets)
            };

            long totalMs = (long)metric.AverageResponseTime.TotalMilliseconds * (metric.TotalRequests - 1) + stopwatch.ElapsedMilliseconds;
            metric.AverageResponseTime = TimeSpan.FromMilliseconds(totalMs / metric.TotalRequests);

            double minutesElapsed = (DateTime.UtcNow - metric.TimeStamp).TotalMinutes;
            metric.RequestsPerMinute = minutesElapsed > 0 ? (int)(metric.TotalRequests / minutesElapsed) : 1;

            memoryCache.Set(apiUrl, detailedResult, TimeSpan.FromMinutes(5));

            return (metric, response.StatusCode, detailedResult.BodySnippet);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metric.ErrorsCount++;

            ApiScanResult detailedResult = new()
            {
                Status = "Failed",
                Latency = new LatencyMetrics { DnsResolution = "N/A", Connect = "N/A", TotalRequest = "N/A" },
                Headers = new List<string> { "N/A" },
                BodySnippet = $"Error: {ex.Message}",
                Health = "❌ Unhealthy: Scan Failed ❌",
                ColorHint = "red",
                Packets = new List<PacketInfo>(packets)
            };

            return (metric, HttpStatusCode.RequestTimeout, detailedResult.BodySnippet);
        }
        finally
        {
            captureDevice.StopCapture();
            captureDevice.Close();
            response?.Dispose();
        }
    }

    private async Task SendNotificationAsync(string? userId, string title, string message)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }
        else
        {
            logger.LogWarning("UserId is null or empty; notification not sent.");
        }
    }
    
    private string GetHealthStatus(double totalTimeMs)
    {
        return totalTimeMs < 300 ? "🌟 Healthy: Blazing Fast! 🌟" : totalTimeMs < 1000 ? "✅ Healthy: Good Response Time" : "⚠️ Warning: Slow Response—Investigate! ⚠️";
    }

    private string GetColorHint(double totalTimeMs)
    {
        return totalTimeMs < 300 ? "green" : totalTimeMs < 1000 ? "yellow" : "red";
    }
}