using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace APIMonitor.server.Identity.Services.TokenServices;

public class TokenService
{
    private readonly UserManager<User> userManager;
    private readonly IConfiguration configuration;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly JwtSecurityTokenHandler handler;

    public TokenService(UserManager<User> userManager, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.handler = new JwtSecurityTokenHandler();
    }
    
    public async Task<string> GenerateShortLivedAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        IList<string> roles = await userManager.GetRolesAsync(user);
        
        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("isAdmin", user.IsAdmin.ToString().ToLower()),
            new Claim("rememberMe", user.RememberMe.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        SymmetricSecurityKey key = GetSecurityKey();
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        int expiryMinutes = user.RememberMe ? 43200 : 60;

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"]
        };

        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }
    
    public async Task<string> GenerateLongLivedRefreshToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        string refreshToken = GenerateRefreshToken();
        string hashedRefreshToken = HashToken(refreshToken); 

        user.RefreshToken = hashedRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(60);

        await userManager.UpdateAsync(user);
        
        return refreshToken;
    }

    public void IssueShortLivedAccessToken(string token)
    {
        IssueCookie("access_token", token, 60);
    }
    
    public void IssueLongLivedRefreshToken(string token)
    {
        IssueCookie("refresh_token", token, 43200);
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken, "Refresh token cannot be null or empty.");

        string hashedRefreshToken = HashToken(refreshToken);
        User user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new SecurityTokenException("Invalid or expired refresh token.");
        }

        string newAccessToken = await GenerateShortLivedAccessToken(user);
        string newRefreshToken = await GenerateLongLivedRefreshToken(user);

        IssueShortLivedAccessToken(newAccessToken);
        IssueLongLivedRefreshToken(newRefreshToken);

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
        };
    }

    public async Task RevokeRefreshToken(User user)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiry = DateTime.MinValue;
        await userManager.UpdateAsync(user);
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        return Convert.ToBase64String(randomNumber);
    }

    private static string HashToken(string token)
    {
        using SHA256 sha256 = SHA256.Create();
        
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }

    private void IssueCookie(string name, string token, int expiryMinutes)
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Append(name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SameSite = SameSiteMode.Strict
        });
    }

    private SymmetricSecurityKey GetSecurityKey()
    {
        string jsonKey = configuration["Jwt:Key"] ?? throw new ApplicationException("JWT Key is missing.");
        
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jsonKey));
    }
}
