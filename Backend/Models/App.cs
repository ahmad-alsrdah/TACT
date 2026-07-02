using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TACT.Enums;

namespace TACT.Models;

[Table("Apps")]
public class App
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string PackageName { get; set; }
    
    [Required]
    public PolicyType PolicyType { get; set; }
    
    public bool IsRevoked { get; set; }
    
    public int TaskId { get; set; }
    [ForeignKey("TaskId")] public virtual WorkTask? WorkTask { get; set; } = null;
}