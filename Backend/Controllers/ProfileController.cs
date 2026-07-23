using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TACT.Data;
using TACT.DTOs;
using TACT.Enums;
using TACT.Models;
using TACT.Services;

namespace TACT.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;

    public ProfileController(UserManager<User> userManager, IWebHostEnvironment env,  IEmailService emailService,  ApplicationDbContext context)
    {
        _userManager = userManager;
        _env = env;
        _emailService = emailService;
        _context = context;
    }

    [HttpPut("update-info")]
    public async Task<IActionResult> UpdateInfo([FromBody] UpdateProfileDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new
            { message = "Profile updated successfully!", firstName = user.FirstName, lastName = user.LastName });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var passwordHasher = new PasswordHasher<User>();
        var verifyOldAndNew = passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, dto.NewPassword);

        if (verifyOldAndNew == PasswordVerificationResult.Success)
        {
            return BadRequest(new { message = "New password cannot be the same as your current password." });
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = "Password changed successfully!" });
    }

    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var userFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles", userId);
        if (Directory.Exists(userFolder))
        {
            Directory.Delete(userFolder, true);
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Your TACT account and all associated data have been permanently deleted." });
    }

    [HttpPost("request-change-email")]
    public async Task<IActionResult> RequestChangeEmail([FromBody] RequestChangeEmailDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var existingUser = await _userManager.FindByEmailAsync(dto.newEmail);
        if (existingUser != null)
            return BadRequest(new { message = "Email is already taken by another account." });

        var otpCode = await _userManager.GenerateUserTokenAsync(user, "Email", $"ChangeEmail:{dto.newEmail}");

        var subject = "TACT App - Confirm Your New Email";
        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; text-align: center;'>
            <h2 style='color: #2D3748;'>Confirm Your New Email</h2>
            <p>Use the 6-digit code below to confirm and update your TACT email address:</p>
            <div style='background-color: #F7FAFC; padding: 15px; display: inline-block; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #3182CE; border: 1px dashed #3182CE; margin: 20px 0; border-radius: 6px;'>
                {otpCode}
            </div>
        </div>";

        await _emailService.SendEmailAsync(dto.newEmail, subject, body);

        return Ok(new { message = "Verification code has been sent to your new email address." });
    }

    [HttpPost("confirm-change-email")]
    public async Task<IActionResult> ConfirmChangeEmail([FromBody] ChangeEmailDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var isValid = await _userManager.VerifyUserTokenAsync(user, "Email", $"ChangeEmail:{dto.newEmail}", dto.verficationCode);
        if (!isValid)
            return BadRequest(new { message = "Invalid or expired verification code." });

        user.Email = dto.newEmail;
        user.NormalizedEmail = dto.newEmail.ToUpper();
        user.UserName = dto.newEmail;
        user.NormalizedUserName = dto.newEmail.ToUpper();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        _context.RefreshTokens.UpdateRange(activeTokens);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Email updated successfully! All active sessions have been revoked. Please log in again using your new email." });
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Invalid file type. Only JPG, JPEG, and PNG are allowed." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var userFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles", userId);

        if (!Directory.Exists(userFolder))
            Directory.CreateDirectory(userFolder);

        var directoryInfo = new DirectoryInfo(userFolder);
        foreach (var existingFile in directoryInfo.GetFiles())
        {
            existingFile.Delete();
        }

        var fileName = $"avatar{extension}";
        var filePath = Path.Combine(userFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.ProfileImagePath = $"/uploads/profiles/{userId}/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Profile image uploaded successfully!", imagePath = user.ProfileImagePath });
    }

    [HttpDelete("delete-image")]
    public async Task<IActionResult> DeleteImage()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        if (string.IsNullOrEmpty(user.ProfileImagePath))
            return BadRequest(new { message = "You don't have a profile image to delete." });

        var imagePath = Path.Combine(_env.WebRootPath, user.ProfileImagePath.TrimStart('/'));

        if (System.IO.File.Exists(imagePath))
        {
            System.IO.File.Delete(imagePath);
        }

        user.ProfileImagePath = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Profile image deleted successfully!" });
    }
    
    [HttpPut("update-mode")]
    public async Task<IActionResult> UpdateGlobalMode([FromBody] AppMode mode)
    {
        if (mode < (AppMode)1 || mode > (AppMode)3)
            return BadRequest(new { message = "Invalid mode. Must be 1 (Light), 2 (Medium), or 3 (Strict)." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        user.AppMode = mode;
        await _context.SaveChangesAsync();


        return Ok(new { message = "Global mode updated successfully!", currentMode = user.AppMode });
    }
}