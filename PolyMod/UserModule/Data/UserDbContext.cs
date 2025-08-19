using Microsoft.EntityFrameworkCore;
using TBD.UserModule.Data.Configuration;
using TBD.UserModule.Models;

namespace TBD.UserModule.Data;

public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: User, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        user.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        user.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Detached:
                    case EntityState.Unchanged:
                    case EntityState.Deleted:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }


        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: User, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        user.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        user.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Detached:
                    case EntityState.Unchanged:
                    case EntityState.Deleted:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
