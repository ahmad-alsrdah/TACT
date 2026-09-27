using System.ComponentModel.DataAnnotations;

namespace TACT.DTOs;

public class UserAppsSyncDto
{
    [Required]
    public List<UserAppItemDto> Apps { get; set; } = new();
}

