using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class ResetPasswordDto
{
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}