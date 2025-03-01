using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Identity;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly ApplicationDbContext dbContext;
    private readonly IAuditLogService auditLogService;
    private readonly INotificationService notificationService;

    public UserController(UserManager<User> userManager, ApplicationDbContext dbContext, IAuditLogService auditLogService, INotificationService notificationService)
    {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDetails()
    {
        DateTime startTime = DateTime.UtcNow;
        
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        User? user = await userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        
        await auditLogService.LogActionAsync(int.Parse(userId), "GetUserDetails", "User retrieved profile details.", startTime);
        
        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            Roles = await userManager.GetRolesAsync(user)
        });
    }

    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
    {
        DateTime startTime = DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(request.NewUsername))
        {
            return BadRequest(new { message = "New username is required." });
        }
        
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        User? user = await userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        
        string oldUsername = user.UserName!;
        user.UserName = request.NewUsername;
        IdentityResult result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to update username.", errors = result.Errors });
        }
        
        await auditLogService.LogActionAsync(int.Parse(userId), "UpdateUsername", $"Changed from {oldUsername} to {request.NewUsername}", startTime);
        await notificationService.SendNotificationAsync(userId, "Username Updated", $"Your username has been changed to {request.NewUsername}.", HttpContext);
        
        return Ok(new { message = "Username updated successfully.", newUsername = request.NewUsername });
    }

    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        DateTime startTime = DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Current and new passwords are required." });
        }
        
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        User? user = await userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        
        IdentityResult result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to update password.", errors = result.Errors });
        }
        
        await auditLogService.LogActionAsync(int.Parse(userId), "UpdatePassword", "Password changed successfully.", startTime);
        await notificationService.SendNotificationAsync(userId, "Password Updated", "Your password has been successfully updated.", HttpContext);
        
        return Ok(new { message = "Password updated successfully." });
    }

    [HttpPut("name")]
    public async Task<IActionResult> UpdateName([FromBody] UpdateNameRequest request)
    {
        DateTime startTime = DateTime.UtcNow;
        
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        User? user = await userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        
        string oldFirstName = user.FirstName!;
        string oldLastName = user.LastName!;
        user.FirstName = request.FirstName ?? "";
        user.LastName = request.LastName ?? "";

        IdentityResult result = await userManager.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to update name.", errors = result.Errors });
        }
        
        await auditLogService.LogActionAsync(int.Parse(userId), "UpdateName", $"Name changed from {oldFirstName} {oldLastName} to {user.FirstName} {user.LastName}", startTime);
        await notificationService.SendNotificationAsync(userId, "Name Updated", $"Your name has been updated to {user.FirstName} {user.LastName}.", HttpContext);
        
        return Ok(new { message = "Name updated successfully.", firstName = user.FirstName, lastName = user.LastName });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        DateTime startTime = DateTime.UtcNow;
        
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        User? user = await userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        
        IdentityResult result = await userManager.DeleteAsync(user);
        
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to delete account.", errors = result.Errors });
        }
        
        await auditLogService.LogActionAsync(int.Parse(userId), "DeleteAccount", $"Account {user.UserName} deleted.", startTime);
        await notificationService.SendNotificationAsync(userId, "Account Deleted", "Your account has been deleted.", HttpContext);

        return Ok(new { message = "Account deleted successfully." });
    }
}
