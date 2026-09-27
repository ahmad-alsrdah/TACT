using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class RequestChangeEmailDto
{
    [EmailAddress]
    [Required]
    public string newEmail { get; set; } = string.Empty;
}