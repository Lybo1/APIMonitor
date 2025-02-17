using Microsoft.AspNetCore.Identity;

namespace APIMonitor.server.Identity.Services.RoleServices;

public interface IRoleService
{
    Task<IdentityResult> CreateRoleAsync(string roleName);
    Task<IdentityResult> DeleteRoleAsync(string roleName);
    Task<List<IdentityRole<int>>> GetAllRolesAsync();
}