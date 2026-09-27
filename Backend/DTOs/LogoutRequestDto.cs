using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class LogoutRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}