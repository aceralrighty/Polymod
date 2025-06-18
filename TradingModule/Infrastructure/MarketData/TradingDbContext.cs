using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Infrastructure.MarketData.Configuration;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class TradingDbContext(DbContextOptions<TradingDbContext> options) : DbContext(options)
{
    public DbSet<StockFeatureVector> StockFeatures { get; set; }
    public DbSet<PredictionResult> Predictions { get; set; }
    public DbSet<RawMarketData> RawData { get; set; }


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
        modelBuilder.ApplyConfiguration(new PredictionResultConfiguration());
        modelBuilder.ApplyConfiguration(new StockFeatureVectorConfiguration());
        modelBuilder.ApplyConfiguration(new RawDataConfiguration());
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
            e.Entity is RawMarketData or StockFeatureVector or PredictionResult);

        foreach (var entityEntry in entries)
        {
            switch (entityEntry.Entity)
            {
                case RawMarketData rawMarketData:
                {
                    rawMarketData.UpdatedAt = DateTime.UtcNow;
                    if (entityEntry.State == EntityState.Added)
                    {
                        rawMarketData.CreatedAt = DateTime.UtcNow;
                    }

                    break;
                }
                case StockFeatureVector featureVector:
                {
                    featureVector.UpdatedAt = DateTime.UtcNow;
                    if (entityEntry.State == EntityState.Added)
                    {
                        featureVector.CreatedAt = DateTime.UtcNow;
                    }

                    break;
                }
                case PredictionResult predictionResult:
                {
                    predictionResult.UpdatedAt = DateTime.UtcNow;
                    if (entityEntry.State == EntityState.Added)
                    {
                        predictionResult.CreatedAt = DateTime.UtcNow;
                    }

                    break;
                }
            }
        }
    }
}
