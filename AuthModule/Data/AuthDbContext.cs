using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Models;
using TBD.UserModule.Models;

namespace TBD.AuthModule.Data;

public class AuthDbContext : DbContext
{
    public DbSet<AuthUser> AuthUsers { get; set; }
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
    public AuthDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }



    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is AuthUser);
        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is not AuthUser auth) continue;
            auth.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                auth.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is AuthUser);
        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is not AuthUser auth) continue;
            auth.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                auth.CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
