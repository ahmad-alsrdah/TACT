using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TACT.Models;

[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Token { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; }
    
    public int  UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null;
    
}