using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.DataAccess;
using TBD.TradingModule.MarketData.Configuration;

namespace TBD.TradingModule.MarketData;

public class TradingDbContext(DbContextOptions<TradingDbContext> options) : DbContext(options)
{
    public DbSet<RawMarketData> RawData { get; set; }
    public DbSet<StockFeatureVector> StockFeatures { get; set; }
    public DbSet<PredictionResult> Predictions { get; set; }

    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }

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
        modelBuilder.ApplyConfiguration(new RawMarketDataConfiguration());
        modelBuilder.ApplyConfiguration(new StockFeatureVectorConfiguration());
        modelBuilder.ApplyConfiguration(new ApiRequestLogConfiguration());
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
            e.Entity is RawMarketData or StockFeatureVector or PredictionResult or ApiRequestLog);

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
