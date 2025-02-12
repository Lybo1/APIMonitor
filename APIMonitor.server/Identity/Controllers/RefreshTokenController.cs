using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Models;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefreshTokenController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly ITokenService tokenService;

    public RefreshTokenController(UserManager<User> userManager, ITokenService tokenService)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrEmpty(model.RefreshToken))
        {
            return Unauthorized( new { message = "Refresh token is required." } );
        }

        try
        {
            TokenResponse newTokens = await tokenService.RefreshTokenAsync(model.RefreshToken);
            
            return Ok(newTokens);
        }
        catch (SecurityTokenException e)
        {
            return Unauthorized( new { message = e.Message } );
        }
    }
}