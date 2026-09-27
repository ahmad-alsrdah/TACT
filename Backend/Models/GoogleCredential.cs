using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TACT.Models;

[Table("GoogleCredentials")]
public class GoogleCredential
{
    [Key]
    public long Id { get; set; }
    
    public string? AccessToken { get; set; } = string.Empty;
    
    public string? RefreshToken { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    
    public long UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null;
    
}