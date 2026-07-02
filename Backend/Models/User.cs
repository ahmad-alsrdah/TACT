using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TACT.Models;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } =  string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string ProfileImagePath { get; set; } = string.Empty;
    
    [Required]
    public int TotalScore { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public DateTime LastLoginAt { get; set; }
                                        
     public bool IsDeleted { get; set; }

    public virtual ICollection<WorkTask> WorkTasks { get; set; } = new List<WorkTask>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        
    public virtual GoogleCredential? GoogleCredentials { get; set; } 
    
        
}