using APIMonitor.server.Models;

namespace APIMonitor.server.Identity.Services.TokenServices;

public interface ITokenService
{
    Task<string> GenerateShortLivedAccessToken(User user);
    Task<string> GenerateLongLivedRefreshToken(User user);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(User user);
}