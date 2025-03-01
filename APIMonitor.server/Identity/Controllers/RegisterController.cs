using APIMonitor.server.Data;
using APIMonitor.server.Identity.Services.RoleServices;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly IRoleService roleService;
    private readonly ITokenService tokenService;
    private readonly IAuditLogService auditLogService;
    private readonly INotificationService notificationService;

    public RegisterController(UserManager<User> userManager, IRoleService roleService, ITokenService tokenService, IAuditLogService auditLogService, INotificationService notificationService)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel? model)
    {
        DateTime startTime = DateTime.UtcNow;
                
        if (model is null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { message = "Invalid registration details." });
        }
        
        User existingUser = await userManager.FindByEmailAsync(model.Email);

        if (existingUser is not null)
        {
            return Conflict(new { message = "Email is already taken." });
        }

        User newUser = new()
        {
            UserName = model.Email,
            Email = model.Email,
            RememberMe = model.RememberMe,
        };
        
        IdentityResult result = await userManager.CreateAsync(newUser, model.Password);

        if (!result.Succeeded)
        {
            string errors = string.Join(',', result.Errors.Select(e => e.Description));
            
            return BadRequest(new { message = errors });
        }

        const string defaultRole = "User";
        
        bool isInRole = await userManager.IsInRoleAsync(newUser, defaultRole);
        
        await auditLogService.LogActionAsync(newUser.Id, "Register", $"User {model.Email} registered.", startTime);
        await notificationService.SendNotificationAsync(newUser.Id.ToString(), "Welcome!", "Your account has been created.", HttpContext);

        if (!isInRole)
        {
            IdentityRole<int>? existingRole = await roleService.GetAllRolesAsync()
                .ContinueWith(t => t.Result.FirstOrDefault(r => r.Name == defaultRole));

            if (existingRole is null)
            {
                IdentityResult roleCreateResult = await roleService.CreateRoleAsync(defaultRole);

                if (!roleCreateResult.Succeeded)
                {
                    return StatusCode(500, new { message = $"Failed to create role {defaultRole}." });
                }
            }
            
            await userManager.AddToRoleAsync(newUser, defaultRole);
        }
        
        string token = model.RememberMe
            ? await tokenService.GenerateLongLivedRefreshToken(newUser)
            : await tokenService.GenerateShortLivedAccessToken(newUser);
        
        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(Constants.RefreshTokenExpirationDays) : DateTimeOffset.UtcNow.AddHours(Constants.DefaultAccessTokenExpirationMinutes)
        });
        
        return Ok(new
        {
            message = "User registered successfully.",
            TokenType = model.RememberMe ? "LongLivedRefreshToken" : "ShortLivedAccessToken"
        });
    }
}