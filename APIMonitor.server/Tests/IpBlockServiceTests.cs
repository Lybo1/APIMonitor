using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.IpBlockService;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class IpBlockServiceTests
{
    private readonly Mock<IAuditLogService> mockAuditLogService;
        private readonly ApplicationDbContext dbContext;
        private readonly IpBlockService ipBlockService;

        public IpBlockServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySQL("TestDatabase")
                .Options;

            this.dbContext = new ApplicationDbContext(options);
            this.mockAuditLogService = new Mock<IAuditLogService>();

            this.ipBlockService = new IpBlockService(this.dbContext, this.mockAuditLogService.Object);
        }

        [Fact]
        public async Task GetAllBannedIpsAsync_ShouldReturnBannedIps()
        {
            IpBlock ipBlock1 = new()
            {
                Ip = "192.168.0.1",
                BlockedUntil = DateTime.UtcNow.AddHours(1),
                FailedAttempts = 5,
                Reason = "Test reason"
            };

            IpBlock ipBlock2 = new()
            {
                Ip = "192.168.0.2",
                BlockedUntil = DateTime.UtcNow.AddHours(2),
                FailedAttempts = 3,
                Reason = "Another reason"
            };

            this.dbContext.IpBlocks.AddRange(ipBlock1, ipBlock2);
            await this.dbContext.SaveChangesAsync();

            List<IpBlock> result = await this.ipBlockService.GetAllBannedIpsAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, ip => ip.Ip == "192.168.0.1");
            Assert.Contains(result, ip => ip.Ip == "192.168.0.2");
        }

        [Fact]
        public async Task GetBannedIpAsync_ShouldReturnCorrectIpBlock()
        {
            IpBlock ipBlock = new()
            {
                Ip = "192.168.0.1",
                BlockedUntil = DateTime.UtcNow.AddHours(1),
                FailedAttempts = 5,
                Reason = "Test reason"
            };

            this.dbContext.IpBlocks.Add(ipBlock);
            await this.dbContext.SaveChangesAsync();

            IpBlock? result = await this.ipBlockService.GetBannedIpAsync("192.168.0.1");

            Assert.NotNull(result);
            Assert.Equal("192.168.0.1", result?.Ip);
        }

        [Fact]
        public async Task UnblockIpAsync_ShouldUnblockIp()
        {
            IpBlock ipBlock = new()
            {
                Ip = "192.168.0.1",
                BlockedUntil = DateTime.UtcNow.AddHours(1),
                FailedAttempts = 5,
                Reason = "Test reason"
            };

            this.dbContext.IpBlocks.Add(ipBlock);
            await this.dbContext.SaveChangesAsync();

            const int adminUserId = 1;

            bool result = await this.ipBlockService.UnblockIpAsync("192.168.0.1", adminUserId);
            Assert.True(result);
            Assert.Null(await this.dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == "192.168.0.1"));
            
            this.mockAuditLogService.Verify(m => m.LogActionAsync(
                adminUserId,
                "Unblocked IP",
                $"Admin unblocked IP 192.168.0.1",
                It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task BlockIpAsync_ShouldBlockIp()
        {
            const string ipAddress = "192.168.0.1";
            const string reason = "Suspicious activity";
            const int adminUserId = 1;
            TimeSpan blockDuration = TimeSpan.FromHours(2);

            bool result = await this.ipBlockService.BlockIpAsync(ipAddress, blockDuration, reason, adminUserId);
            Assert.True(result);
            
            IpBlock? ipBlock = await this.dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
            Assert.NotNull(ipBlock);
            Assert.Equal(ipAddress, ipBlock?.Ip);
            Assert.Equal(blockDuration, ipBlock?.BlockedUntil - DateTime.UtcNow);
            
            this.mockAuditLogService.Verify(m => m.LogActionAsync(
                adminUserId,
                "Blocked IP",
                $"Admin blocked IP 192.168.0.1 for 2 hours. Reason: Suspicious activity",
                It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task BlockIpAsync_ShouldExtendBlockIfAlreadyBanned()
        {
            const string ipAddress = "192.168.0.1";
            const string reason = "Initial block";
            const int adminUserId = 1;

            IpBlock existingBlock = new()
            {
                Ip = ipAddress,
                BlockedUntil = DateTime.UtcNow.AddHours(1),
                FailedAttempts = 5,
                Reason = "Initial block"
            };

            this.dbContext.IpBlocks.Add(existingBlock);
            await this.dbContext.SaveChangesAsync();

            bool result = await this.ipBlockService.BlockIpAsync(ipAddress, TimeSpan.FromHours(3), reason, adminUserId);
            Assert.True(result);
            
            IpBlock? ipBlock = await this.dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
            Assert.NotNull(ipBlock);
            Assert.Equal(TimeSpan.FromHours(3), ipBlock?.BlockedUntil - DateTime.UtcNow);
            Assert.Equal(reason, ipBlock?.Reason);
            
            this.mockAuditLogService.Verify(m => m.LogActionAsync(
                adminUserId,
                "Blocked IP",
                $"Admin blocked IP 192.168.0.1 for 3 hours. Reason: Initial block",
                It.IsAny<DateTime>()), Times.Once);
        }
    }