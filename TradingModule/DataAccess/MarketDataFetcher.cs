using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.MarketData;

namespace TBD.TradingModule.DataAccess;

public class MarketDataFetcher(
    TradingDbContext dbContext,
    ILogger<MarketDataFetcher> logger)
{
    private readonly HttpClient _httpClient = new();
    private const string ApiKey = "";
    private const string BaseUrl = "https://api.polygon.io/v3/reference/dividends?apiKey=";
    private const int MaxRequestsPerMinute = 5;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(12);

    private async Task<List<RawMarketData>> GetHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        await _rateLimiter.WaitAsync();

        try
        {
            if (!await CanMakeRequestAsync())
            {
                throw new InvalidOperationException("Rate limit exceeded. Please wait before making more requests.");
            }

            logger.LogInformation("Fetching historical data for {Symbol} from {StartDate} to {EndDate}",
                symbol, startDate, endDate);

            var json = await

            // Log the request
            await LogApiRequestAsync("Yahoo", "HistoricalData", symbol, 200);

            var security = securities.FirstOrDefault();
            if (security == null)
            {
                logger.LogWarning("No data found for symbol {Symbol}", symbol);
                return new List<RawMarketData>();
            }

            // This mapping logic from the 'Candle' object is correct
            var marketData = security.PriceHistory.Select(candle => new RawMarketData
            {
                Symbol = symbol,
                Date = candle.DateTime,
                Open = (decimal)candle.Open,
                High = (decimal)candle.High,
                Low = (decimal)candle.Low,
                Close = (decimal)candle.Close,
                AdjustedClose = (decimal)candle.AdjustedClose,
                Volume = candle.Volume
            }).ToList();

            logger.LogInformation("Successfully fetched {Count} records for {Symbol}",
                marketData.Count, symbol);

            return marketData;
        }
        catch (Exception ex)
        {
            // It is critical to inspect the full 'ex' object here during debugging.
            logger.LogError(ex, "Failed to fetch historical data for {Symbol}", symbol);
            await LogApiRequestAsync("Yahoo", "HistoricalData", symbol, 500, ex.Message);
            throw;
        }
        finally
        {
            await Task.Delay(_requestDelay);
            _rateLimiter.Release();
        }
    }

    public async Task<RawMarketData> GetLatestQuoteAsync(string symbol)
    {
        await _rateLimiter.WaitAsync();

        try
        {
            if (!await CanMakeRequestAsync())
            {
                throw new InvalidOperationException("Rate limit exceeded. Please wait before making more requests.");
            }

            logger.LogInformation("Fetching latest quote for {Symbol}", symbol);

            // Use YahooFinanceApi wrapper for real-time quote
            var securities = await Yahoo.Symbols(symbol)
                .Fields(Field.Symbol, Field.RegularMarketPrice, Field.RegularMarketOpen,
                    Field.RegularMarketDayHigh, Field.RegularMarketDayLow,
                    Field.RegularMarketVolume, Field.RegularMarketTime, Field.RegularMarketTime)
                .QueryAsync();

            await LogApiRequestAsync("Yahoo", "Quote", symbol, 200);

            var security = securities.FirstOrDefault();

            return new RawMarketData
            {
                Symbol = symbol,
                Date = security.RegularMarketTime,
                Open = (decimal)security.RegularMarketOpen,
                High = (decimal)security.RegularMarketDayHigh,
                Low = (decimal)security.RegularMarketDayLow,
                Close = (decimal)security.RegularMarketPrice,
                AdjustedClose = (decimal)security.RegularMarketPrice, // For current day, same as close
                Volume = security.RegularMarketVolume ?? 0
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch quote for {Symbol}", symbol);
            await LogApiRequestAsync("Yahoo", "Quote", symbol, 500, ex.Message);
            throw;
        }
        finally
        {
            await Task.Delay(_requestDelay);
            _rateLimiter.Release();
        }
    }

    private async Task<bool> CanMakeRequestAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        var recentRequests = await dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "Yahoo" && log.RequestTime >= oneHourAgo)
            .SumAsync(log => log.RequestCount);

        return recentRequests < MaxRequestsPerHour;
    }

    private async Task LogApiRequestAsync(string provider, string requestType, string symbol, int responseCode,
        string errorMessage = null)
    {
        var log = new ApiRequestLog
        {
            ApiProvider = provider,
            RequestType = requestType,
            Symbol = symbol,
            RequestTime = DateTime.UtcNow,
            ResponseCode = responseCode,
            ErrorMessage = errorMessage
        };

        dbContext.ApiRequestLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> GetRemainingRequestsAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentRequests = await dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "Yahoo" && log.RequestTime >= oneHourAgo)
            .SumAsync(log => log.RequestCount);

        return Math.Max(0, MaxRequestsPerHour - recentRequests);
    }

    // Batch method for multiple symbols - more efficient
    public async Task<Dictionary<string, List<RawMarketData>>> GetBatchHistoricalDataAsync(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new Dictionary<string, List<RawMarketData>>();

        // Process in smaller batches to avoid overwhelming the API
        const int batchSize = 10;
        for (int i = 0; i < symbols.Count; i += batchSize)
        {
            var batch = symbols.Skip(i).Take(batchSize).ToList();

            var tasks = batch.Select(async symbol =>
            {
                try
                {
                    var data = await GetHistoricalDataAsync(symbol, startDate, endDate);
                    return new { Symbol = symbol, Data = data, Success = true };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch data for {Symbol} in batch", symbol);
                    return new { Symbol = symbol, Data = new List<RawMarketData>(), Success = false };
                }
            });

            var batchResults = await Task.WhenAll(tasks);

            foreach (var batchResult in batchResults)
            {
                result[batchResult.Symbol] = batchResult.Data;
            }

            // Small delay between batches
            if (i + batchSize < symbols.Count)
            {
                await Task.Delay(5000); // 5 second delay between batches
            }
        }

        return result;
    }
}
