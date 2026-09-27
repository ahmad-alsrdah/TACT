using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class GoogleLoginDto
{
    public string IdToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresInSeconds { get; set; } 
}