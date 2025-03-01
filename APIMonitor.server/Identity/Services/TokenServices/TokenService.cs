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

    private readonly byte[] encryptionKey;

    public TokenService(UserManager<User> userManager,
                        IConfiguration configuration,
                        IHttpContextAccessor httpContextAccessor,
                        IMemoryCache memoryCache)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.handler = new JwtSecurityTokenHandler();

        string? key = configuration["JWT:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ApplicationException("Encryption key is missing or invalid.");
        }

        encryptionKey = Convert.FromBase64String(key);
        if (encryptionKey.Length != 16 && encryptionKey.Length != 24 && encryptionKey.Length != 32)
        {
            throw new ApplicationException("Invalid encryption key size. Must be 16, 24, or 32 bytes.");
        }

        Console.WriteLine($"Encryption Key Length: {encryptionKey.Length}");
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            SymmetricSecurityKey key = GetSecurityKey();

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return false;
            }

            string? jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti) || !memoryCache.TryGetValue($"jti_{jti}", out bool isValid) || !isValid)
            {
                return false;
            }

            string? userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                User? user = await userManager.FindByIdAsync(userId);
                return user is not null && !user.IsLockedOut;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GenerateShortLivedAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        Console.WriteLine($"Generating Access Token for: {user.Email}");

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

        Console.WriteLine($"Generated JTI: {jti}");
        return token;
    }

    public async Task<string> GenerateLongLivedRefreshToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        Console.WriteLine($"Generating Refresh Token for: {user.Email}");

        string refreshToken = GenerateSecureToken();
        string hashedRefreshToken = EncryptRefreshToken(refreshToken);

        user.RefreshToken = hashedRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(Constants.RefreshTokenExpirationDays);

        await userManager.UpdateAsync(user);

        return refreshToken;
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, "Invalid refresh token.");

        Console.WriteLine($"Attempting to refresh token: {refreshToken}");

        string? ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        string? userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

        if (ipAddress is null || userAgent is null)
        {
            throw new SecurityTokenException("Could not verify client identity.");
        }

        string refreshTokenHash = EncryptRefreshToken(refreshToken);
        User? user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenHash);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            Console.WriteLine("Invalid or expired refresh token.");
            throw new SecurityTokenException("Invalid or expired refresh token.");
        }

        if (memoryCache.TryGetValue($"refresh_meta_{refreshTokenHash}", out dynamic? storedMeta))
        {
            string storedIp = storedMeta!.IP;
            string storedUserAgent = storedMeta.UserAgent;

            if (storedIp != ipAddress || storedUserAgent != userAgent)
            {
                await RevokeRefreshTokenAsync(user);
                Console.WriteLine("Suspicious login detected for user: " + user.Email);
                throw new SecurityTokenException("Suspicious login detected. Please re-authenticate.");
            }
        }

        string newAccessToken = await GenerateShortLivedAccessToken(user);
        string newRefreshToken = await GenerateLongLivedRefreshToken(user);

        memoryCache.Set($"refresh_meta_{EncryptRefreshToken(newRefreshToken)}", new { IP = ipAddress, UserAgent = userAgent }, TimeSpan.FromDays(Constants.RefreshTokenExpirationDays));

        return new TokenResponse { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
    }

    public async Task RevokeRefreshTokenAsync(User user)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiry = DateTime.MinValue;
        await userManager.UpdateAsync(user);
    }

    public void IssueShortLivedAccessToken(string accessToken)
    {
        ArgumentNullException.ThrowIfNull(accessToken, nameof(accessToken));

        httpContextAccessor.HttpContext?.Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(Constants.DefaultAccessTokenExpirationMinutes)
        });

        Console.WriteLine("Issued access token cookie.");
    }

    public void IssueLongLivedRefreshToken(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken, nameof(refreshToken));

        httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(Constants.RefreshTokenExpirationDays)
        });

        Console.WriteLine("Issued refresh token cookie.");
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
        aes.Key = encryptionKey;
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using MemoryStream ms = new();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
        using StreamWriter sw = new(cs);
        sw.Write(token);
        sw.Flush();
        cs.FlushFinalBlock();

        return Convert.ToBase64String(ms.ToArray());
    }

    private string DecryptRefreshToken(string encryptedToken)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedToken);

        using Aes aes = Aes.Create();
        aes.Key = encryptionKey;
        aes.IV = fullCipher.Take(16).ToArray();

        using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using MemoryStream ms = new(fullCipher, 16, fullCipher.Length - 16);
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new(cs);
        return sr.ReadToEnd();
    }

    private SymmetricSecurityKey GetSecurityKey()
    {
        string jsonKey = configuration["Jwt:Key"] ?? throw new ApplicationException("JWT Key is missing.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jsonKey));
    }
}