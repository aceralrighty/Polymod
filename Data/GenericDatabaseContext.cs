using Microsoft.EntityFrameworkCore;
using TBD.Models.Entities;

namespace TBD.Data;

public class GenericDatabaseContext(DbContextOptions<GenericDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Stats> Stats { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<Schedule> Schedules { get; set; }

    /// Configures the model and relationships for the context using the specified ModelBuilder.
    /// This method is called by the framework when the model for a derived context has been initialized,
    /// but before the model has been locked down. Implementors can override this method to modify the
    /// model using the ModelBuilder API.
    /// <param name="modelBuilder">An instance of ModelBuilder used to configure the model and its relationships.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>().HasOne(u => u.Schedule).WithOne(s => s.User).HasForeignKey<Schedule>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Stats>().HasIndex(s => s.Id).IsUnique();
        modelBuilder.Entity<Stats>().Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<UserAddress>().HasIndex(u => u.Id).IsUnique();
        modelBuilder.Entity<UserAddress>().Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<UserAddress>().HasIndex(ua => ua.Id).IsUnique();
        
        modelBuilder.Entity<Schedule>().HasIndex(s => s.Id).IsUnique();
        modelBuilder.Entity<Schedule>().Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        base.OnModelCreating(modelBuilder);
    }

    /// Saves all changes made in the context to the database.
    /// Automatically updates the `UpdatedAt` property of entities that implement the `GenericEntity` class
    /// whenever they are modified or added. Also updates the `CreatedAt` property for newly added entities.
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e is { Entity: GenericEntity, State: EntityState.Added or EntityState.Modified });

        foreach (var entityEntry in entries)
        {
            ((GenericEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((GenericEntity)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    /// Asynchronously saves all changes made in this context to the database.
    /// This method updates the CreatedAt and UpdatedAt properties of entities that inherit from GenericEntity
    /// before saving them to the database.
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation.
    /// The task result contains the number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e is { Entity: GenericEntity, State: EntityState.Added or EntityState.Modified });

        foreach (var entityEntry in entries)
        {
            ((GenericEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((GenericEntity)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}