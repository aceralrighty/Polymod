using Microsoft.EntityFrameworkCore;
using TBD.GenericDBProperties;
using TBD.RecommendationModule.Data.Configuration.Recommendation;
using TBD.RecommendationModule.Data.Configuration.Schedule;
using TBD.RecommendationModule.Data.Configuration.User;
using TBD.RecommendationModule.Models;
using TBD.ScheduleModule.Models;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Data;

public class RecommendationDbContext : DbContext
{
     public DbSet<UserRecommendation> UserRecommendations { get; set; }
    public DbSet<RecommendationOutput> RecommendationOutputs { get; set; } // New table
    public DbSet<User> Users { get; set; }
    public DbSet<Service> Services { get; set; }

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
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserRecommendationConfiguration());
        modelBuilder.ApplyConfiguration(new RecommendationOutputConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries().Where(e =>
            e.Entity is UserRecommendation or RecommendationOutput or Service or User or Schedule);

        foreach (var entityEntry in entries)
        {
            // Handle UserRecommendation
            if (entityEntry.Entity is UserRecommendation recommendation)
            {
                recommendation.UpdatedAt = DateTime.UtcNow;
                if (entityEntry.State == EntityState.Added)
                {
                    recommendation.CreatedAt = DateTime.UtcNow;
                }
            }

            // Handle RecommendationOutput
            if (entityEntry.Entity is RecommendationOutput output)
            {
                output.UpdatedAt = DateTime.UtcNow;
                if (entityEntry.State == EntityState.Added)
                {
                    output.CreatedAt = DateTime.UtcNow;
                    output.GeneratedAt = DateTime.UtcNow;
                }
            }

            // Handle entities that inherit from BaseTableProperties
            if (entityEntry.Entity is not BaseTableProperties baseEntity)
            {
                continue;
            }

            baseEntity.UpdatedAt = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                baseEntity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
