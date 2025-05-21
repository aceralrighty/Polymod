using Microsoft.EntityFrameworkCore;
using TBD.AddressService.Models;

namespace TBD.AddressService.Data;

public class AddressDbContext : DbContext
{
    public DbSet<UserAddress> UserAddress { get; set; }

    public AddressDbContext(DbContextOptions<AddressDbContext> options) : base(options)
    {
    }

    public AddressDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAddress>().HasIndex(u => u.Id).IsUnique();
        modelBuilder.Entity<UserAddress>().Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is UserAddress);
        foreach (var entityEntry in entries)
        {
            ((UserAddress)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                ((UserAddress)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is UserAddress);
        foreach (var entityEntry in entries)
        {
            ((UserAddress)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                ((UserAddress)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}