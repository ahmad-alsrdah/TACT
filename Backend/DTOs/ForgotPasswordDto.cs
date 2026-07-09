using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class ForgotPasswordDto
{
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

