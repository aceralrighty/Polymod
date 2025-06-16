using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.DataAccess.Interfaces;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class TradingRepository(TradingDbContext context, ILogger<TradingRepository> logger)
    : ITradingRepository
{
    public async Task SaveMarketDataAsync(List<RawMarketData> marketData)
    {
        if (marketData.Count == 0) return;

        // Use bulk insert for better performance
        var existingKeys = await context.RawData
            .Where(d => marketData.Select(m => m.Symbol).Contains(d.Symbol))
            .Where(d => marketData.Select(m => m.Date).Contains(d.Date))
            .Select(d => new { d.Symbol, d.Date })
            .ToListAsync();

        var newData = marketData
            .Where(m => !existingKeys.Any(k => k.Symbol == m.Symbol && k.Date == m.Date))
            .ToList();

        if (newData.Any())
        {
            await context.RawData.AddRangeAsync(newData);
            await context.SaveChangesAsync();

            logger.LogInformation("Saved {Count} new market data records", newData.Count);
        }
    }

    public async Task<List<RawMarketData>> GetMarketDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        return await context.RawData
            .Where(d => d.Symbol == symbol && d.Date >= startDate && d.Date <= endDate)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    public async Task<DateTime?> GetLatestDataDateAsync(string symbol)
    {
        return await context.RawData
            .Where(d => d.Symbol == symbol)
            .MaxAsync(d => (DateTime?)d.Date);
    }

    public async Task SaveFeatureVectorsAsync(List<StockFeatureVector> features)
    {
        if (features.Count == 0) return;

        // Remove existing features for the same dates/symbols to avoid duplicates
        var keysToRemove = features.Select(f => new { f.Symbol, f.Date }).ToList();

        var existingFeatures = await context.StockFeatures
            .Where(f => keysToRemove.Any(k => k.Symbol == f.Symbol && k.Date == f.Date))
            .ToListAsync();

        if (existingFeatures.Count != 0)
        {
            context.StockFeatures.RemoveRange(existingFeatures);
        }

        await context.StockFeatures.AddRangeAsync(features);
        await context.SaveChangesAsync();

        logger.LogInformation("Saved {Count} feature vectors", features.Count);
    }

    public async Task<List<StockFeatureVector>> GetFeatureVectorsAsync(string symbol, DateTime startDate,
        DateTime endDate)
    {
        return await context.StockFeatures
            .Where(f => f.Symbol == symbol && f.Date >= startDate && f.Date <= endDate)
            .OrderBy(f => f.Date)
            .ToListAsync();
    }

    public async Task SavePredictionAsync(PredictionResult prediction)
    {
        context.Predictions.Add(prediction);
        await context.SaveChangesAsync();
    }

    public async Task<List<PredictionResult>> GetPredictionsAsync(DateTime predictionDate)
    {
        return await context.Predictions
            .Where(p => p.PredictionDate.Date == predictionDate.Date)
            .OrderByDescending(p => p.RiskAdjustedScore)
            .ToListAsync();
    }

    public async Task UpdateActualResultsAsync(string symbol, DateTime targetDate, float actualReturn,
        float actualVolatility)
    {
        var predictions = await context.Predictions
            .Where(p => p.Symbol == symbol && p.TargetDate.Date == targetDate.Date)
            .ToListAsync();

        foreach (var prediction in predictions)
        {
            prediction.ActualReturn = actualReturn;
            prediction.ActualVolatility = actualVolatility;
        }

        await context.SaveChangesAsync();
    }


    public async Task<bool> HasDataForSymbolAsync(string symbol, DateTime date)
    {
        return await context.RawData
            .AnyAsync(d => d.Symbol == symbol && d.Date.Date == date.Date);
    }
}
