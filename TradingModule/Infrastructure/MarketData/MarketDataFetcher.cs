using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class MarketDataFetcher : IDisposable // Implement IDisposable for HttpClient
{
    private readonly IConfiguration _configuration; // Make it non-static
    private readonly HttpClient _httpClient; // No longer static, initialized in constructor or as field
    private readonly string _apiKey; // Make it non-static and not nullable here

    private const string BaseUrl = "https://www.alphavantage.co/query";
    private const string GlobalFunction = "DIVIDENDS";
    private const int MaxRequestsPerHour = 25; // Alpha Vantage free tier limit
    private const int MaxRequestsPerMinute = 5;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(12); // This can remain static

    private readonly TradingDbContext dbContext; // Fields to store injected services
    private readonly ILogger<MarketDataFetcher> logger; // Fields to store injected services


    // Constructor with injected services
    public MarketDataFetcher(
        TradingDbContext dbContext,
        ILogger<MarketDataFetcher> logger,
        IConfiguration configuration) // Inject IConfiguration
    {
        _httpClient = new HttpClient(); // Initialize HttpClient here
        _configuration = configuration;
        // Read API_KEY from the injected configuration here
        _apiKey = _configuration["API_KEY"] ??
                  throw new InvalidOperationException("API_KEY is not configured in application settings.");
        this.dbContext = dbContext; // Assign dbContext
        this.logger = logger; // Assign logger
    }


    private async Task<string> FetchAlphaVantageAsync(string symbol)
    {
        // Use the non-static _apiKey field
        var url = $"{BaseUrl}?function={GlobalFunction}S&symbol={symbol}&interval=15min&apikey={_apiKey}";
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

            // This check is now redundant because the constructor will throw if _apiKey is null
            // but you can keep it if you want an explicit check here.
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException(
                    "API_KEY is not set (internal check). This should not happen if configured correctly).");
            }

            // Check if data already exists
            var existingData = await dbContext.RawData
                .Where(r => r.Symbol == symbol && r.Date >= startDate && r.Date <= endDate)
                .CountAsync();

            logger.LogInformation("Found {Count} existing records for {Symbol}", existingData, symbol);

            var url = $"{BaseUrl}?function={GlobalFunction}&symbol={symbol}&apikey={_apiKey}";
            logger.LogInformation("Making API request to: {Url}", url.Replace(_apiKey, "***"));

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

            // IMPORTANT: The root property name is "data" for dividends, not the function name itself.
            // Ensure this matches the JSON preview you provided.
            if (!root.TryGetProperty("data",
                    out var dataArrayElement)) // Renamed timeSeriesElement to dataArrayElement for clarity
            {
                logger.LogError($"'data' property not found in response for {GlobalFunction} data.");
                throw new InvalidOperationException(
                    $"Invalid API response: missing 'data' property for {GlobalFunction} data.");
            }

            var marketDataList = new List<RawMarketData>();
            var processedCount = 0;

            // Iterate through the array of dividend objects
            foreach (var dividendObject in
                     dataArrayElement.EnumerateArray()) // Changed dayProperty to dividendObject. This is correct.
            {
                try
                {
                    // Correctly extract the ex_dividend_date and amount from the current dividendObject
                    var exDateString = dividendObject.GetProperty("ex_dividend_date").GetString();
                    var amountString = dividendObject.GetProperty("amount").GetString();

                    if (!DateTime.TryParse(exDateString, out var date))
                    {
                        logger.LogWarning("Failed to parse ex_dividend_date: {DateString}. Skipping entry.",
                            exDateString);
                        continue; // Skip this entry if date cannot be parsed
                    }

                    if (date < startDate || date > endDate)
                        continue;

                    if (!decimal.TryParse(amountString, out var amount))
                    {
                        logger.LogWarning("Failed to parse dividend amount: {AmountString}. Skipping entry.",
                            amountString);
                        continue; // Skip if amount cannot be parsed
                    }

                    // --- IMPORTANT DATA MAPPING ---
                    // RawMarketData is designed for stock price data (Open, High, Low, Close, Volume).
                    // Dividend data (ex_dividend_date, amount, payment_date, etc.) doesn't fit directly.
                    //
                    // RECOMMENDATION: Create a new entity (e.g., 'RawDividendData') to store dividend specific fields.
                    // For now, if you MUST use RawMarketData, here's an arbitrary mapping:
                    var marketData = new RawMarketData
                    {
                        Symbol = symbol,
                        Date = date, // Map ex_dividend_date to Date
                        Open = 0, // Set to 0 or null as no direct equivalent
                        High = 0, // Set to 0 or null
                        Low = 0, // Set to 0 or null
                        Close = amount, // Map dividend 'amount' to 'Close'
                        AdjustedClose = amount, // And 'AdjustedClose'
                        Volume = 0 // Set to 0 or null
                    };

                    marketDataList.Add(marketData);
                    processedCount++;

                    logger.LogInformation("Successfully parsed dividend for {Symbol} on {Date} with Amount: {Amount}",
                        symbol, date.ToShortDateString(), amount);
                }
                catch (Exception ex)
                {
                    // Log the full JSON of the problematic element for debugging
                    logger.LogError(ex,
                        "Failed to parse individual dividend entry for {Symbol}. Element JSON: {JsonElementText}",
                        symbol, dividendObject.GetRawText());
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

    // Dispose HttpClient to release resources
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
