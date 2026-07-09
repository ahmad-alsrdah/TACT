using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class ChangeEmailDto
{
    [EmailAddress]
    [Required]
    public string newEmail { get; set; } = string.Empty;
    
    public string verficationCode { get; set; } = string.Empty;
}