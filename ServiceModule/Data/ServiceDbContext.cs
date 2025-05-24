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
        modelBuilder.Entity<Service>().Property(s => s.FormattedPrice).HasColumnType("varchar(255)");
    }
}