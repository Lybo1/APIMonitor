using APIMonitor.server.Identity.Services.RoleServices;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly IRoleService roleService;
    private readonly ITokenService tokenService;

    public RegisterController(UserManager<User> userManager, IRoleService roleService, ITokenService tokenService)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { message = "Invalid registration details." });
        }
        
        User existingUser = await userManager.FindByEmailAsync(model.Email);

        if (existingUser is not null)
        {
            return Conflict(new { message = "Email is already taken." });
        }

        User newUser = new User
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
        
        string accessToken = await tokenService.GenerateShortLivedAccessToken(newUser);
        string refreshToken = await tokenService.GenerateLongLivedRefreshToken(newUser);

        return Ok(new
        {
            message = "User registered successfully.",
            accessToken = model.RememberMe ? accessToken : string.Empty,
            refreshToken = model.RememberMe ? refreshToken : string.Empty
        });
    }
}