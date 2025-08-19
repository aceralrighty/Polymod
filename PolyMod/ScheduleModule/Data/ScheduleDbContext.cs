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
        modelBuilder.Entity<Schedule>().HasIndex(u => u.UserId).IsUnique();

        modelBuilder.Entity<Schedule>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<Schedule>().Property(s => s.DaysWorkedJson).HasColumnType("varchar(max)");
        modelBuilder.Entity<Schedule>()
            .Property(s => s.TotalPay)
            .HasComputedColumnSql(
                "CASE " +
                // No overtime (≤40 hours)
                "WHEN [TotalHoursWorked] <= 40 THEN [BasePay] * [TotalHoursWorked] " +
                // Regular overtime only (41-60 hours): 1.5x rate
                "WHEN [TotalHoursWorked] <= 60 THEN " +
                "([BasePay] * 40) + " + // Regular pay for the first 40 hours
                "(([BasePay] * 1.5) * ([TotalHoursWorked] - 40)) " + // 1.5x for hours 41-60
                // Double overtime (61+ hours): 1.5x for 41-60, 2x for 61+
                "ELSE " +
                "([BasePay] * 40) + " + // Regular pay for the first 40 hours
                "(([BasePay] * 1.5) * 20) + " + // 1.5x for hours 41-60 (20-hour max)
                "(([BasePay] * 2.0) * ([TotalHoursWorked] - 60)) " + // 2x for hours 61+
                "END"
            );


        modelBuilder.Entity<Schedule>().HasQueryFilter(s => s.DeletedAt == null);

        base.OnModelCreating(modelBuilder);
    }


    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is Schedule);
        foreach (var entityEntry in entries)
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
        var entries = ChangeTracker.Entries().Where(u => u.Entity is Schedule);
        foreach (var entityEntry in entries)
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
