using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TACT.Models;

namespace TACT.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<long>, long>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<GoogleCredential> GoogleCredentials => Set<GoogleCredential>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<App> Apps => Set<App>();
    public DbSet<Log> Logs => Set<Log>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();

        modelBuilder.Entity<GoogleCredential>()
            .HasIndex(g => g.UserId)
            .IsUnique();

        modelBuilder.Entity<Log>()
            .Property(l => l.AiSeverityScore)
            .HasColumnType("decimal(3,2)");
            
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);
    }
}