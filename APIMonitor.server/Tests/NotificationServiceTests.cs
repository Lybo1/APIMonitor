using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.NotificationsService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class NotificationServiceTests
{
     private readonly Mock<IHubContext<NotificationHub>> mockHubContext;
        private readonly Mock<IMemoryCache> mockCache;
        private readonly ApplicationDbContext dbContext;
        private readonly NotificationService notificationService;

        public NotificationServiceTests()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySQL("TestDatabase")
                .Options;

            this.dbContext = new ApplicationDbContext(options);
            this.mockHubContext = new Mock<IHubContext<NotificationHub>>();
            this.mockCache = new Mock<IMemoryCache>();

            this.notificationService = new NotificationService(this.dbContext, this.mockHubContext.Object, this.mockCache.Object);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_ShouldReturnNotificationsForUser()
        {
           const int userId = 1;
           
           Notification notification1 = new()
            {
                UserId = userId,
                Title = "Test Notification 1",
                Message = "Message 1",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
           
            Notification notification2 = new()
            {
                UserId = userId,
                Title = "Test Notification 2",
                Message = "Message 2",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            this.dbContext.Notifications.Add(notification1);
            this.dbContext.Notifications.Add(notification2);
            await this.dbContext.SaveChangesAsync();

            List<Notification> result = await this.notificationService.GetUserNotificationsAsync(userId.ToString());

            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Title == "Test Notification 1");
            Assert.Contains(result, n => n.Title == "Test Notification 2");
        }

        [Fact]
        public async Task MarkAsRead_ShouldMarkNotificationAsRead()
        {
            Notification notification = new()
            {
                UserId = 1,
                Title = "Test Notification",
                Message = "Message",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            
            this.dbContext.Notifications.Add(notification);
            await this.dbContext.SaveChangesAsync();

            await this.notificationService.MarkAsRead(notification.Id);
            
            Notification? updatedNotification = await this.dbContext.Notifications.FindAsync(notification.Id);
            Assert.True(updatedNotification?.IsRead);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldSendNotificationAndSaveToDatabase()
        {
            const string userId = "1";
            const string title = "Test Notification";
            const string message = "This is a test message.";
            DefaultHttpContext context = new();
            
            context.Items["UserIP"] = "127.0.0.1";
            context.Items["UserAgent"] = "Mozilla/5.0";
            
            bool isCritical = false;

            this.dbContext.TrustedDevices.Add(new TrustedDevice { UserId = 1, IpAddress = "127.0.0.1", UserAgent = "Mozilla/5.0" });
            await this.dbContext.SaveChangesAsync();

            bool result = await this.notificationService.SendNotificationAsync(userId, title, message, context, isCritical);
            Assert.True(result);
            
            Notification? notification = await this.dbContext.Notifications.FirstOrDefaultAsync(n => n.UserId == 1);
            
            Assert.NotNull(notification);
            Assert.Equal(title, notification?.Title);
            Assert.Equal(message, notification?.Message);
            
            this.mockHubContext.Verify(h => h.Clients.Group(userId).SendAsync("ReceiveNotification", title, message, Times.Once);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldNotSendIfNotTrustedDevice()
        {
            const string userId = "1";
            const string title = "Test Notification";
            const string message = "This is a test message.";
            DefaultHttpContext context = new();
            
            context.Items["UserIP"] = "127.0.0.1";
            context.Items["UserAgent"] = "Mozilla/5.0";

            bool result = await this.notificationService.SendNotificationAsync(userId, title, message, context);

            Assert.False(result);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldNotSendIfRateLimited()
        {
            const string userId = "1";
            const string title = "Test Notification";
            const string message = "This is a test message.";
            DefaultHttpContext context = new();
            
            context.Items["UserIP"] = "127.0.0.1";
            context.Items["UserAgent"] = "Mozilla/5.0";

            bool result = await this.notificationService.SendNotificationAsync(userId, title, message, context);
            Assert.False(result);
        }
    }