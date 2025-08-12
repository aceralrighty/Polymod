using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using TBD.Shared.Repositories;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule.Repository;

public class StockRepository(StockDbContext context) : GenericRepository<RawData>(context), IStockRepository
{
    public async Task<RawData?> GetByTableIdAsync(Guid id)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task SaveStockAsync(List<Stock> stocks)
    {
        if (stocks.Count == 0)
            return;

        Console.WriteLine($"Attempting to save {stocks.Count} stocks using bulk operations...");

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
            BatchSize = 100,
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
            const int chunkSize = 1000;
            var totalSaved = 0;

            for (var i = 0; i < stocks.Count; i += chunkSize)
            {
                var chunk = stocks.Skip(i).Take(chunkSize).ToList();

                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    Console.WriteLine($"Processing chunk {i / chunkSize + 1} ({chunk.Count} records)...");

                    await context.BulkInsertAsync(chunk, bulkConfig);

                    await transaction.CommitAsync();

                    totalSaved += chunk.Count;
                    Console.WriteLine(
                        $"✅ Saved chunk {i / chunkSize + 1}: {chunk.Count} records (Total: {totalSaved}/{stocks.Count})");

                    if ((i / chunkSize + 1) % 10 == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        });

        var actualCount = await context.Stocks.CountAsync();
        Console.WriteLine($"🎯 Final verification: Database contains {actualCount} total stocks");
    }


    public async Task<IEnumerable<RawData>> GetBySymbolAsync(string symbol)
    {
        return await DbSet.Where(f => f.Symbol == symbol).OrderByDescending(f => f.Date).ToListAsync();
    }

    public async Task<IEnumerable<RawData>> GetByHighestVolumeAsync(float volume)
    {
        return await DbSet.Where(f => f.Volume > volume).OrderByDescending(f => f.Volume).ToListAsync();
    }

    public async Task<IEnumerable<RawData>> GetByLowestCloseAsync(float close)
    {
        return await DbSet.Where(f => f.Close < close).OrderBy(f => f.Close).ToListAsync();
    }

    public async Task<IEnumerable<RawData>> GetByLatestDateAsync(string date)
    {
        return await DbSet.Where(f => f.Date == date).OrderByDescending(f => f.Date).ToListAsync();
    }
}
