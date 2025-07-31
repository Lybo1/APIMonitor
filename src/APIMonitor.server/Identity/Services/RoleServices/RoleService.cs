using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Identity.Services.RoleServices;

public class RoleService : IRoleService
{
    private readonly UserManager<User> userManager;
    private readonly RoleManager<IdentityRole<int>> roleManager;

    public RoleService(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
    }

    public async Task<IdentityResult> CreateRoleAsync(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName, "Role name cannot be empty.");
        
        roleName = roleName.Trim().ToUpperInvariant();
        
        IdentityRole<int>? role = await roleManager.FindByNameAsync(roleName);
        
        if (role != null)
        {
            return IdentityResult.Failed(new IdentityError { Description = $"Role {roleName} already exists." });
        }
        
        IdentityRole<int> newRole = new(roleName);
        IdentityResult result = await roleManager.CreateAsync(newRole);
        
        string errors = string.Join(',', result.Errors.Select(e => e.Description));
        
        return result.Succeeded ? IdentityResult.Success : IdentityResult.Failed( new IdentityError { Description = $"Unable to create role {roleName}. Error: {errors}." } );
    }

    public async Task<IdentityResult> DeleteRoleAsync(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName, "Role name cannot be empty or whitespace.");
        
        roleName = roleName.Trim().ToUpperInvariant();
        
        IdentityRole<int>? role = await roleManager.FindByNameAsync(roleName);

        if (role == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = $"Role {roleName} does not exist." });
        }

        IList<User> usersInRole = await userManager.GetUsersInRoleAsync(roleName);

        if (usersInRole.Any())
        {
            return IdentityResult.Failed(new IdentityError { Description = $"Role {roleName} is in use. Remove users before deleting." });
        }
        
        IdentityResult result = await roleManager.DeleteAsync(role);
        
        string errors = string.Join(',', result.Errors.Select(e => e.Description));
        
        return result.Succeeded ? IdentityResult.Success : IdentityResult.Failed( new IdentityError { Description = $"Failed to delete role {roleName}. Errors: {errors}." } );

    }

    public async Task<List<IdentityRole<int>>> GetAllRolesAsync()
    {
        return await roleManager.Roles.ToListAsync();
    }
}