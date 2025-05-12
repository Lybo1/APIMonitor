using APIMonitor.server.Identity;
using APIMonitor.server.Identity.Services.RoleServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace APIMonitor.server.Tests;

public class RoleServiceTests
{
    private readonly Mock<UserManager<User>> mockUserManager;
    private readonly Mock<RoleManager<IdentityRole<int>>> mockRoleManager;
    private readonly RoleService roleService;

    public RoleServiceTests()
    {
        Mock<IUserStore<User>> userStoreMock = new();
        
        this.mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object,
            null, null, null, null, null, null, null, null);

        Mock<IRoleStore<IdentityRole<int>>> roleStoreMock = new();
        
        this.mockRoleManager = new Mock<RoleManager<IdentityRole<int>>>(
            roleStoreMock.Object, null, null, null, null);

        this.roleService = new RoleService(this.mockUserManager.Object, this.mockRoleManager.Object);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidRole_ReturnsSuccess()
    {
        const string roleName = "TESTROLE";
        
        this.mockRoleManager.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync((IdentityRole<int>)null);
        this.mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<int>>())).ReturnsAsync(IdentityResult.Success);

        IdentityResult result = await this.roleService.CreateRoleAsync(roleName);
        Assert.True(result.Succeeded);
        
        this.mockRoleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole<int>>(r => r.Name == roleName)), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_WithExistingRole_ReturnsFailed()
    {
        const string roleName = "TESTROLE";
        IdentityRole<int> existingRole = new(roleName);
        
        this.mockRoleManager.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync(existingRole);

        IdentityResult result = await this.roleService.CreateRoleAsync(roleName);
        Assert.False(result.Succeeded);
        Assert.Contains($"Role {roleName} already exists", result.Errors.First().Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateRoleAsync_WithInvalidRoleName_ThrowsArgumentException(string roleName)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => this.roleService.CreateRoleAsync(roleName));
    }

    [Fact]
    public async Task DeleteRoleAsync_WithValidUnusedRole_ReturnsSuccess()
    {
        const string roleName = "TESTROLE";
        IdentityRole<int> existingRole = new(roleName);
        
        this.mockRoleManager.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync(existingRole);
        this.mockUserManager.Setup(x => x.GetUsersInRoleAsync(roleName)).ReturnsAsync(new List<User>());
        this.mockRoleManager.Setup(x => x.DeleteAsync(existingRole)).ReturnsAsync(IdentityResult.Success);

        IdentityResult result = await this.roleService.DeleteRoleAsync(roleName);
        Assert.True(result.Succeeded);
        
        this.mockRoleManager.Verify(x => x.DeleteAsync(existingRole), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithNonexistentRole_ReturnsFailed()
    {
        const string roleName = "NONEXISTENT";
        
        this.mockRoleManager.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync((IdentityRole<int>)null);

        IdentityResult result = await this.roleService.DeleteRoleAsync(roleName);
        Assert.False(result.Succeeded);
        Assert.Contains($"Role {roleName} does not exist", result.Errors.First().Description);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithUsedRole_ReturnsFailed()
    {
        const string roleName = "USEDROLE";
        IdentityRole<int> existingRole = new(roleName);
        List<User> usersInRole = new()
        {
            new()
        };
        
        this.mockRoleManager.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync(existingRole);
        this.mockUserManager.Setup(x => x.GetUsersInRoleAsync(roleName)).ReturnsAsync(usersInRole);

        IdentityResult result = await this.roleService.DeleteRoleAsync(roleName);
        Assert.False(result.Succeeded);
        Assert.Contains($"Role {roleName} is in use", result.Errors.First().Description);
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRoles()
    {
        List<IdentityRole<int>> roles = new()
        {
            new("ROLE1"),
            new("ROLE2")
        };
        
        Mock<DbSet<IdentityRole<int>>> mockDbSet = roles.AsQueryable().BuildMockDbSet();
        this.mockRoleManager.Setup(x => x.Roles).Returns(mockDbSet.Object);

        List<IdentityRole<int>> result = await this.roleService.GetAllRolesAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "ROLE1");
        Assert.Contains(result, r => r.Name == "ROLE2");
    }
}