using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class UserAppItemDto
{
    [Required]
    public string PackageName { get; set; } = string.Empty;

    public string AppName { get; set; } = string.Empty;
}