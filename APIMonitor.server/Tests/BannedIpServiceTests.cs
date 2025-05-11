using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.BannedIpService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class BannedIpServiceTests
{
    private readonly Mock<ApplicationDbContext> dbContextMock;
    private readonly Mock<IMemoryCache> memoryCacheMock;
    private readonly BannedIpService bannedIpService;

    public BannedIpServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL("TestDb")
            .Options;

        this.dbContextMock = new Mock<ApplicationDbContext>(options);
        this.memoryCacheMock = new Mock<IMemoryCache>();

        this.bannedIpService = new BannedIpService(this.dbContextMock.Object, this.memoryCacheMock.Object);
    }

    [Fact]
    public async Task GetBannedIpsAsync_Should_ReturnBannedIps_FromCache_WhenAvailable()
    {
        List<IpBlock>? bannedIps = new()
        {
            new IpBlock
            {
                Ip = "192.168.1.1", BlockedUntil = DateTime.UtcNow.AddMinutes(10)
            }
        };

        this.memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<string>(), out bannedIps)).Returns(true);

        List<IpBlock> result = await this.bannedIpService.GetBannedIpsAsync();

        Assert.Equal(1, result.Count);
        Assert.Equal("192.168.1.1", result[0].Ip);
    }

    [Fact]
    public async Task GetBannedIpsAsync_Should_ReturnBannedIps_FromDb_WhenNotInCache()
    {
        List<IpBlock>? bannedIps = new()
        {
            new IpBlock
            {
                Ip = "192.168.1.1", BlockedUntil = DateTime.UtcNow.AddMinutes(10)
            }
        };

        this.memoryCacheMock.Setup(cache => cache.TryGetValue(It.IsAny<string>(), out bannedIps)).Returns(false);

        this.dbContextMock.Object.IpBlocks.AddRange(bannedIps);
        await this.dbContextMock.Object.SaveChangesAsync();

        List<IpBlock> result = await this.bannedIpService.GetBannedIpsAsync();

        Assert.Single(result);
        Assert.Equal("192.168.1.1", result[0].Ip);
    }

    [Fact]
    public async Task UnbanIpAsync_Should_RemoveIpFromDbAndCache()
    {
        const string ipAddress = "192.168.1.1";
        IpBlock ipBlock = new()
        {
            Ip = ipAddress, BlockedUntil = DateTime.UtcNow.AddMinutes(10)
        };

        this.dbContextMock.Object.IpBlocks.Add(ipBlock);
        await this.dbContextMock.Object.SaveChangesAsync();

        bool result = await this.bannedIpService.UnbanIpAsync(ipAddress);
        Assert.True(result);
        
        IpBlock? dbIpBlock = await this.dbContextMock.Object.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
        Assert.Null(dbIpBlock);

        this.memoryCacheMock.Verify(cache => cache.Remove(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ClearAllBannedIpsAsync_Should_ClearAllIpsFromDbAndCache()
    {
        List<IpBlock> bannedIps = new()
        {
            new IpBlock
            {
                Ip = "192.168.1.1", BlockedUntil = DateTime.UtcNow.AddMinutes(10)
            },
            new IpBlock
            {
                Ip = "192.168.1.2", BlockedUntil = DateTime.UtcNow.AddMinutes(10)
            }
        };

        this.dbContextMock.Object.IpBlocks.AddRange(bannedIps);
        await this.dbContextMock.Object.SaveChangesAsync();

        bool result = await this.bannedIpService.ClearAllBannedIpsAsync();
        Assert.True(result);
        
        List<IpBlock> remainingIps = await this.dbContextMock.Object.IpBlocks.ToListAsync();
        Assert.Empty(remainingIps);

        this.memoryCacheMock.Verify(cache => cache.Remove(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void UnbanExpiredAsync_Should_RemoveExpiredBans()
    {
        IpBlock expiredIpBlock = new()
        {
            Ip = "192.168.1.1",
            BlockedUntil = DateTime.UtcNow.AddMinutes(-1)
        };

        this.dbContextMock.Object.IpBlocks.Add(expiredIpBlock);
        this.dbContextMock.Object.SaveChanges();

        Task.Delay(1000).Wait();

        List<IpBlock> remainingIps = this.dbContextMock.Object.IpBlocks.ToList();
        Assert.Empty(remainingIps);
    }
}