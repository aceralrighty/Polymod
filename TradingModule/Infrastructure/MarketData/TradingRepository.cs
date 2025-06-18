using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Core.Entities.Interfaces;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class TradingRepository(TradingDbContext context, ILogger<TradingRepository> logger)
    : ITradingRepository
{
    public async Task SaveMarketDataAsync(List<RawMarketData> marketData)
    {
        if (marketData.Count == 0) return;

        try
        {
            // Create a HashSet for efficient duplicate checking
            var incomingKeys = (marketData ?? throw new ArgumentNullException(nameof(marketData)))
                .Select(m => $"{m.Symbol}_{m.Date:yyyy-MM-dd}")
                .ToHashSet();

            var existingKeys = await context.RawData
                .Where(d => marketData.Select(m => m.Symbol).Contains(d.Symbol))
                .Where(d => marketData.Select(m => m.Date.Date).Contains(d.Date.Date))
                .Select(d => $"{d.Symbol}_{d.Date:yyyy-MM-dd}")
                .ToHashSetAsync();

            var newData = marketData
                .Where(m => !existingKeys.Contains($"{m.Symbol}_{m.Date:yyyy-MM-dd}"))
                .ToList();

            if (newData.Any())
            {
                await context.RawData.AddRangeAsync(newData);
                await context.SaveChangesAsync();

                logger.LogInformation("Saved {Count} new market data records out of {Total} provided",
                    newData.Count, marketData.Count);
            }
            else
            {
                logger.LogInformation("No new market data to save - all {Count} records already exist",
                    marketData.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save market data. Records count: {Count}", marketData?.Count ?? 0);
            throw;
        }
    }

    public async Task<List<RawMarketData>> GetMarketDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            return await context.RawData
                .Where(d => d.Symbol == symbol && d.Date >= startDate && d.Date <= endDate)
                .OrderBy(d => d.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get market data for symbol {Symbol} from {StartDate} to {EndDate}",
                symbol, startDate, endDate);
            throw;
        }
    }

    public async Task<DateTime?> GetLatestDataDateAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            return await context.RawData
                .Where(d => d.Symbol == symbol)
                .MaxAsync(d => (DateTime?)d.Date);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get latest data date for symbol {Symbol}", symbol);
            throw;
        }
    }

    public async Task SaveFeatureVectorsAsync(List<StockFeatureVector> features)
    {
        if (features.Count == 0) return;

        try
        {
            // More efficient approach: use a single query to remove existing features
            var symbolDatePairs = features.Select(f => new { f.Symbol, f.Date.Date }).ToList();

            var existingFeatures = await context.StockFeatures
                .Where(f => symbolDatePairs.Any(p => p.Symbol == f.Symbol && p.Date == f.Date.Date))
                .ToListAsync();

            if (existingFeatures.Count > 0)
            {
                context.StockFeatures.RemoveRange(existingFeatures);
            }

            await context.StockFeatures.AddRangeAsync(features);
            await context.SaveChangesAsync();

            logger.LogInformation("Saved {Count} feature vectors, replaced {ExistingCount} existing records",
                features.Count, existingFeatures.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save feature vectors. Count: {Count}", features?.Count ?? 0);
            throw;
        }
    }

    public async Task<List<StockFeatureVector>> GetFeatureVectorsAsync(string symbol, DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            return await context.StockFeatures
                .Where(f => f.Symbol == symbol && f.Date >= startDate && f.Date <= endDate)
                .OrderBy(f => f.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get feature vectors for symbol {Symbol} from {StartDate} to {EndDate}",
                symbol, startDate, endDate);
            throw;
        }
    }

    public async Task SavePredictionAsync(PredictionResult prediction)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        try
        {
            context.Predictions.Add(prediction);
            await context.SaveChangesAsync();

            logger.LogDebug("Saved prediction for symbol {Symbol} with target date {TargetDate}",
                prediction.Symbol, prediction.TargetDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save prediction for symbol {Symbol}", prediction.Symbol);
            throw;
        }
    }

    public async Task SavePredictionsAsync(List<PredictionResult> predictions)
    {
        if (predictions.Count == 0) return;

        try
        {
            await context.Predictions.AddRangeAsync(predictions ??
                                                    throw new ArgumentNullException(nameof(predictions)));
            await context.SaveChangesAsync();

            logger.LogInformation("Saved {Count} predictions", predictions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save bulk predictions. Count: {Count}", predictions?.Count ?? 0);
            throw;
        }
    }

    public async Task<List<PredictionResult>> GetPredictionsAsync(DateTime predictionDate)
    {
        try
        {
            return await context.Predictions
                .Where(p => p.PredictionDate.Date == predictionDate.Date)
                .OrderByDescending(p => p.RiskAdjustedScore)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get predictions for date {PredictionDate}", predictionDate);
            throw;
        }
    }

    public async Task<List<PredictionResult>> GetPredictionsBySymbolAsync(string symbol, DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            var symbolPredictions = await context.Predictions
                .Where(p => p.Symbol == symbol && p.TargetDate >= startDate && p.TargetDate <= endDate)
                .OrderByDescending(p => p.RiskAdjustedScore)
                .ToListAsync();

            return symbolPredictions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get predictions for symbol {Symbol} from {StartDate} to {EndDate}",
                symbol, startDate, endDate);
            throw;
        }
    }

    public async Task<PredictionResult?> GetLatestPredictionAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            var latestPrediction = await context.Predictions
                .Where(p => p.Symbol == symbol)
                .OrderByDescending(p => p.TargetDate)
                .FirstOrDefaultAsync();

            return latestPrediction;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get latest prediction for symbol {Symbol}", symbol);
            throw;
        }
    }

    public async Task UpdateActualResultsAsync(string symbol, DateTime targetDate, float actualReturn,
        float actualVolatility)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            var predictions = await context.Predictions
                .Where(p => p.Symbol == symbol && p.TargetDate.Date == targetDate.Date)
                .ToListAsync();

            if (predictions.Count == 0)
            {
                logger.LogWarning("No predictions found to update for symbol {Symbol} on {TargetDate}",
                    symbol, targetDate);
                return;
            }

            foreach (var prediction in predictions)
            {
                prediction.ActualReturn = actualReturn;
                prediction.ActualVolatility = actualVolatility;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Updated actual results for {Count} predictions of symbol {Symbol} on {TargetDate}",
                predictions.Count, symbol, targetDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update actual results for symbol {Symbol} on {TargetDate}",
                symbol, targetDate);
            throw;
        }
    }

    public async Task<List<PredictionResult>> GetPredictionsForBacktestingAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var backtestingPredictions = await context.Predictions
                .Where(p => p.TargetDate >= startDate && p.TargetDate <= endDate)
                .OrderByDescending(p => p.RiskAdjustedScore)
                .ToListAsync();

            return backtestingPredictions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get predictions for backtesting from {StartDate} to {EndDate}",
                startDate, endDate);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetDataCountBySymbolAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var dataCountBySymbol = await context.RawData
                .Where(d => d.Date >= startDate && d.Date <= endDate)
                .GroupBy(d => d.Symbol)
                .Select(g => new { Symbol = g.Key, Count = g.Count() })
                .ToListAsync();

            return dataCountBySymbol.ToDictionary(x => x.Symbol, x => x.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get data count by symbol from {StartDate} to {EndDate}",
                startDate, endDate);
            throw;
        }
    }

    public async Task CleanupOldDataAsync(DateTime cutoffDate)
    {
        try
        {
            var oldDataCount = await context.RawData
                .Where(d => d.Date < cutoffDate)
                .CountAsync();

            if (oldDataCount == 0)
            {
                logger.LogInformation("No old data found before {CutoffDate} to cleanup", cutoffDate);
                return;
            }

            logger.LogInformation("Starting cleanup of {Count} old data records before {CutoffDate}",
                oldDataCount, cutoffDate);

            // Delete in batches to avoid memory issues with large datasets
            const int batchSize = 1000;
            int deletedCount = 0;

            while (true)
            {
                var batch = await context.RawData
                    .Where(d => d.Date < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any()) break;

                context.RawData.RemoveRange(batch);
                await context.SaveChangesAsync();

                deletedCount += batch.Count;
                logger.LogDebug("Deleted batch of {BatchCount} records, total deleted: {TotalDeleted}",
                    batch.Count, deletedCount);
            }

            logger.LogInformation("Completed cleanup of {DeletedCount} old data records", deletedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cleanup old data before {CutoffDate}", cutoffDate);
            throw;
        }
    }

    public async Task CleanupOldPredictionsAsync(DateTime cutoffDate)
    {
        try
        {
            var oldPredictionCount = await context.Predictions
                .Where(p => p.TargetDate < cutoffDate)
                .CountAsync();

            if (oldPredictionCount == 0)
            {
                logger.LogInformation("No old predictions found before {CutoffDate} to cleanup", cutoffDate);
                return;
            }

            logger.LogInformation("Starting cleanup of {Count} old predictions before {CutoffDate}",
                oldPredictionCount, cutoffDate);

            // Delete in batches to avoid memory issues
            const int batchSize = 1000;
            int deletedCount = 0;

            while (true)
            {
                var batch = await context.Predictions
                    .Where(p => p.TargetDate < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any()) break;

                context.Predictions.RemoveRange(batch);
                await context.SaveChangesAsync();

                deletedCount += batch.Count;
                logger.LogDebug("Deleted batch of {BatchCount} predictions, total deleted: {TotalDeleted}",
                    batch.Count, deletedCount);
            }

            logger.LogInformation("Completed cleanup of {DeletedCount} old predictions", deletedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cleanup old predictions before {CutoffDate}", cutoffDate);
            throw;
        }
    }

    public async Task<bool> HasDataForSymbolAsync(string symbol, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        try
        {
            return await context.RawData
                .AnyAsync(d => d.Symbol == symbol && d.Date.Date == date.Date);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check if data exists for symbol {Symbol} on {Date}", symbol, date);
            throw;
        }
    }


}
