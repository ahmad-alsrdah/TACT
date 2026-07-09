using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TACT.Enums;

namespace TACT.Models;

[Table("Logs")]
public class Log
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    public LogType Type { get; set; }
    
    [Required]
    public LogStatus Status { get; set; }
    
    [Required]
    public string? Description { get; set; }
    
    [Required]
    public decimal AiSeverityScore { get; set; }
    
    public DateTime TimeStamp { get; set; }
    
    public long TaskId { get; set; }
    [ForeignKey("TaskId")] public virtual WorkTask WorkTask { get; set; } = null;
    
}