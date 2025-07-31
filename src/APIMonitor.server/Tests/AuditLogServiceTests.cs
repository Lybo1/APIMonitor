using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.AuditLogService;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class AuditLogServiceTests
{
    private readonly Mock<IHttpContextAccessor> httpContextAccessorMock;
    private readonly Mock<ApplicationDbContext> dbContextMock;
    private readonly AuditLogService auditLogService;

    public AuditLogServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("TestDb")
            .Options;

        this.dbContextMock = new Mock<ApplicationDbContext>(options);
        this.httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        this.auditLogService = new AuditLogService(this.dbContextMock.Object, this.httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task LogActionAsync_Should_LogActionCorrectly()
    {
        const int userId = 1;
        const string action = "Create";
        const string details = "Created a new item";
        DateTime requestStartTime = DateTime.UtcNow.AddMinutes(-2);

        Mock<HttpContext> contextMock = new();
        
        contextMock.Setup(ctx => ctx.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.1"));
        contextMock.Setup(ctx => ctx.Request.Headers["User-Agent"]).Returns("Mozilla/5.0");

        this.httpContextAccessorMock.Setup(h => h.HttpContext).Returns(contextMock.Object);

        await this.auditLogService.LogActionAsync(userId, action, details, requestStartTime);

        List<AuditLog> auditLogs = await this.dbContextMock.Object.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);
        
        AuditLog log = auditLogs.First();
        
        Assert.Equal(userId, log.UserId);
        Assert.Equal(action, log.Action);
        Assert.Equal(details, log.Details);
        Assert.Equal("192.168.1.1", log.Ipv4Address);
        Assert.Equal("Mozilla/5.0", log.UserAgent);
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_Should_ReturnUserAuditLogs()
    {
        const int userId = 1;
        
        this.dbContextMock.Object.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Create",
            Details = "Created a new item",
            Date = DateTime.UtcNow
        });
        
        await this.dbContextMock.Object.SaveChangesAsync();
        
        List<AuditLog> logs = await this.auditLogService.GetUserAuditLogsAsync(userId);

        Assert.Single(logs);
        Assert.Equal(userId, logs.First().UserId);
    }

    [Fact]
    public async Task GetAuditLogByIdAsync_Should_ReturnLog_When_ValidId()
    {
        const int logId = 1;
        const int userId = 1;
        
        this.dbContextMock.Object.AuditLogs.Add(new AuditLog
        {
            Id = logId,
            UserId = userId,
            Action = "Create",
            Details = "Created a new item",
            Date = DateTime.UtcNow
        });
        
        await this.dbContextMock.Object.SaveChangesAsync();

        AuditLog? log = await this.auditLogService.GetAuditLogByIdAsync(logId, userId);

        Assert.NotNull(log);
        Assert.Equal(logId, log?.Id);
    }

    [Fact]
    public async Task DeleteAuditLogAsync_Should_RemoveLog_When_ValidId()
    {
        const int logId = 1;
        
        this.dbContextMock.Object.AuditLogs.Add(new AuditLog
        {
            Id = logId,
            UserId = 1,
            Action = "Create",
            Details = "Created a new item",
            Date = DateTime.UtcNow
        });
        
        await this.dbContextMock.Object.SaveChangesAsync();

        bool result = await this.auditLogService.DeleteAuditLogAsync(logId);
        Assert.True(result);
        
        AuditLog? log = await this.dbContextMock.Object.AuditLogs.FindAsync(logId);
        Assert.Null(log);
    }

    [Fact]
    public async Task PurgeAuditLogsAsync_Should_ClearAllLogs()
    {
        this.dbContextMock.Object.AuditLogs.Add(new AuditLog
        {
            UserId = 1,
            Action = "Create",
            Details = "Created a new item",
            Date = DateTime.UtcNow
        });
        
        await this.dbContextMock.Object.SaveChangesAsync();

        bool result = await this.auditLogService.PurgeAuditLogsAsync();
        Assert.True(result);
        
        List<AuditLog> logs = await this.dbContextMock.Object.AuditLogs.ToListAsync();
        Assert.Empty(logs);
    }
}