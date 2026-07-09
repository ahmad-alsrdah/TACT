using System.ComponentModel.DataAnnotations;
namespace TACT.DTOs;

public class SignupDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed {1} characters.")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed {1} characters.")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password must be between {2} and {1} characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}