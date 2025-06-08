using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Models;

namespace TBD.ServiceModule.Data;

public class ServiceDbContext(DbContextOptions<ServiceDbContext> options) : DbContext(options)
{
    public DbSet<Service> Services { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>()
            .Property(s => s.TotalPrice)
            .HasComputedColumnSql(
                "CAST(ROUND(CASE " +
                "WHEN [DurationInMinutes] < 60 THEN ([Price] / 60.0 * [DurationInMinutes]) " +
                "ELSE [Price] * ([DurationInMinutes] / 60.0) END, 2) AS DECIMAL(18,2))",
                stored: true
            );
    }
}
