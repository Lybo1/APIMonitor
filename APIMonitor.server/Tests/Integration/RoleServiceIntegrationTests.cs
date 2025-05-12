using APIMonitor.server.Data;
using APIMonitor.server.Identity;
using APIMonitor.server.Identity.Services.RoleServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APIMonitor.server.Tests.EndToEnd;

public class RoleServiceIntegrationTests : IAsyncLifetime
{
    private readonly ApplicationDbContext dbContext;
    private readonly RoleService roleService;

    public RoleServiceIntegrationTests()
    {
        ServiceCollection services = new();

        services.AddDbContext<ApplicationDbContext>(options => options.UseMySQL(Guid.NewGuid().ToString()));

        services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IRoleService, RoleService>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        
        this.dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        this.roleService = serviceProvider.GetRequiredService<RoleService>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await this.dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task CompleteRoleManagementWorkflow()
    {
        IdentityResult createResult = await this.roleService.CreateRoleAsync("TESTROLE");
        Assert.True(createResult.Succeeded);

        List<IdentityRole<int>> roles = await this.roleService.GetAllRolesAsync();
        IdentityRole<int> role = Assert.Single(roles);
        Assert.Equal("TESTROLE", role.Name);

        IdentityResult deleteResult = await this.roleService.DeleteRoleAsync("TESTROLE");
        Assert.True(deleteResult.Succeeded);

        roles = await this.roleService.GetAllRolesAsync();
        Assert.Empty(roles);
    }
}