using APIMonitor.server.Identity.Services.TokenServices;
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

    public LoginController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService)
    {
        userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid login request." });
        }
        
        User? user = await userManager.FindByEmailAsync(model.Email);

        if (user is null)
        {
            return Unauthorized(new { message = "Username or password is incorrect." });
        }
        
        SignInResult result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Username or password is incorrect." });
        }
        
        string accessToken = await tokenService.GenerateShortLivedAccessToken(user);
        string refreshToken = await tokenService.GenerateLongLivedRefreshToken(user);

        if (model.RememberMe)
        {
            tokenService.IssueShortLivedAccessToken(accessToken);
            tokenService.IssueLongLivedRefreshToken(refreshToken);
        }
        
        return Ok(new
        {
            message = "Login successful.",
            AccessToken = model.RememberMe ? null : accessToken,
            RefreshToken = model.RememberMe ? null : refreshToken
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