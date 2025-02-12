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
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        User currentUser = await userManager.FindByEmailAsync(model.Email);

        if (currentUser is not null)
        {
            return BadRequest("Email is already taken.");
        }

        User user = new()
        {
            Email = model.Email,
            UserName = model.Email,
            RememberMe = model.RememberMe,
        };
        
        IdentityResult result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            
            return BadRequest($"User creation failed: {errors}");
        }

        IdentityResult roleResult = await roleService.CreateRoleAsync("User");

        if (!roleResult.Succeeded)
        {
            string errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            
            return BadRequest($"Role creation failed: {errors}");
        }
        
        await userManager.AddToRoleAsync(user, "User");
        
        string accessToken = await tokenService.GenerateShortLivedAccessToken(user);
        string refreshToken = await tokenService.GenerateLongLivedRefreshToken(user);
        
        tokenService.IssueShortLivedAccessToken(accessToken);
        tokenService.IssueLongLivedRefreshToken(refreshToken);

        return Ok(
            new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            }
        );
    }
}