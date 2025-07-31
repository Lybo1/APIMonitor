using System.Net;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SharpPcap;
using Xunit;

namespace APIMonitor.server.Tests;

public class ApiScannerServiceTests
{
        private readonly Mock<HttpClient> mockHttpClient;
        private readonly Mock<ICaptureDevice> mockCaptureDevice;
        private readonly Mock<IMemoryCache> mockMemoryCache;
        private readonly Mock<ILogger<ApiScannerService>> mockLogger;
        private readonly Mock<IMapper> mockMapper;
        private readonly Mock<IHubContext<NotificationHub>> mockHubContext;
        private readonly ApiScannerService apiScannerService;

        public ApiScannerServiceTests()
        {
            this.mockHttpClient = new Mock<HttpClient>();
            this.mockCaptureDevice = new Mock<ICaptureDevice>();
            this.mockMemoryCache = new Mock<IMemoryCache>();
            this.mockLogger = new Mock<ILogger<ApiScannerService>>();
            this.mockMapper = new Mock<IMapper>();
            this.mockHubContext = new Mock<IHubContext<NotificationHub>>();

            this.apiScannerService = new ApiScannerService(
                this.mockHttpClient.Object,
                this.mockCaptureDevice.Object,
                this.mockMemoryCache.Object,
                this.mockLogger.Object,
                this.mockMapper.Object,
                this.mockHubContext.Object);
        }

        [Fact]
        public async Task ScanSingleApiAsync_ReturnsSuccessfulResult()
        {
            string apiUrl = "https://example.com";
            string method = "GET";
            Mock<HttpResponseMessage> mockResponseMessage = new();
            
            mockResponseMessage.Setup(r => r.StatusCode).Returns(HttpStatusCode.OK);
            mockResponseMessage.Setup(r => r.Content).Returns(new StringContent("Response Body"));

            this.mockHttpClient.Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(mockResponseMessage.Object);

            this.mockMemoryCache.Setup(cache => cache.TryGetValue(It.IsAny<string>(), out It.Ref<ApiScanResult>.IsAny))
                           .Returns(false);

            (ApiMetrics Metrics, HttpStatusCode StatusCode, string ResponseSnippet) result = await this.apiScannerService.ScanSingleApiAsync(apiUrl, method);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("Response Body...", result.ResponseSnippet);
            Assert.NotNull(result.Metrics);
        }

        [Fact]
        public async Task ScanSingleApiAsync_ReturnsCachedResult()
        {
            string apiUrl = "https://example.com";
            
            ApiScanResult? cachedResult = new()
            {
                BodySnippet = "Cached Body"
            };

            this.mockMemoryCache.Setup(cache => cache.TryGetValue(It.IsAny<string>(), out cachedResult))
                           .Returns(true);

            (ApiMetrics Metrics, HttpStatusCode StatusCode, string ResponseSnippet) result = await this.apiScannerService.ScanSingleApiAsync(apiUrl);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("Cached Body", result.ResponseSnippet);
        }

        [Fact]
        public async Task ScanSingleApiAsync_Fails_WithException()
        {
            string apiUrl = "https://example.com";
            HttpRequestException exception = new("Request failed");
            
            this.mockHttpClient.Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                          .ThrowsAsync(exception);

            (ApiMetrics Metrics, HttpStatusCode StatusCode, string ResponseSnippet) result = await this.apiScannerService.ScanSingleApiAsync(apiUrl);

            Assert.Equal(HttpStatusCode.RequestTimeout, result.StatusCode);
            Assert.Contains("Error: Request failed", result.ResponseSnippet);
        }
    }