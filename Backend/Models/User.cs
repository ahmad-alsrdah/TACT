using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using TACT.Enums;

namespace TACT.Models;

[Table("Users")]
public class User : IdentityUser<long>
{
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public string? ProfileImagePath { get; set; } = string.Empty;
    
    [Required]
    public int TotalScore { get; set; } = 0;

    public AppMode AppMode { get; set; } = AppMode.Light;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; } 
    
    public DateTime? DeletedAt { get; set; }
    
    public DateTime? LastLoginAt { get; set; } 
                                        
    public bool IsDeleted { get; set; }

    public virtual ICollection<WorkTask> WorkTasks { get; set; } = new List<WorkTask>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        
    public virtual GoogleCredential? GoogleCredentials { get; set; } 
    
    public virtual ICollection<UserInstalledApp>? InstalledApps { get; set; } = new List<UserInstalledApp>();
}