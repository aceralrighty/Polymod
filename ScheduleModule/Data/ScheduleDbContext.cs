using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        modelBuilder.Entity<Schedule>().Property(s => s.DaysWorkedJson).HasColumnType("varchar(255)");
        modelBuilder.Entity<Schedule>()
            .Property(s => s.TotalPay)
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [TotalHoursWorked] > 40 THEN ([BasePay] * 40) + (([BasePay] * 1.5) * ([TotalHoursWorked] - 40)) " +
                "ELSE [BasePay] * [TotalHoursWorked] " +
                "END"
            );

        modelBuilder.Entity<Schedule>().HasQueryFilter(s => s.DeletedAt == null);

        base.OnModelCreating(modelBuilder);
    }


    public override int SaveChanges()
    {
        IEnumerable<EntityEntry> entries = ChangeTracker.Entries().Where(u => u.Entity is Schedule);
        foreach (EntityEntry entityEntry in entries)
        {
            if (entityEntry.Entity is not Schedule schedule) continue;
            schedule.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                schedule.CreatedAt = DateTime.UtcNow;
            }
        }


        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<EntityEntry> entries = ChangeTracker.Entries().Where(u => u.Entity is Schedule);
        foreach (EntityEntry entityEntry in entries)
        {
            if (entityEntry.Entity is not Schedule schedule) continue;
            schedule.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                schedule.CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
