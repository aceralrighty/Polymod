using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class MarketDataFetcher(
    TradingDbContext dbContext,
    ILogger<MarketDataFetcher> logger)
{
    private readonly HttpClient _httpClient = new();
    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("API_KEY");
    private const string BaseUrl = "https://www.alphavantage.co/query";
    private const int MaxRequestsPerMinute = 5;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(12);


    private async Task<string> FetchAlphaVantageAsync(string symbol)
    {
        var url = $"{BaseUrl}?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&apikey={ApiKey}";
        await _rateLimiter.WaitAsync();
        try
        {
            var response = await _httpClient.GetAsync(url);
            await Task.Delay(RequestDelay);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

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

            var json = await FetchAlphaVantageAsync(symbol);
            var data = JsonDocument.Parse(json);

            await LogApiRequestAsync("AlphaVantage", "HistoricalData", symbol, 200);

            var history = data.RootElement.GetProperty("Time Series (Daily)");

            var marketData = new List<RawMarketData>();
            foreach (var day in history.EnumerateObject())
            {
                var date = DateTime.Parse(day.Name);
                if (date < startDate || date > endDate)
                    continue;

                var values = day.Value;
                marketData.Add(new RawMarketData
                {
                    Symbol = symbol,
                    Date = date,
                    Open = decimal.Parse(values.GetProperty("1. open").GetString() ?? string.Empty),
                    High = decimal.Parse(values.GetProperty("2. high").GetString() ?? string.Empty),
                    Low = decimal.Parse(values.GetProperty("3. low").GetString() ?? string.Empty),
                    Close = decimal.Parse(values.GetProperty("4. close").GetString() ?? string.Empty),
                    AdjustedClose =
                        decimal.Parse(values.GetProperty("5. adjusted close").GetString() ?? string.Empty),
                    Volume = long.Parse(values.GetProperty("6. volume").GetString() ?? string.Empty)
                });
            }

            logger.LogInformation("Successfully fetched {Count} records for {Symbol}",
                marketData.Count, symbol);

            return marketData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch historical data for {Symbol}", symbol);
            await LogApiRequestAsync("AlphaVantage", "HistoricalData", symbol, 500, ex.Message);
            throw;
        }
        finally
        {
            await Task.Delay(RequestDelay);
            _rateLimiter.Release();
        }
    }

    // Placeholder: Re-implement using Alpha Vantage GLOBAL_QUOTE endpoint if needed
    public Task<RawMarketData> GetLatestQuoteAsync(string symbol) =>
        throw new NotImplementedException("Use GLOBAL_QUOTE from Alpha Vantage or a real-time provider.");

    private async Task<bool> CanMakeRequestAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        var recentRequests = await dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneHourAgo)
            .CountAsync(); // Count requests, not sum

        return recentRequests < MaxRequestsPerMinute;
    }

    private async Task LogApiRequestAsync(string provider, string requestType, string symbol, int responseCode,
        string? errorMessage = null)
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
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneHourAgo)
            .CountAsync();

        return Math.Max(0, MaxRequestsPerMinute - recentRequests);
    }

    public async Task<Dictionary<string, List<RawMarketData>>> GetBatchHistoricalDataAsync(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new Dictionary<string, List<RawMarketData>>();
        const int batchSize = 5; // Max 5 per minute

        for (int i = 0; i < symbols.Count; i += batchSize)
        {
            var batch = symbols.Skip(i).Take(batchSize).ToList();

            foreach (var symbol in batch)
            {
                try
                {
                    var data = await GetHistoricalDataAsync(symbol, startDate, endDate);
                    result[symbol] = data;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch data for {Symbol} in batch", symbol);
                    result[symbol] = new List<RawMarketData>();
                }
            }

            if (i + batchSize < symbols.Count)
            {
                await Task.Delay(TimeSpan.FromMinutes(1)); // Full minute delay to stay safe
            }
        }

        return result;
    }
}
