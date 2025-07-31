using System.IdentityModel.Tokens.Jwt;
using APIMonitor.server.Identity;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class TokenServiceTests
{
    private readonly Mock<UserManager<User>> mockUserManager;
    private readonly Mock<IHttpContextAccessor> mockHttpContextAccessor;
    private readonly TokenService tokenService;
    private const string ValidKey = "VGhpc0lzQVZhbGlkMzJCeXRlc0Jhc2U2NEtleT09";

    public TokenServiceTests()
    {
        Mock<IUserStore<User>> userStoreMock = new();
        
        this.mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        Mock<IConfiguration> mockConfiguration1 = new();
        
        mockConfiguration1.Setup(c => c["JWT:Key"]).Returns(ValidKey);
        mockConfiguration1.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfiguration1.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        this.mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        this.tokenService = new TokenService(
            this.mockUserManager.Object,
            mockConfiguration1.Object,
            this.mockHttpContextAccessor.Object,
            memoryCache);
    }

    [Fact]
    public async Task GenerateShortLivedAccessToken_ValidUser_ReturnsToken()
    {
        User user = new()
        {
            Id = 1,
            Email = "test@example.com",
            IsAdmin = true,
            RememberMe = false
        };

        this.mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        string token = await this.tokenService.GenerateShortLivedAccessToken(user);
        Assert.NotNull(token);
        
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken? jwtToken = handler.ReadJwtToken(token);
        
        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        User user = new()
        {
            Id = 1,
            Email = "test@example.com",
            IsAdmin = true,
            RememberMe = false
        };

        this.mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);

        string token = await this.tokenService.GenerateShortLivedAccessToken(user);

        bool result = await this.tokenService.ValidateTokenAsync(token);
        Assert.True(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        User user = new()
        {
            Id = 1,
            Email = "test@example.com"
        };

        DefaultHttpContext httpContext = new();
        
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "TestAgent";

        this.mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        this.mockUserManager.Setup(x => x.Users).Returns(new List<User> { user }.AsQueryable());

        string refreshToken = await this.tokenService.GenerateLongLivedRefreshToken(user);

        TokenResponse result = await this.tokenService.RefreshTokenAsync(refreshToken);

        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidUser_TokenRevoked()
    {
        User user = new()
        {
            Id = 1,
            Email = "test@example.com",
            RefreshToken = "oldToken",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        this.mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        await this.tokenService.RevokeRefreshTokenAsync(user);

        Assert.Null(user.RefreshToken);
        Assert.Equal(DateTime.MinValue, user.RefreshTokenExpiry);
        
        this.mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }
}