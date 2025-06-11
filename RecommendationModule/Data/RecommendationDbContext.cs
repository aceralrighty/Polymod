using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Data;

public class RecommendationDbContext : DbContext
{
    public DbSet<Recommendation> Recommendations { get; set; }

    public RecommendationDbContext(DbContextOptions<RecommendationDbContext> options) : base(options) { }
    public RecommendationDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Service).WithMany().HasForeignKey(r => r.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is Recommendation);
        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is not Recommendation recommendation) continue;
            recommendation.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                recommendation.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries().Where(u => u.Entity is Recommendation);
        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is not Recommendation recommendation) continue;
            recommendation.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                recommendation.CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
