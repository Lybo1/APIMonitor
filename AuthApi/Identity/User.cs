using Microsoft.AspNetCore.Identity;

namespace AuthApi.Identity;

public class User : IdentityUser<int>
{
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; } = false;
    public bool RememberMe { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

}