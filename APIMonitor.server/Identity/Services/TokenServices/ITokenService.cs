using APIMonitor.server.Models;
using Microsoft.IdentityModel.Tokens;

namespace APIMonitor.server.Identity.Services.TokenServices;

public interface ITokenService
{
    Task<string> GenerateShortLivedAccessToken(User user);
    Task<string> GenerateLongLivedRefreshToken(User user);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeRefreshToken(User user);
    SymmetricSecurityKey GetSecurityKey();
    void IssueCookie(string name, string token, int expiryMinutes);
    void IssueShortLivedAccessToken(string token);
    void IssueLongLivedRefreshToken(string token);
}