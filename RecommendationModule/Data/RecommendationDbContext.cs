using Microsoft.EntityFrameworkCore;
using TBD.GenericDBProperties;
using TBD.RecommendationModule.Models;
using TBD.ScheduleModule.Models;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Data;

public class RecommendationDbContext : DbContext
{
    public DbSet<UserRecommendation> UserRecommendations { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Schedule> Schedules { get; set; }

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

        // UserRecommendation configuration
        modelBuilder.Entity<UserRecommendation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Service)
                .WithMany()
                .HasForeignKey(r => r.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasOne(u => u.Schedule)
                .WithOne(s => s.User)
                .HasForeignKey<Schedule>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Schedule configuration
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.User)
                .WithOne(u => u.Schedule)
                .HasForeignKey<Schedule>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the JSON column for DaysWorked
            entity.Property(s => s.DaysWorkedJson)
                .HasColumnName("DaysWorkedJson")
                .HasColumnType("nvarchar(255)");

            // Ignore the computed property since it's not mapped
            entity.Ignore(s => s.DaysWorked);

            // Configure precision for decimal/double properties
            entity.Property(s => s.BasePay)
                .HasPrecision(18, 2);

            entity.Property(s => s.TotalPay)
                .HasPrecision(18, 2);
        });

        // Service configuration (same as ServiceDbContext)
        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(s => s.Price)
                .HasPrecision(18, 2);

            entity.Property(s => s.TotalPrice)
                .HasPrecision(18, 2)
                .HasComputedColumnSql(
                    "CAST(ROUND(CASE " +
                    "WHEN [DurationInMinutes] < 60 THEN ([Price] / 60.0 * [DurationInMinutes]) " +
                    "ELSE [Price] * ([DurationInMinutes] / 60.0) END, 2) AS DECIMAL(18,2))",
                    stored: true
                );
        });
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
            e.Entity is UserRecommendation or Service or User or Schedule);

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
