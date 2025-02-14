using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace APIMonitor.server.Identity.Services.TokenServices;

public class TokenService : ITokenService
{
    private readonly UserManager<User> userManager;
    private readonly IConfiguration configuration;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMemoryCache memoryCache;
    private readonly JwtSecurityTokenHandler handler;

    public TokenService(UserManager<User> userManager, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.handler = new JwtSecurityTokenHandler();
    }
    
    public async Task<string> GenerateShortLivedAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        IList<string> roles = await userManager.GetRolesAsync(user);
        
        string jti = Guid.NewGuid().ToString();
        
        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("isAdmin", user.IsAdmin.ToString().ToLower()),
            new Claim("rememberMe", user.RememberMe.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        SymmetricSecurityKey key = GetSecurityKey();
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(Constants.DefaultAccessTokenExpirationMinutes),
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"]
        };
        
        string token = handler.WriteToken(handler.CreateToken(tokenDescriptor));

        memoryCache.Set($"jti_{jti}", true, TimeSpan.FromMinutes(Constants.DefaultAccessTokenExpirationMinutes));

        return token;
    }
    
    public async Task<string> GenerateLongLivedRefreshToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        string refreshToken = GenerateSecureToken();
        string hashedRefreshToken = EncryptRefreshToken(refreshToken); 

        user.RefreshToken = hashedRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(Constants.RefreshTokenExpirationDays);

        await userManager.UpdateAsync(user);
        
        return refreshToken;
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new SecurityTokenException("Invalid refresh token");
        }

        User user = (await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == EncryptRefreshToken(refreshToken)))!;

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new SecurityTokenException("Invalid refresh token");
        }
        
        string newAccessToken = await GenerateShortLivedAccessToken(user);
        string newRefreshToken = await GenerateLongLivedRefreshToken(user);

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
        };
    }
    
    public async Task RevokeRefreshTokenAsync(User user)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiry = DateTime.MinValue;
        
        await userManager.UpdateAsync(user);
    }

    private static string GenerateSecureToken()
    {
        byte[] randomNumber = new byte[32];
        
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        return Convert.ToBase64String(randomNumber);
    }

    private string EncryptRefreshToken(string token)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
        aes.IV = new byte[16];
        
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);
        
        return Convert.ToBase64String(encryptedBytes);
    }

    private SymmetricSecurityKey GetSecurityKey()
    {
        string jsonKey = configuration["Jwt:Key"] ?? throw new ApplicationException("JWT Key is missing.");
        
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jsonKey));
    }
}
