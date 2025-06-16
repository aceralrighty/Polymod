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
    private const int MaxRequestsPerHour = 25; // Alpha Vantage free tier limit
    private const int MaxRequestsPerMinute = 5;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(12);


    private async Task<string> FetchAlphaVantageAsync(string symbol)
    {
        var url = $"{BaseUrl}?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&interval=15min&apikey={ApiKey}";
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

    public async Task<List<RawMarketData>> GetAndSaveHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            logger.LogInformation("Starting data fetch for {Symbol}", symbol);

            // Check if the API key exists
            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new InvalidOperationException("API_KEY environment variable is not set");
            }

            // Check if data already exists
            var existingData = await dbContext.RawData
                .Where(r => r.Symbol == symbol && r.Date >= startDate && r.Date <= endDate)
                .CountAsync();

            logger.LogInformation("Found {Count} existing records for {Symbol}", existingData, symbol);

            var url = $"{BaseUrl}?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&apikey={ApiKey}";
            logger.LogInformation("Making API request to: {Url}", url.Replace(ApiKey, "***"));

            var response = await _httpClient.GetAsync(url);
            var jsonContent = await response.Content.ReadAsStringAsync();

            logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
            logger.LogInformation("Response length: {Length} characters", jsonContent?.Length ?? 0);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("API request failed with status {StatusCode}: {Content}",
                    response.StatusCode, jsonContent);
                throw new HttpRequestException($"API request failed: {response.StatusCode}");
            }

            // Log first 500 characters of response for debugging
            if (jsonContent is { Length: > 0 })
            {
                var preview = jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent;
                logger.LogInformation("Response preview: {Preview}", preview);
            }

            using var document = JsonDocument.Parse(jsonContent ?? throw new InvalidOperationException());
            var root = document.RootElement;

            // Check for API errors
            if (root.TryGetProperty("Error Message", out var errorElement))
            {
                var errorMessage = errorElement.GetString();
                logger.LogError("Alpha Vantage API error: {Error}", errorMessage);
                throw new InvalidOperationException($"Alpha Vantage API error: {errorMessage}");
            }

            if (root.TryGetProperty("Note", out var noteElement))
            {
                var note = noteElement.GetString();
                logger.LogWarning("Alpha Vantage API note: {Note}", note);
                if (note?.Contains("rate limit") == true)
                {
                    throw new InvalidOperationException($"Rate limit exceeded: {note}");
                }
            }

            if (!root.TryGetProperty("Time Series (Daily)", out var timeSeriesElement))
            {
                logger.LogError("Time Series (Daily) property not found in response");
                throw new InvalidOperationException("Invalid API response: missing Time Series data");
            }

            var marketDataList = new List<RawMarketData>();
            var processedCount = 0;

            foreach (var dayProperty in timeSeriesElement.EnumerateObject())
            {
                if (!DateTime.TryParse(dayProperty.Name, out var date))
                {
                    logger.LogWarning("Failed to parse date: {DateString}", dayProperty.Name);
                    continue;
                }

                if (date < startDate || date > endDate)
                    continue;

                var values = dayProperty.Value;

                try
                {
                    var marketData = new RawMarketData
                    {
                        Symbol = symbol,
                        Date = date,
                        Open = decimal.Parse(values.GetProperty("1. open").GetString() ?? "0"),
                        High = decimal.Parse(values.GetProperty("2. high").GetString() ?? "0"),
                        Low = decimal.Parse(values.GetProperty("3. low").GetString() ?? "0"),
                        Close = decimal.Parse(values.GetProperty("4. close").GetString() ?? "0"),
                        AdjustedClose = decimal.Parse(values.GetProperty("5. adjusted close").GetString() ?? "0"),
                        Volume = long.Parse(values.GetProperty("6. volume").GetString() ?? "0")
                    };

                    marketDataList.Add(marketData);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to parse market data for {Date}", date);
                }
            }

            logger.LogInformation("Processed {Count} records for {Symbol}", processedCount, symbol);

            if (marketDataList.Any())
            {
                // Save to the database
                await dbContext.RawData.AddRangeAsync(marketDataList);
                var savedCount = await dbContext.SaveChangesAsync();
                logger.LogInformation("Saved {Count} records to database for {Symbol}", savedCount, symbol);
            }
            else
            {
                logger.LogWarning("No market data to save for {Symbol}", symbol);
            }

            return marketDataList;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch and save historical data for {Symbol}", symbol);
            throw;
        }
    }

    // Placeholder: Re-implement using Alpha Vantage GLOBAL_QUOTE endpoint if needed
    public Task<RawMarketData> GetLatestQuoteAsync(string symbol) =>
        throw new NotImplementedException("Use GLOBAL_QUOTE from Alpha Vantage or a real-time provider.");

    private async Task<bool> CanMakeRequestAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

        var hourlyRequests = await dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneHourAgo)
            .CountAsync();

        var minuteRequests = await dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneMinuteAgo)
            .CountAsync();

        return hourlyRequests < MaxRequestsPerHour && minuteRequests < MaxRequestsPerMinute;
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

        for (var i = 0; i < symbols.Count; i += batchSize)
        {
            var batch = symbols.Skip(i).Take(batchSize).ToList();

            foreach (var symbol in batch)
            {
                try
                {
                    var data = await GetAndSaveHistoricalDataAsync(symbol, startDate, endDate);
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
