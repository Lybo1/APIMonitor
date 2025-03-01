using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signInManager;
    private readonly ITokenService tokenService;
    private readonly IAuditLogService auditLogService;
    private readonly INotificationService notificationService;

    public LoginController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService, IAuditLogService auditLogService, INotificationService notificationService)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        DateTime startTime = DateTime.UtcNow;
        
        if (!ModelState.IsValid)
        {
            await auditLogService.LogActionAsync(0, "LoginFailed", $"Failed login attempt for {model.Email} - User not found.", startTime);
            return BadRequest(new { message = "Invalid login request." });
        }
        
        User? user = await userManager.FindByEmailAsync(model.Email);

        if (user is null)
        {
            await auditLogService.LogActionAsync(user.Id, "LoginFailed", $"Failed login attempt for {model.Email}.", startTime);
            return Unauthorized(new { message = "Username or password is incorrect." });
        }
        
        SignInResult result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);

        if (result.IsLockedOut)
        {
            return Unauthorized(new { message = "Account is locked." });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Username or password is incorrect." });
        }
        
        await auditLogService.LogActionAsync(user.Id, "Login", $"User {model.Email} logged in.", startTime);
        await notificationService.SendNotificationAsync(user.Id.ToString(), "Login", "You have successfully logged in.", HttpContext);

        if (model.RememberMe)
        {
            string refreshToken = await tokenService.GenerateLongLivedRefreshToken(user);
            tokenService.IssueLongLivedRefreshToken(refreshToken);

            return Ok(new
            {
                message = "Login successful.",
                RefreshToken = refreshToken,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.FirstName,
                    user.LastName,
                    user.CreatedAt,
                    user.IsAdmin,
                    Roles = await userManager.GetRolesAsync(user)
                }
            });
        }
        
        string accessToken = await tokenService.GenerateShortLivedAccessToken(user);
        tokenService.IssueShortLivedAccessToken(accessToken);

        return Ok(new
        {
            message = "Login successful.",
            AccessToken = accessToken,
            User = new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.CreatedAt,
                user.IsAdmin,
                Roles = await userManager.GetRolesAsync(user)
            }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        User? user = await userManager.GetUserAsync(User);

        if (user is not null)
        {
            await tokenService.RevokeRefreshTokenAsync(user);
        }
        
        await signInManager.SignOutAsync();
        
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
        
        return Ok(new { message = "Logged out successful." });
    }
}