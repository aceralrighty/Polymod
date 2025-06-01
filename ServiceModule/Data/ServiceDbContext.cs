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
        modelBuilder.Entity<Service>().HasIndex(u => u.Id).IsUnique();
        modelBuilder.Entity<Service>().Property(u => u.TotalPrice).HasComputedColumnSql(
            "CASE \nWHEN [DurationInMinutes] < 60 \nTHEN ([Price] / 60.0 * [DurationInMinutes]) \nELSE [Price] * ([DurationInMinutes] / 60.0) \nEND");
    }
}