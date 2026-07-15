using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TACT.Data;
using TACT.DTOs;
using TACT.Models;

namespace TACT.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class AppsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppsController> _logger;

    public AppsController(ApplicationDbContext context, ILogger<AppsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("sync-installed-apps")]
    public async Task<IActionResult> SyncInstalledApps([FromBody] UserAppsSyncDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
        {
            _logger.LogWarning("Unauthorized access attempt: Invalid or missing User ID in Token.");
            return Unauthorized(new { message = "Invalid user token." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Syncing installed apps for User ID: {UserId}. Apps received: {Count}", userId, dto.Apps.Count);

            var existingApps = await _context.UserInstalledApps
                .Where(ua => ua.UserId == userId)
                .ToListAsync();

            if (existingApps.Any())
            {
                _context.UserInstalledApps.RemoveRange(existingApps);
            }

            var newApps = dto.Apps.Select(app => new UserInstalledApp
            {
                PackageName = app.PackageName,
                AppName = app.AppName,
                UserId = userId,
                LastSyncedAt = DateTime.UtcNow
            }).ToList();

            await _context.UserInstalledApps.AddRangeAsync(newApps);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Successfully synchronized {Count} apps for User ID: {UserId}", newApps.Count, userId);

            return Ok(new { message = "Apps synchronized successfully!", count = newApps.Count });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred while syncing apps for User ID: {UserId}", userId);
            throw; 
        }
    }
}