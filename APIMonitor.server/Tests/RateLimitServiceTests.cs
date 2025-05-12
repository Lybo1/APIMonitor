using System.Linq.Expressions;
using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.RateLimitService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using static System.Threading.Tasks.ValueTask;

namespace APIMonitor.server.Tests;

public class RateLimitServiceTests
{
    private readonly Mock<ApplicationDbContext> mockDbContext;
    private readonly Mock<IMemoryCache> mockMemoryCache;
    private readonly Mock<HttpContext> mockHttpContext;
    private readonly RateLimitService rateLimitService;
    
    public RateLimitServiceTests()
    {
        this.mockDbContext = new Mock<ApplicationDbContext>();
        this.mockMemoryCache = new Mock<IMemoryCache>();
        this.mockHttpContext = new Mock<HttpContext>();
        
        this.rateLimitService = new RateLimitService(mockDbContext.Object, mockMemoryCache.Object);
        
        Mock<ConnectionInfo> mockConnection = new();
        
        mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        this.mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, "testUser")
        };
        
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal claimsPrincipal = new(identity);
        
        this.mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);
    }

    [Fact]
    public async Task IsRateLimitedAsync_WhenNoRuleExists_ReturnsFalse()
    {
        Mock<DbSet<RateLimitRule>> mockRateLimitRules = new();
        this.mockDbContext.Setup(db => db.RateLimitRules).Returns(mockRateLimitRules.Object);

        bool result = await this.rateLimitService.IsRateLimitedAsync(this.mockHttpContext.Object, "testAction");
        Assert.False(result);
    }

    [Fact]
    public async Task IsRateLimitedAsync_WhenUnderLimit_ReturnsFalse()
    {
        RateLimitRule rule = new()
        {
            Action = "testAction", MaxRequests = 5, TimeWindow = TimeSpan.FromMinutes(1), IsActive = true
        };
        
        DbSet<RateLimitRule> mockRateLimitRules = MockDbSet([rule]);
        this.mockDbContext.Setup(db => db.RateLimitRules).Returns(mockRateLimitRules);

        object cacheValue = 1;
        
        this.mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out cacheValue)).Returns(true);

        bool result = await this.rateLimitService.IsRateLimitedAsync(this.mockHttpContext.Object, "testAction");
        Assert.False(result);
    }

    [Fact]
    public async Task IsRateLimitedAsync_WhenOverLimit_ReturnsTrue()
    {
        RateLimitRule rule = new()
        {
            Action = "testAction", MaxRequests = 5, TimeWindow = TimeSpan.FromMinutes(1), IsActive = true
        };
        
        DbSet<RateLimitRule> mockRateLimitRules = MockDbSet([rule]);
        this.mockDbContext.Setup(db => db.RateLimitRules).Returns(mockRateLimitRules);

        object cacheValue = 5;
        
        this.mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out cacheValue)).Returns(true);

        this.mockDbContext.Setup(db => db.RateLimitViolations.AddAsync(It.IsAny<RateLimitViolation>(), CancellationToken.None)).Returns(FromResult<EntityEntry<RateLimitViolation>>(null!));

        bool result = await this.rateLimitService.IsRateLimitedAsync(mockHttpContext.Object, "testAction");
        Assert.True(result);
        
        this.mockDbContext.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task IsRateLimitedAsync_WhenExcessiveViolations_AutoBansIp()
    {
        RateLimitRule rule = new()
        {
            Action = "testAction", MaxRequests = 5, TimeWindow = TimeSpan.FromMinutes(1), IsActive = true
        };
        
        DbSet<RateLimitRule> mockRateLimitRules = MockDbSet([rule]);
        this.mockDbContext.Setup(db => db.RateLimitRules).Returns(mockRateLimitRules);

        object cacheValue = 5;
        
        this.mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out cacheValue!)).Returns(true);

        Mock<DbSet<BannedIp>> mockBannedIps = new();
        
        this.mockDbContext.Setup(db => db.BannedIps).Returns(mockBannedIps.Object);
        this.mockDbContext.Setup(db => db.BannedIps.AnyAsync(It.IsAny<Expression<Func<BannedIp, bool>>>(), CancellationToken.None)).ReturnsAsync(false);

        for (int i = 0; i <= Constants.MaxLoginAttempts; i++)
        {
            await this.rateLimitService.IsRateLimitedAsync(mockHttpContext.Object, "testAction");
        }

        this.mockDbContext.Verify(db => db.BannedIps.AddAsync(It.IsAny<BannedIp>(), default), Times.Once);
    }

    private static DbSet<T> MockDbSet<T>(List<T> data) where T : class
    {
        IQueryable<T> queryable = data.AsQueryable();
        Mock<DbSet<T>> mockSet = new();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        
        using IEnumerator<T> enumerator = queryable.GetEnumerator();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(enumerator);
        
        return mockSet.Object;
    }
}
