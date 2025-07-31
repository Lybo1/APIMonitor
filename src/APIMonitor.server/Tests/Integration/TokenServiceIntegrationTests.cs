using APIMonitor.server.Data;
using APIMonitor.server.Identity;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

namespace APIMonitor.server.Tests.Integration;

public class TokenServiceIntegrationTests : IAsyncLifetime
{
    private readonly ApplicationDbContext dbContext;
    private readonly TokenService tokenService;
    private readonly UserManager<User> userManager;

    public TokenServiceIntegrationTests()
    {
        ServiceCollection services = new();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["JWT:Key"] = "VGhpc0lzQVZhbGlkMzJCeXRlc0Jhc2U2NEtleT09",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddDbContext<ApplicationDbContext>(options => options.UseMySQL(Guid.NewGuid().ToString()));

        services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ITokenService, TokenService>();

        IServiceProvider serviceProvider1 = services.BuildServiceProvider();
        
        this.dbContext = serviceProvider1.GetRequiredService<ApplicationDbContext>();
        this.tokenService = serviceProvider1.GetRequiredService<TokenService>();
        this.userManager = serviceProvider1.GetRequiredService<UserManager<User>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await this.dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task CompleteTokenLifecycle()
    {
        User user = new()
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        await this.userManager.CreateAsync(user, "Password123!");

        string accessToken = await this.tokenService.GenerateShortLivedAccessToken(user);
        Assert.NotNull(accessToken);

        bool isValid = await this.tokenService.ValidateTokenAsync(accessToken);
        Assert.True(isValid);

        string refreshToken = await this.tokenService.GenerateLongLivedRefreshToken(user);
        Assert.NotNull(refreshToken);

        TokenResponse newTokens = await this.tokenService.RefreshTokenAsync(refreshToken);
        Assert.NotNull(newTokens.AccessToken);
        Assert.NotNull(newTokens.RefreshToken);

        await this.tokenService.RevokeRefreshTokenAsync(user);
        
        User? updatedUser = await this.userManager.FindByIdAsync(user.Id.ToString());
        Assert.Null(updatedUser?.RefreshToken);
    }
}