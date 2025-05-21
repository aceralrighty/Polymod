using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Models;

namespace TBD.ScheduleModule.Data;

public class ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : DbContext(options)
{
    public DbSet<Schedule> Schedules { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.User)
            .WithOne(u => u.Schedule)
            .HasForeignKey<Schedule>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Schedule>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        base.OnModelCreating(modelBuilder);
    }


    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is Schedule);
        foreach (var entityEntry in entries)
        {
            ((Schedule)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                ((Schedule)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is Schedule);
        foreach (var entityEntry in entries)
        {
            ((Schedule)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                ((Schedule)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}