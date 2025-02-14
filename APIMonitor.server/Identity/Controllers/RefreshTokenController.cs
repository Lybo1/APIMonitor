using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Models;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefreshTokenController : ControllerBase
{
    private readonly ITokenService tokenService;

    public RefreshTokenController(ITokenService tokenService)
    {
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            TokenResponse newToken = await tokenService.RefreshTokenAsync(model.RefreshToken);
            
            return Ok(newToken);
        }
        catch (SecurityTokenException e)
        {
            return Unauthorized(new { message = "Authentication failed. Please re-login." } );
        }
    }
}