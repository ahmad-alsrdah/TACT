using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TACT.Enums;

namespace TACT.Models;

[Table("WorkTasks")]
public class WorkTask
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    public string? Title { get; set; }
    
    public DateTime StartTime { get; set; } =  DateTime.UtcNow;
    
    public DateTime EndTime { get; set; }
    
    [Required]
    public WorkTaskSource Source { get; set; }
    
    public virtual ICollection<App>  Apps { get; set; } = new List<App>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
    
    public long UserId { get; set; }
    [ForeignKey("UserId")] public User User { get; set; } = null;
    
}