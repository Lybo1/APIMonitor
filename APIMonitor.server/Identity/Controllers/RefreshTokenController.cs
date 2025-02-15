using APIMonitor.server.Data;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefreshTokenController : ControllerBase
{
    private readonly ITokenService tokenService;
    private readonly IMemoryCache memoryCache;
    private readonly IHttpContextAccessor httpContextAccessor;
    
    public RefreshTokenController(ITokenService tokenService, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor)
    {
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromHeader(Name = "Authorization")] string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Missing refresh token." });
        }
        
        string ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        string userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        string refreshTokenHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken)));

        string failedAttemptsKey = $"failed_refresh_{ipAddress}";

        if (memoryCache.TryGetValue(failedAttemptsKey, out int failedAttempts) && failedAttempts >= Constants.MaxLoginAttempts)
        {
            return Unauthorized(new { message = $"Too many failed attempts. Try again later in {Constants.LockoutMinutes} minutes." });
        }

        try
        {
            if (memoryCache.TryGetValue($"used_refresh_{refreshTokenHash}", out _))
            {
                return Unauthorized(new
                    { message = $"Refresh token has already been used or revoked. Re-login required." });
            }

            TokenResponse newToken = await tokenService.RefreshTokenAsync(refreshToken);

            memoryCache.Set($"used_refresh_{refreshTokenHash}", true, TimeSpan.FromDays(7));
            memoryCache.Set($"refresh_meta_{refreshTokenHash}", new { IP = ipAddress, UserAgent = userAgent },
                TimeSpan.FromDays(7));

            return Ok(newToken);
        }
        catch (SecurityTokenException)
        {
            memoryCache.Set(failedAttemptsKey, failedAttempts + 1, TimeSpan.FromMinutes(Constants.LockoutMinutes));

            return Unauthorized(new { message = "Invalid or expired refresh token. Please re-login." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
        }
    }
}