using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationPreferencesController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public NotificationPreferencesController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserPreferences()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }

        NotificationPreference? preferences = await dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == Convert.ToInt32(userId));

        if (preferences is null)
        {
            return NotFound(new { message = "Notification preferences not found" });
        }
        
        return Ok(preferences);
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreference updatedPreferences)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        NotificationPreference? preferences = await dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == Convert.ToInt32(userId));

        if (preferences is null)
        {
            preferences = new NotificationPreference
            {
                UserId = Convert.ToInt32(userId),
                EnablePush = updatedPreferences.EnablePush,
                EnableCriticalAlertsOnly = updatedPreferences.EnableCriticalAlertsOnly,
                UpdatedAt = DateTime.UtcNow
            };
            
            await dbContext.NotificationPreferences.AddAsync(preferences);
        }
        else
        {
            preferences.EnablePush = updatedPreferences.EnablePush;
            preferences.EnableCriticalAlertsOnly = updatedPreferences.EnableCriticalAlertsOnly;
            preferences.UpdatedAt = DateTime.UtcNow;
            
            dbContext.NotificationPreferences.Update(preferences);
        }
        
        await dbContext.SaveChangesAsync();
        
        return Ok(new { message = "Notification preferences updated successfully." });
    }

}