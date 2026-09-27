using System.ComponentModel.DataAnnotations;
using TACT.Enums;

namespace TACT.DTOs;

public class CreateTaskRequestDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(1);
}