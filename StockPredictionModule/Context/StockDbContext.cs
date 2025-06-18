using Microsoft.EntityFrameworkCore;
using TBD.StockPredictionModule.Context.Configuration;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Context;

public class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options) : base(options) { }
    public StockDbContext() { }

    public DbSet<RawData> StockData { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockPrediction> StockPredictions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("FallbackTestDb");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            e.Entity is RawData or Stock or StockPrediction);
        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is not RawData rawData) continue;
            rawData.UpdatedAt = DateTime.UtcNow;
        }

        base.SaveChanges();
    }
}
