using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterController : ControllerBase
{
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly UserManager<User> userManager;

    public RegisterController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
    {
        this.roleManager = roleManager;
        this.userManager = userManager;
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        User currentUser = await userManager.FindByEmailAsync(model.Email);

        if (currentUser != null)
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

        IdentityRole? userRole = await roleManager.FindByNameAsync("User");

        if (userRole == null)
        {
            userRole = new IdentityRole("User");
            
            await roleManager.CreateAsync(userRole);
        }
        
        IdentityResult roleResult = await userManager.AddToRoleAsync(user, "User");

        if (!roleResult.Succeeded)
        {
            string errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            
            return BadRequest($"Failed to assign role: {errors}");
        }
        
        return Ok("User created successfully.");
    }
}