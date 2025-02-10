using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace APIMonitor.server.Identity.Services.TokenServices;

public class TokenService
{
    private readonly UserManager<User> userManager;
    private readonly IConfiguration configuration;
    private JwtSecurityTokenHandler handler;

    public TokenService(UserManager<User> userManager, IConfiguration configuration, JwtSecurityTokenHandler handler)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }
    
    /*
     * Todo Implement!
     */

    public async Task<string> GenerateToken(User user)
    {
        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("isAdmin", user.IsAdmin.ToString().ToLower()), 
            new Claim("rememberMe", user.RememberMe.ToString().ToLower()) 
        };

        return string.Empty;

    }
}