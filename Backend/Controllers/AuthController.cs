using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TACT.Data;
using TACT.DTOs;
using TACT.Models;
using TACT.Services;
using Google.Apis.Auth; 

namespace TACT.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService; 
    private readonly IConfiguration _configuration; 

    public AuthController(UserManager<User> userManager, IJwtService jwtService, ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _context =  context;
        _emailService = emailService; 
        _configuration = configuration;
    }
    
    [HttpPost("signup")]  
    public async Task<IActionResult> Signup([FromBody] SignupDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userExists = await _userManager.FindByEmailAsync(dto.Email);
        if (userExists != null)
            return BadRequest(new { message = "Email address is already registered." });

        var newUser = new User
        {
            UserName = dto.Email, 
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(newUser, dto.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        var welcomeSubject = "Welcome to TACT!";
        var welcomeBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
            <h2 style='color: #3182CE; text-align: center;'>Welcome to TACT, {dto.FirstName}!</h2>
            <p>Thank you for signing up. Your account has been created successfully.</p>
            <p>We are excited to have you on board. You can now log in and start exploring all the features of TACT.</p>
            <hr style='border: none; border-top: 1px solid #E2E8F0; margin: 20px 0;'>
            <p style='font-size: 12px; color: #A0AEC0; text-align: center;'>TACT Application Team</p>
        </div>";

        await _emailService.SendEmailAsync(newUser.Email, welcomeSubject, welcomeBody);
        
        var authResponse = _jwtService.GenerateAuthTokens(newUser);
        authResponse.Message = "User registered successfully!";

        return Ok(authResponse);
    }
    
    [HttpPost("login")] 
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email address or password." });

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid email address or password." });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var authResponse = _jwtService.GenerateAuthTokens(user);
        authResponse.Message = "Logged in successfully!";
        
        var refreshTokenEntity = new RefreshToken
        {
            Token = authResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), 
            IsRevoked = false,
            UserId = user.Id
        };

        await _context.RefreshTokens.AddAsync(refreshTokenEntity);
        await _context.SaveChangesAsync();
        
        return Ok(authResponse);
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var googleSettings = _configuration.GetSection("GoogleSettings");
            
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { googleSettings["ClientId"] }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            
            var email = payload.Email;
            var firstName = payload.GivenName ?? "";
            var lastName = payload.FamilyName ?? "";

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true 
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return BadRequest(new { message = "Failed to create user via Google.", errors = createResult.Errors });

                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                await _userManager.AddLoginAsync(user, loginInfo);
                
                var welcomeSubject = "Welcome to TACT via Google!";
                var welcomeBody = $"<h3>Welcome {user.FirstName}!</h3><p>Your account has been successfully created using Google.</p>";
                await _emailService.SendEmailAsync(user.Email, welcomeSubject, welcomeBody);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var expiresAt = DateTime.UtcNow.AddSeconds(dto.ExpiresInSeconds);
            var existingCred = await _context.GoogleCredentials.FirstOrDefaultAsync(gc => gc.UserId == user.Id);

            if (existingCred == null)
            {
                var newCred = new GoogleCredential
                {
                    UserId = user.Id,
                    AccessToken = dto.AccessToken,
                    RefreshToken = dto.RefreshToken,
                    ExpiresAt = expiresAt
                };
                await _context.GoogleCredentials.AddAsync(newCred);
            }
            else
            {
                existingCred.AccessToken = dto.AccessToken;
                existingCred.ExpiresAt = expiresAt;
                if (!string.IsNullOrEmpty(dto.RefreshToken))
                {
                    existingCred.RefreshToken = dto.RefreshToken;
                }
                _context.GoogleCredentials.Update(existingCred);
            }

            var authResponse = _jwtService.GenerateAuthTokens(user);
            
            var refreshTokenEntity = new RefreshToken
            {
                Token = authResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                UserId = user.Id
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            authResponse.Message = "Logged in successfully via Google!";
            return Ok(authResponse);
        }
        catch (InvalidJwtException)
        {
            return BadRequest(new { message = "Invalid Google ID Token." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during Google authentication.", detail = ex.Message });
        }
    }
    
    [HttpPost("refresh-token")] 
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null)
            return Unauthorized(new { message = "Invalid refresh token." });

        if (storedToken.IsRevoked)
            return Unauthorized(new { message = "This refresh token has been revoked." });

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Refresh token has expired. Please log in again." });

        storedToken.IsRevoked = true;
        _context.RefreshTokens.Update(storedToken);

        var authResponse = _jwtService.GenerateAuthTokens(storedToken.User);

        var newRefreshToken = new RefreshToken
        {
            Token = authResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), 
            IsRevoked = false,
            UserId = storedToken.UserId
        };

        await _context.RefreshTokens.AddAsync(newRefreshToken);
        await _context.SaveChangesAsync(); 

        authResponse.Message = "Token refreshed successfully!";
        return Ok(authResponse);
    }
    
    [HttpPost("logout")] 
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null)
            return Ok(new { message = "Logged out successfully (Token not found)." });

        storedToken.IsRevoked = true;
        _context.RefreshTokens.Update(storedToken);
    
        await _context.SaveChangesAsync();

        return Ok(new { message = "Logged out successfully!" });
    }
    
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Ok(new { message = "If the email exists, a secure reset code has been sent." }); 

        var otpCode = await _userManager.GenerateUserTokenAsync(user, "Email", "ResetPasswordPurpose");

        var emailSubject = "TACT App - Your Reset Code";
        var emailBody = $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; text-align: center;'>
        <h2 style='color: #2D3748;'>Reset Your Password</h2>
        <p>Use the 6-digit verification code below to reset your TACT password. This code is valid for a limited time:</p>
        <div style='background-color: #F7FAFC; padding: 15px; display: inline-block; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #3182CE; border: 1px dashed #3182CE; margin: 20px 0; border-radius: 6px;'>
            {otpCode}
        </div>
        <p style='color: #718096; font-size: 12px;'>If you didn't request this, please ignore this email.</p>
    </div>";

        await _emailService.SendEmailAsync(user.Email!, emailSubject, emailBody);
   
        return Ok(new { message = "If the email exists, a secure reset code has been sent." });
    }
    
    
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) 
            return BadRequest(new { message = "Invalid request." });

        var isValid = await _userManager.VerifyUserTokenAsync(user, "Email", "ResetPasswordPurpose", dto.VerificationCode);
    
        if (!isValid)
        {
            return BadRequest(new { message = "Invalid or expired verification code." });
        }

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded) return BadRequest(removeResult.Errors);

        var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
        if (!addResult.Succeeded) return BadRequest(addResult.Errors);

        return Ok(new { message = "Password has been reset successfully! You can login now." });
    }
}