using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TACT.Enums;

namespace TACT.Models;

[Table("BlockedApps")]
public class BlockedApp
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    public string? PackageName { get; set; }
    
    [Required]
    public string? AppName { get; set; } = string.Empty;
    
    public long TaskId { get; set; }
    [ForeignKey("TaskId")] public virtual WorkTask? WorkTask { get; set; } = null;
}