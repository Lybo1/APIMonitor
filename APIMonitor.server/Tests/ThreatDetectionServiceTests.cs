using System.Net;
using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ThreatDetectionService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class ThreatDetectionServiceTests
{
    private readonly Mock<ApplicationDbContext> mockDbContext;
    private readonly Mock<IHttpContextAccessor> mockHttpContextAccessor;
    private readonly Mock<IMemoryCache> mockMemoryCache;
    private readonly Mock<IHubContext<NotificationHub>> mockHubContext;
    private readonly Mock<HttpContext> mockHttpContext;
    private readonly ThreatDetectionService threatDetectionService;

    public ThreatDetectionServiceTests()
    {
        this.mockDbContext = new Mock<ApplicationDbContext>();
        this.mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        this.mockMemoryCache = new Mock<IMemoryCache>();
        this.mockHubContext = new Mock<IHubContext<NotificationHub>>();
        this.mockHttpContext = new Mock<HttpContext>();

        SetupHttpContext();

        this.threatDetectionService = new ThreatDetectionService(
            this.mockDbContext.Object,
            this.mockHttpContextAccessor.Object,
            this.mockMemoryCache.Object,
            this.mockHubContext.Object
        );
    }

    [Fact]
    public async Task IsIpBlocked_WhenHttpContextIsNull_ThrowsInvalidOperationException()
    {
        this.mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => this.threatDetectionService.IsIpBlocked());
    }

    [Fact]
    public async Task IsIpBlocked_WhenIpAddressIsNull_ReturnsFalse()
    {
        Mock<ConnectionInfo> mockConnection = new();
        
        mockConnection.Setup(x => x.RemoteIpAddress).Returns((IPAddress?)null);
        this.mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);

        bool result = await this.threatDetectionService.IsIpBlocked();
        Assert.False(result);
    }

    [Fact]
    public async Task IsIpBlocked_WhenIpIsBlockedInCache_ReturnsTrue()
    {
        object? cachedValue = true;
        
        this.mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedValue)).Returns(true);

        bool result = await this.threatDetectionService.IsIpBlocked();
        Assert.True(result);
    }

    [Fact]
    public async Task LogFailedAttempt_WhenAttemptsExceedLimit_BansIp()
    {
        const string testAction = "login";
        const string testReason = "Invalid credentials";
        
        Mock<IClientProxy> mockClientProxy = new();
        Mock<IHubClients> mockHubClients = new();
        
        mockHubClients.Setup(x => x.All).Returns(mockClientProxy.Object);
        this.mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);

        for (int i = 0; i <= Constants.MaxLoginAttempts; i++)
        {
            await this.threatDetectionService.LogFailedAttemptAsync(testAction, testReason, AlertType.SuspiciousActivity);
        }

        this.mockDbContext.Verify(
            x => x.IpBlocks.AddAsync(
                It.IsAny<IpBlock>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task BanIpAsync_SetsMemoryCacheAndSavesToDatabase()
    {
        const string testIp = "192.168.1.1";
        const string testReason = "Test ban reason";
        
        Mock<IClientProxy> mockClientProxy = new();
        Mock<IHubClients> mockHubClients = new();
        
        mockHubClients.Setup(x => x.All).Returns(mockClientProxy.Object);
        this.mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);

        await this.threatDetectionService.BanIpAsync(testIp, testReason);

        this.mockMemoryCache.Verify(
            x => x.Set(
                It.Is<string>(key => key == $"Blocked:{testIp}"),
                It.IsAny<object>(),
                It.IsAny<TimeSpan>()
            ),
            Times.Once
        );
        
        this.mockDbContext.Verify(
            x => x.IpBlocks.AddAsync(
                It.Is<IpBlock>(block => 
                    block.Ip == testIp && 
                    block.Reason == testReason
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task LogThreatAsync_AddsAlertToDatabase()
    {
        const string testIp = "192.168.1.1";
        const string testDescription = "Test threat";
        const AlertType testAlertType = AlertType.DDoS;
        const AlertSeverity testSeverity = AlertSeverity.High;

        await this.threatDetectionService.LogThreatAsync(testIp, testAlertType, testDescription, testSeverity);

        this.mockDbContext.Verify(
            x => x.ThreatAlerts.AddAsync(
                It.Is<ThreatAlert>(alert => 
                    alert.IpAddress == testIp &&
                    alert.AlertType == testAlertType &&
                    alert.Severity == testSeverity &&
                    alert.Description.Contains(testDescription)
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    private void SetupHttpContext()
    {
        Mock<ConnectionInfo> mockConnection = new();
        
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("127.0.0.1"));
        this.mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);

        ClaimsIdentity identity = new(new List<Claim> { new(ClaimTypes.Name, "TestAdmin") });
        ClaimsPrincipal principal = new(identity);
        
        this.mockHttpContext.Setup(x => x.User).Returns(principal);
        this.mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(this.mockHttpContext.Object);
    }
}