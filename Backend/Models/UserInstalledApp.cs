using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TACT.Models;

public class UserInstalledApp
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string? PackageName { get; set; } = string.Empty;

    public string? AppName { get; set; } = string.Empty;

    [Required]
    public long UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}