using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using StockPredictionService.Context;
using StockPredictionService.CrossCutting.Repositories;
using StockPredictionService.Models;
using StockPredictionService.Models.Stocks;
using StockPredictionService.Repository.Interfaces;

namespace StockPredictionService.Repository;

public class StockRepository(StockDbContext context) : GenericRepository<RawData>(context), IStockRepository
{
    public Task<IEnumerable<RawData>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<RawData?> GetByTableIdAsync(Guid id)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task SaveStockAsync(List<Stock> stocks, CancellationToken ct = default)
    {
        if (stocks.Count == 0)
            return;

        Console.WriteLine($"Attempting to save {stocks.Count} stocks using bulk operations...");
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        var now = DateTime.UtcNow;
        foreach (var stock in stocks)
        {
            if (stock.CreatedAt == default)
                stock.CreatedAt = now;
            stock.UpdatedAt = now;
        }

        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = false,
            SetOutputIdentity = false,
            BulkCopyTimeout = 0,
            BatchSize = 10000,
            UseTempDB = true,
            PropertiesToInclude =
            [
                nameof(Stock.Symbol),
                nameof(Stock.Open),
                nameof(Stock.High),
                nameof(Stock.Low),
                nameof(Stock.Close),
                nameof(Stock.Volume),
                nameof(Stock.Date),
                nameof(Stock.UserId),
                nameof(Stock.StockId),
                nameof(Stock.Price),
                nameof(Stock.CreatedAt),
                nameof(Stock.UpdatedAt),
                nameof(Stock.DeletedAt)
            ]
        };

        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var totalSaved = 0;
            await context.BulkInsertAsync(stocks, bulkConfig, progress =>
            {
                if (progress > 0)
                {
                    Console.WriteLine($"📈 Progress: {progress:P0}");
                }
            }, null, ct);
        });
        context.ChangeTracker.Clear();
        var actualCount = await context.Stocks.CountAsync(ct);
        Console.WriteLine($"🎯 Final verification: Database contains {actualCount} total stocks");
    }


    public async Task<IEnumerable<RawData>> GetBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        return await DbSet.Where(f => f.Symbol == symbol).OrderByDescending(f => f.Date).ToListAsync(ct);
    }

    public async Task<IEnumerable<RawData>> GetByHighestVolumeAsync(float volume, CancellationToken ct = default)
    {
        return await DbSet.Where(f => f.Volume > volume).OrderByDescending(f => f.Volume).ToListAsync(ct);
    }

    public async Task<IEnumerable<RawData>> GetByLowestCloseAsync(float close, CancellationToken ct = default)
    {
        return await DbSet.Where(f => f.Close < close).OrderBy(f => f.Close).ToListAsync(ct);
    }

    public async Task<IEnumerable<RawData>> GetByLatestDateAsync(string date, CancellationToken ct = default)
    {
        return await DbSet.Where(f => f.Date == date).OrderByDescending(f => f.Date).ToListAsync(ct);
    }
}
