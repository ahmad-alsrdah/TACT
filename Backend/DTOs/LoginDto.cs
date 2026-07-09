using System.ComponentModel.DataAnnotations;
namespace TACT.DTOs;

public class LoginDto
{
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is invalid")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
}