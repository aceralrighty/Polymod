using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class DividendDataFetcher : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private const string BaseUrl = "https://www.alphavantage.co/query";
    private const string DividendFunction = "DIVIDENDS";
    private const int MaxRequestsPerHour = 25;
    private const int MaxRequestsPerMinute = 5;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(12);

    private readonly TradingDbContext _dbContext;
    private readonly ILogger<DividendDataFetcher> _logger;

    public DividendDataFetcher(
        TradingDbContext dbContext,
        ILogger<DividendDataFetcher> logger,
        IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _configuration = configuration;
        _apiKey = _configuration["API_KEY"] ??
                  throw new InvalidOperationException("API_KEY is not configured in application settings.");
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<RawDividendData>> GetAndSaveDividendDataAsync(
        string symbol,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Starting dividend data fetch for {Symbol}", symbol);

            // Check if data already exists
            var query = _dbContext.DividendData.Where(d => d.Symbol == symbol);
            if (startDate.HasValue)
                query = query.Where(d => d.ExDividendDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(d => d.ExDividendDate <= endDate.Value);

            var existingCount = await query.CountAsync();
            _logger.LogInformation("Found {Count} existing dividend records for {Symbol}", existingCount, symbol);

            // Check rate limits
            if (!await CanMakeRequestAsync())
            {
                throw new InvalidOperationException("Rate limit exceeded. Cannot make request at this time.");
            }

            var url = $"{BaseUrl}?function={DividendFunction}&symbol={symbol}&apikey={_apiKey}";
            _logger.LogInformation("Making dividend API request to: {Url}", url.Replace(_apiKey, "***"));

            await _rateLimiter.WaitAsync();

            try
            {
                // Log the API request
                await LogApiRequestAsync("AlphaVantage", DividendFunction, symbol, 0);

                var response = await _httpClient.GetAsync(url);
                await Task.Delay(RequestDelay);

                var jsonContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response length: {Length} characters", jsonContent?.Length ?? 0);

                // Update API log with actual response code
                await LogApiRequestAsync("AlphaVantage", DividendFunction, symbol, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API request failed with status {StatusCode}: {Content}",
                        response.StatusCode, jsonContent);
                    await LogApiRequestAsync("AlphaVantage", DividendFunction, symbol, (int)response.StatusCode,
                        jsonContent);
                    throw new HttpRequestException($"API request failed: {response.StatusCode}");
                }

                // Log response preview
                if (jsonContent is { Length: > 0 })
                {
                    var preview = jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent;
                    _logger.LogInformation("Response preview: {Preview}", preview);
                }

                using var document = JsonDocument.Parse(jsonContent ?? throw new InvalidOperationException());
                var root = document.RootElement;

                // Check for API errors
                if (root.TryGetProperty("Error Message", out var errorElement))
                {
                    var errorMessage = errorElement.GetString();
                    _logger.LogError("Alpha Vantage API error: {Error}", errorMessage);
                    await LogApiRequestAsync("AlphaVantage", DividendFunction, symbol, 400, errorMessage);
                    throw new InvalidOperationException($"Alpha Vantage API error: {errorMessage}");
                }

                if (root.TryGetProperty("Note", out var noteElement))
                {
                    var note = noteElement.GetString();
                    _logger.LogWarning("Alpha Vantage API note: {Note}", note);
                    if (note?.Contains("rate limit") == true)
                    {
                        await LogApiRequestAsync("AlphaVantage", DividendFunction, symbol, 429, note);
                        throw new InvalidOperationException($"Rate limit exceeded: {note}");
                    }
                }

                // Parse dividend data
                if (!root.TryGetProperty("data", out var dataArrayElement))
                {
                    _logger.LogError("'data' property not found in dividend response");
                    throw new InvalidOperationException(
                        "Invalid API response: missing 'data' property for dividend data");
                }

                var dividendList = new List<RawDividendData>();
                var processedCount = 0;

                foreach (var dividendObject in dataArrayElement.EnumerateArray())
                {
                    try
                    {
                        var dividend = ParseDividendData(dividendObject, symbol);
                        if (dividend == null) continue;

                        // Apply date filters if specified
                        if (startDate.HasValue && dividend.ExDividendDate < startDate.Value) continue;
                        if (endDate.HasValue && dividend.ExDividendDate > endDate.Value) continue;

                        // Check for duplicates
                        var existingRecord = await _dbContext.DividendData
                            .FirstOrDefaultAsync(d =>
                                d.Symbol == symbol && d.ExDividendDate == dividend.ExDividendDate);

                        if (existingRecord != null)
                        {
                            _logger.LogDebug("Dividend record already exists for {Symbol} on {Date}, skipping",
                                symbol, dividend.ExDividendDate);
                            continue;
                        }

                        dividendList.Add(dividend);
                        processedCount++;

                        _logger.LogDebug("Successfully parsed dividend for {Symbol} on {Date} with Amount: {Amount}",
                            symbol, dividend.ExDividendDate.ToShortDateString(), dividend.Amount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to parse individual dividend entry for {Symbol}. Element JSON: {JsonElementText}",
                            symbol, dividendObject.GetRawText());
                    }
                }

                _logger.LogInformation("Processed {Count} new dividend records for {Symbol}", processedCount, symbol);

                // Save to database
                if (dividendList.Any())
                {
                    try
                    {
                        await _dbContext.DividendData.AddRangeAsync(dividendList);
                        var savedCount = await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("Successfully saved {Count} dividend records to database for {Symbol}",
                            savedCount, symbol);
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Failed to save dividend data to database for {Symbol}. Error: {Error}",
                            symbol, dbEx.Message);
                        throw;
                    }
                }
                else
                {
                    _logger.LogInformation("No new dividend data to save for {Symbol}", symbol);
                }

                return dividendList;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch and save dividend data for {Symbol}", symbol);
            throw;
        }
    }

    private RawDividendData? ParseDividendData(JsonElement dividendObject, string symbol)
    {
        try
        {
            var exDateString = dividendObject.GetProperty("ex_dividend_date").GetString();
            var amountString = dividendObject.GetProperty("amount").GetString();

            if (!DateTime.TryParse(exDateString, out var exDate))
            {
                _logger.LogWarning("Failed to parse ex_dividend_date: {DateString}", exDateString);
                return null;
            }

            if (!decimal.TryParse(amountString, out var amount))
            {
                _logger.LogWarning("Failed to parse dividend amount: {AmountString}", amountString);
                return null;
            }

            var dividend = new RawDividendData { Symbol = symbol, ExDividendDate = exDate, Amount = amount };

            // Try to parse optional fields if they exist
            if (dividendObject.TryGetProperty("payment_date", out var paymentDateElement))
            {
                var paymentDateString = paymentDateElement.GetString();
                if (DateTime.TryParse(paymentDateString, out var paymentDate))
                {
                    dividend.PaymentDate = paymentDate;
                }
            }

            if (dividendObject.TryGetProperty("record_date", out var recordDateElement))
            {
                var recordDateString = recordDateElement.GetString();
                if (DateTime.TryParse(recordDateString, out var recordDate))
                {
                    dividend.RecordDate = recordDate;
                }
            }

            if (dividendObject.TryGetProperty("declaration_date", out var declarationDateElement))
            {
                var declarationDateString = declarationDateElement.GetString();
                if (DateTime.TryParse(declarationDateString, out var declarationDate))
                {
                    dividend.DeclarationDate = declarationDate;
                }
            }

            return dividend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing dividend data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<Dictionary<string, List<RawDividendData>>> GetBatchDividendDataAsync(
        List<string> symbols,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var result = new Dictionary<string, List<RawDividendData>>();
        const int batchSize = 5;

        for (var i = 0; i < symbols.Count; i += batchSize)
        {
            var batch = symbols.Skip(i).Take(batchSize).ToList();

            foreach (var symbol in batch)
            {
                try
                {
                    var data = await GetAndSaveDividendDataAsync(symbol, startDate, endDate);
                    result[symbol] = data;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch dividend data for {Symbol} in batch", symbol);
                    result[symbol] = new List<RawDividendData>();
                }
            }

            if (i + batchSize < symbols.Count)
            {
                _logger.LogInformation("Waiting 1 minute before next batch to respect rate limits...");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        return result;
    }

    private async Task<bool> CanMakeRequestAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);

        var hourlyRequests = await _dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneHourAgo)
            .CountAsync();

        var minuteRequests = await _dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneMinuteAgo)
            .CountAsync();

        return hourlyRequests < MaxRequestsPerHour && minuteRequests < MaxRequestsPerMinute;
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

            _dbContext.ApiRequestLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log API request for {Symbol}", symbol);
        }
    }

    public async Task<int> GetRemainingRequestsAsync()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentRequests = await _dbContext.ApiRequestLogs
            .Where(log => log.ApiProvider == "AlphaVantage" && log.RequestTime >= oneHourAgo)
            .CountAsync();

        return Math.Max(0, MaxRequestsPerHour - recentRequests);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _rateLimiter?.Dispose();
    }
}
