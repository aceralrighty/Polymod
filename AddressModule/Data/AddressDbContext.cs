using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TBD.AddressModule.Models;

namespace TBD.AddressModule.Data;

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
        modelBuilder.Entity<UserAddress>().Property(u => u.CreatedAt)
            .ValueGeneratedOnAdd().Metadata
            .SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        ;
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(u =>
            u is { Entity: Models.UserAddress, State: EntityState.Added or EntityState.Modified });
        foreach (var entityEntry in entries)
        {
            var entity = (UserAddress)entityEntry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries().Where(u =>
            u is { Entity: Models.UserAddress, State: EntityState.Added or EntityState.Modified });
        foreach (var entityEntry in entries)
        {
            var entity = (UserAddress)entityEntry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}