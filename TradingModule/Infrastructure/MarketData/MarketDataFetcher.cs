using System.Text.Json;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Core.Entities.Interfaces;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class MarketDataFetcher(
    ITradingRepository repository,
    ILogger<MarketDataFetcher> logger,
    IConfiguration configuration)
    : IDisposable
{
    private readonly HttpClient _httpClient = new();

    private readonly string _apiKey = configuration["API_KEY"] ??
                                      throw new InvalidOperationException(
                                          "API_KEY is not configured in application settings.");

    private const string BaseUrl = "https://www.alphavantage.co/query";
    private const string StockFunction = "TIME_SERIES_DAILY_ADJUSTED";
    private const int MaxRequestsPerHour = 25;
    private const int MaxRequestsPerMinute = 5;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(12);

    public async Task<List<RawMarketData>> GetAndSaveHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            logger.LogInformation("Starting stock data fetch for {Symbol}", symbol);

            // Check if data already exists using repository
            var existingData = await repository.GetMarketDataAsync(symbol, startDate, endDate);
            logger.LogInformation("Found {Count} existing records for {Symbol}", existingData.Count, symbol);

            // Check rate limits
            if (!await CanMakeRequestAsync())
            {
                throw new InvalidOperationException("Rate limit exceeded. Cannot make request at this time.");
            }

            var url = $"{BaseUrl}?function={StockFunction}&symbol={symbol}&apikey={_apiKey}";
            logger.LogInformation("Making stock API request to: {Url}", url.Replace(_apiKey, "***"));

            await _rateLimiter.WaitAsync();

            try
            {
                await LogApiRequestAsync("AlphaVantage", StockFunction, symbol, 0);

                var response = await _httpClient.GetAsync(url);
                await Task.Delay(RequestDelay);

                var jsonContent = await response.Content.ReadAsStringAsync();

                logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                logger.LogInformation("Response length: {Length} characters", jsonContent?.Length ?? 0);

                await LogApiRequestAsync("AlphaVantage", StockFunction, symbol, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("API request failed with status {StatusCode}: {Content}",
                        response.StatusCode, jsonContent);
                    await LogApiRequestAsync("AlphaVantage", StockFunction, symbol, (int)response.StatusCode,
                        jsonContent);
                    throw new HttpRequestException($"API request failed: {response.StatusCode}");
                }

                // Log response preview
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
                    await LogApiRequestAsync("AlphaVantage", StockFunction, symbol, 400, errorMessage);
                    throw new InvalidOperationException($"Alpha Vantage API error: {errorMessage}");
                }

                if (root.TryGetProperty("Note", out var noteElement))
                {
                    var note = noteElement.GetString();
                    logger.LogWarning("Alpha Vantage API note: {Note}", note);
                    if (note?.Contains("rate limit") == true)
                    {
                        await LogApiRequestAsync("AlphaVantage", StockFunction, symbol, 429, note);
                        throw new InvalidOperationException($"Rate limit exceeded: {note}");
                    }
                }

                // Parse stock price data
                if (!root.TryGetProperty("Time Series (Daily)", out var timeSeriesElement))
                {
                    logger.LogError("'Time Series (Daily)' property not found in stock response");
                    throw new InvalidOperationException(
                        "Invalid API response: missing 'Time Series (Daily)' property for stock data");
                }

                var marketDataList = new List<RawMarketData>();
                var processedCount = 0;

                foreach (var dayProperty in timeSeriesElement.EnumerateObject())
                {
                    try
                    {
                        if (!DateTime.TryParse(dayProperty.Name, out var date))
                        {
                            logger.LogWarning("Failed to parse date: {DateString}", dayProperty.Name);
                            continue;
                        }

                        // Apply date filters
                        if (date < startDate || date > endDate) continue;

                        var dayData = dayProperty.Value;

                        // Parse stock price data correctly
                        var open = decimal.Parse(dayData.GetProperty("1. open").GetString()!);
                        var high = decimal.Parse(dayData.GetProperty("2. high").GetString()!);
                        var low = decimal.Parse(dayData.GetProperty("3. low").GetString()!);
                        var close = decimal.Parse(dayData.GetProperty("4. close").GetString()!);
                        var adjustedClose = decimal.Parse(dayData.GetProperty("5. adjusted close").GetString()!);
                        var volume = long.Parse(dayData.GetProperty("6. volume").GetString()!);

                        // Check for duplicates using existing data
                        var existingRecord = existingData.FirstOrDefault(d => d.Date == date);

                        if (existingRecord != null)
                        {
                            logger.LogDebug("Stock data already exists for {Symbol} on {Date}, skipping",
                                symbol, date);
                            continue;
                        }

                        var marketData = new RawMarketData
                        {
                            Symbol = symbol,
                            Date = date,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = close,
                            AdjustedClose = adjustedClose,
                            Volume = volume
                        };

                        marketDataList.Add(marketData);
                        processedCount++;

                        logger.LogDebug("Successfully parsed stock data for {Symbol} on {Date}",
                            symbol, date);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Failed to parse individual stock data entry for {Symbol}. Element JSON: {JsonElementText}",
                            symbol, dayProperty.Value.GetRawText());
                    }
                }

                logger.LogInformation("Processed {Count} new stock records for {Symbol}", processedCount, symbol);

                // Save to database using repository
                if (marketDataList.Any())
                {
                    try
                    {
                        await repository.SaveMarketDataAsync(marketDataList);
                        logger.LogInformation("Successfully saved {Count} stock records to database for {Symbol}",
                            marketDataList.Count, symbol);
                    }
                    catch (Exception dbEx)
                    {
                        logger.LogError(dbEx, "Failed to save stock data to database for {Symbol}. Error: {Error}",
                            symbol, dbEx.Message);
                        throw;
                    }
                }
                else
                {
                    logger.LogInformation("No new stock data to save for {Symbol}", symbol);
                }

                return marketDataList;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch and save stock data for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Batch fetch for multiple symbols - called by TrainingOrchestrator
    /// </summary>
    public async Task<Dictionary<string, List<RawMarketData>>> GetBatchHistoricalDataAsync(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate)
    {
        var results = new Dictionary<string, List<RawMarketData>>();

        foreach (var symbol in symbols)
        {
            try
            {
                var data = await GetAndSaveHistoricalDataAsync(symbol, startDate, endDate);
                results[symbol] = data;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch data for symbol {Symbol}", symbol);
                results[symbol] = new List<RawMarketData>(); // Empty list for failed symbols
            }
        }

        return results;
    }

    private async Task<bool> CanMakeRequestAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

        var hourlyRequests = await repository.GetApiRequestCountAsync("AlphaVantage", oneHourAgo);
        var minuteRequests = await repository.GetApiRequestCountAsync("AlphaVantage", oneMinuteAgo);

        return hourlyRequests < MaxRequestsPerHour && minuteRequests < MaxRequestsPerMinute;
    }

    public async Task<int> GetRemainingRequestsAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var hourlyRequests = await repository.GetApiRequestCountAsync("AlphaVantage", oneHourAgo);
        return Math.Max(0, MaxRequestsPerHour - hourlyRequests);
    }

    private async Task LogApiRequestAsync(string provider, string requestType, string symbol, int responseCode,
        string? errorMessage = null)
    {
        try
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

            await repository.SaveApiRequestLogAsync(log);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log API request for {Symbol}", symbol);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _rateLimiter?.Dispose();
    }
}
