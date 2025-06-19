using Microsoft.EntityFrameworkCore;
using TBD.StockPredictionModule.Context.Configuration;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;

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
        modelBuilder.ApplyConfiguration(new StockPredictionConfiguration());
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
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified &&
                        e.Entity is RawData or Stock or StockPrediction);

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case RawData r:
                    r.UpdatedAt = now;
                    if (entry.State == EntityState.Added)
                        r.CreatedAt = now;
                    break;
                case Stock s:
                    s.UpdatedAt = now;
                    if (entry.State == EntityState.Added)
                        s.CreatedAt = now;
                    break;
                case StockPrediction p:
                    p.UpdatedAt = now;
                    if (entry.State == EntityState.Added)
                        p.CreatedAt = now;
                    break;
            }
        }
    }
}
