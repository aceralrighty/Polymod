using System.Globalization;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Core.Entities.Interfaces;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class RawCsvData(ITradingRepository repository, ILogger<RawCsvData> logger)
{
    private async IAsyncEnumerable<RawMarketData> LoadFromCsvStreamAsync(string filePath, string symbol = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        using (var reader = new StreamReader(filePath))
        {
            // Read header
            var headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
            {
                logger.LogWarning("CSV file is empty: {FilePath}", filePath);
                yield break; // Exit if file is empty
            }

            var header = headerLine.Split(',');
            var format = DetermineFormat(header);
            logger.LogInformation("Detected CSV format: {Format} for file: {FilePath}", format, filePath);

            string? line;
            int lineNumber = 1; // Start from 1 for data lines after header
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                try
                {
                    var data = ParseCsvLine(line, format, symbol);
                    break;

                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse line {LineNumber}: {Line}", lineNumber, line);
                }
            }
        }
        logger.LogInformation("Finished streaming and parsing records from CSV: {FilePath}", filePath);
    }

    public async Task<List<RawMarketData>> LoadAndSaveFromCsvAsync(string filePath, string symbol = null)
    {
        // This method needs to collect all data from the stream if it's still intended to save all at once
        var marketData = new List<RawMarketData>();
        await foreach (var data in LoadFromCsvStreamAsync(filePath, symbol))
        {
            marketData.Add(data);
        }

        if (marketData.Count == 0)
        {
            return marketData;
        }

        await repository.SaveMarketDataAsync(marketData);
        logger.LogInformation("Saved {Count} records to database", marketData.Count);

        return marketData;
    }

    public async Task<Dictionary<string, List<RawMarketData>>> LoadFromDirectoryAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var results = new Dictionary<string, List<RawMarketData>>();
        var csvFiles = Directory.GetFiles(directoryPath, "*.csv");

        foreach (var file in csvFiles)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var symbolFromFile = ExtractSymbolFromFileName(fileName);
                var fileData = new List<RawMarketData>();
                await foreach (var data in LoadFromCsvStreamAsync(file, symbolFromFile)) // Use streaming here
                {
                    fileData.Add(data);
                }

                if (fileData.Count != 0)
                    results[fileData.First().Symbol] = fileData;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load CSV file: {FilePath}", file);
            }
        }

        return results;
    }

    public async Task<Dictionary<string, List<RawMarketData>>> LoadBatchFromSingleCsvAsync(string csvFilePath,
        DateTime startDate, DateTime endDate)
    {
        logger.LogInformation("Loading batch data from file: {FilePath}", csvFilePath);

        var allData = new List<RawMarketData>();
        await foreach (var data in LoadFromCsvStreamAsync(csvFilePath)) // Use streaming here
        {
            allData.Add(data);
        }

        var groupedData = allData
            .Where(d => d.Date >= startDate && d.Date <= endDate)
            .GroupBy(d => d.Symbol)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Date).ToList());

        if (allData.Any())
            await repository.SaveMarketDataAsync(allData);

        logger.LogInformation("Loaded data for {Count} symbols from single CSV file", groupedData.Count);
        return groupedData;
    }

    private CsvFormat DetermineFormat(string[] header)
    {
        var headerLower = header.Select(h => h.Trim().ToLower()).ToArray();

        var hasSymbol = headerLower.Contains("symbol") || headerLower.Contains("ticker") ||
                         headerLower.Contains("name");
        var hasAdjClose = headerLower.Contains("adj close") || headerLower.Contains("adjusted close") ||
                           headerLower.Contains("adjclose");

        if (headerLower is [_, _, _, _, _, _, "name"])
            return CsvFormat.SymbolAtEnd;

        return hasSymbol switch
        {
            true when hasAdjClose => CsvFormat.SymbolWithAdjClose,
            true => CsvFormat.SymbolBasic,
            _ => hasAdjClose ? CsvFormat.YahooFinance : CsvFormat.Basic
        };
    }

    private RawMarketData? ParseCsvLine(string line, CsvFormat format, string defaultSymbol)
    {
        var parts = line.Split(',');

        try
        {
            return format switch
            {
                CsvFormat.YahooFinance => ParseYahooFormat(parts, defaultSymbol),
                CsvFormat.SymbolWithAdjClose => ParseSymbolWithAdjCloseFormat(parts),
                CsvFormat.SymbolBasic => ParseSymbolBasicFormat(parts),
                CsvFormat.SymbolAtEnd => ParseSymbolAtEndFormat(parts),
                CsvFormat.Basic => ParseBasicFormat(parts, defaultSymbol),
                _ => throw new ArgumentException($"Unsupported CSV format: {format}")
            };
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Skipping line due to format specific error: {Line}", line);
            return null;
        }
    }

    private RawMarketData? ParseYahooFormat(string[] parts, string defaultSymbol)
    {
        if (parts.Length < 7)
        {
            logger.LogWarning("Yahoo Finance format requires 7 columns, but got {ColumnCount}: {Line}", parts.Length, string.Join(",", parts));
            return null;
        }

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            logger.LogWarning("Failed to parse Date: {DatePart} in Yahoo format for line: {Line}", parts[0], string.Join(",", parts)); return null;
        }

        decimal open = 0m, high = 0m, low = 0m, close = 0m, adjustedClose = 0m;
        long volume = 0L;

        if (!string.IsNullOrWhiteSpace(parts[1]) && !decimal.TryParse(parts[1], CultureInfo.InvariantCulture, out open))
        {
            logger.LogWarning("Failed to parse Open: {OpenPart} in Yahoo format for line: {Line}", parts[1], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[2]) && !decimal.TryParse(parts[2], CultureInfo.InvariantCulture, out high))
        {
            logger.LogWarning("Failed to parse High: {HighPart} in Yahoo format for line: {Line}", parts[2], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[3]) && !decimal.TryParse(parts[3], CultureInfo.InvariantCulture, out low))
        {
            logger.LogWarning("Failed to parse Low: {LowPart} in Yahoo format for line: {Line}", parts[3], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[4]) && !decimal.TryParse(parts[4], CultureInfo.InvariantCulture, out close))
        {
            logger.LogWarning("Failed to parse Close: {ClosePart} in Yahoo format for line: {Line}", parts[4], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[5]) && !decimal.TryParse(parts[5], CultureInfo.InvariantCulture, out adjustedClose))
        {
            logger.LogWarning("Failed to parse AdjustedClose: {AdjustedClosePart} in Yahoo format for line: {Line}", parts[5], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[6]) && !long.TryParse(parts[6], CultureInfo.InvariantCulture, out volume))
        {
            logger.LogWarning("Failed to parse Volume: {VolumePart} in Yahoo format for line: {Line}", parts[6], string.Join(",", parts)); return null;
        }

        return new RawMarketData
        {
            Symbol = defaultSymbol ?? "UNKNOWN",
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            AdjustedClose = adjustedClose,
            Volume = volume
        };
    }

    private RawMarketData? ParseSymbolWithAdjCloseFormat(string[] parts)
    {
        if (parts.Length < 8)
        {
            logger.LogWarning("Symbol with Adj Close format requires 8 columns, but got {ColumnCount}: {Line}", parts.Length, string.Join(",", parts));
            return null;
        }

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            logger.LogWarning("Failed to parse Date: {DatePart} in Symbol with Adj Close format for line: {Line}", parts[0], string.Join(",", parts)); return null;
        }

        decimal open = 0m, high = 0m, low = 0m, close = 0m, adjustedClose = 0m;
        long volume = 0L;

        if (!string.IsNullOrWhiteSpace(parts[2]) && !decimal.TryParse(parts[2], CultureInfo.InvariantCulture, out open))
        {
            logger.LogWarning("Failed to parse Open: {OpenPart} in Symbol with Adj Close format for line: {Line}", parts[2], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[3]) && !decimal.TryParse(parts[3], CultureInfo.InvariantCulture, out high))
        {
            logger.LogWarning("Failed to parse High: {HighPart} in Symbol with Adj Close format for line: {Line}", parts[3], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[4]) && !decimal.TryParse(parts[4], CultureInfo.InvariantCulture, out low))
        {
            logger.LogWarning("Failed to parse Low: {LowPart} in Symbol with Adj Close format for line: {Line}", parts[4], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[5]) && !decimal.TryParse(parts[5], CultureInfo.InvariantCulture, out close))
        {
            logger.LogWarning("Failed to parse Close: {ClosePart} in Symbol with Adj Close format for line: {Line}", parts[5], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[6]) && !decimal.TryParse(parts[6], CultureInfo.InvariantCulture, out adjustedClose))
        {
            logger.LogWarning("Failed to parse AdjustedClose: {AdjustedClosePart} in Symbol with Adj Close format for line: {Line}", parts[6], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[7]) && !long.TryParse(parts[7], CultureInfo.InvariantCulture, out volume))
        {
            logger.LogWarning("Failed to parse Volume: {VolumePart} in Symbol with Adj Close format for line: {Line}", parts[7], string.Join(",", parts)); return null;
        }

        return new RawMarketData
        {
            Symbol = parts[1].Trim(),
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            AdjustedClose = adjustedClose,
            Volume = volume
        };
    }

    private RawMarketData? ParseSymbolBasicFormat(string[] parts)
    {
        if (parts.Length < 7)
        {
            logger.LogWarning("Symbol basic format requires 7 columns, but got {ColumnCount}: {Line}", parts.Length, string.Join(",", parts));
            return null;
        }

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            logger.LogWarning("Failed to parse Date: {DatePart} in Symbol basic format for line: {Line}", parts[0], string.Join(",", parts)); return null;
        }

        decimal open = 0m, high = 0m, low = 0m, close = 0m;
        long volume = 0L;

        if (!string.IsNullOrWhiteSpace(parts[2]) && !decimal.TryParse(parts[2], CultureInfo.InvariantCulture, out open))
        {
            logger.LogWarning("Failed to parse Open: {OpenPart} in Symbol basic format for line: {Line}", parts[2], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[3]) && !decimal.TryParse(parts[3], CultureInfo.InvariantCulture, out high))
        {
            logger.LogWarning("Failed to parse High: {HighPart} in Symbol basic format for line: {Line}", parts[3], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[4]) && !decimal.TryParse(parts[4], CultureInfo.InvariantCulture, out low))
        {
            logger.LogWarning("Failed to parse Low: {LowPart} in Symbol basic format for line: {Line}", parts[4], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[5]) && !decimal.TryParse(parts[5], CultureInfo.InvariantCulture, out close))
        {
            logger.LogWarning("Failed to parse Close: {ClosePart} in Symbol basic format for line: {Line}", parts[5], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[6]) && !long.TryParse(parts[6], CultureInfo.InvariantCulture, out volume))
        {
            logger.LogWarning("Failed to parse Volume: {VolumePart} in Symbol basic format for line: {Line}", parts[6], string.Join(",", parts)); return null;
        }

        return new RawMarketData
        {
            Symbol = parts[1].Trim(),
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            AdjustedClose = close,
            Volume = volume
        };
    }

    private RawMarketData? ParseBasicFormat(string[] parts, string defaultSymbol)
    {
        if (parts.Length < 6)
        {
            logger.LogWarning("Basic format requires 6 columns, but got {ColumnCount}: {Line}", parts.Length, string.Join(",", parts));
            return null;
        }

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            logger.LogWarning("Failed to parse Date: {DatePart} in Basic format for line: {Line}", parts[0], string.Join(",", parts)); return null;
        }

        decimal open = 0m, high = 0m, low = 0m, close = 0m;
        long volume = 0L;

        if (!string.IsNullOrWhiteSpace(parts[1]) && !decimal.TryParse(parts[1], CultureInfo.InvariantCulture, out open))
        {
            logger.LogWarning("Failed to parse Open: {OpenPart} in Basic format for line: {Line}", parts[1], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[2]) && !decimal.TryParse(parts[2], CultureInfo.InvariantCulture, out high))
        {
            logger.LogWarning("Failed to parse High: {HighPart} in Basic format for line: {Line}", parts[2], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[3]) && !decimal.TryParse(parts[3], CultureInfo.InvariantCulture, out low))
        {
            logger.LogWarning("Failed to parse Low: {LowPart} in Basic format for line: {Line}", parts[3], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[4]) && !decimal.TryParse(parts[4], CultureInfo.InvariantCulture, out close))
        {
            logger.LogWarning("Failed to parse Close: {ClosePart} in Basic format for line: {Line}", parts[4], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[5]) && !long.TryParse(parts[5], CultureInfo.InvariantCulture, out volume))
        {
            logger.LogWarning("Failed to parse Volume: {VolumePart} in Basic format for line: {Line}", parts[5], string.Join(",", parts)); return null;
        }

        return new RawMarketData
        {
            Symbol = defaultSymbol ?? "UNKNOWN",
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            AdjustedClose = close,
            Volume = volume
        };
    }

    private RawMarketData? ParseSymbolAtEndFormat(string[] parts)
    {
        if (parts.Length < 7)
        {
            logger.LogWarning("Symbol-at-end format requires 7 columns, but got {ColumnCount}: {Line}", parts.Length, string.Join(",", parts));
            return null;
        }

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            logger.LogWarning("Failed to parse Date: {DatePart} in Symbol-at-end format for line: {Line}", parts[0], string.Join(",", parts)); return null;
        }

        decimal open = 0m, high = 0m, low = 0m, close = 0m;
        long volume = 0L;

        if (!string.IsNullOrWhiteSpace(parts[1]) && !decimal.TryParse(parts[1], CultureInfo.InvariantCulture, out open))
        {
            logger.LogWarning("Failed to parse Open: {OpenPart} in Symbol-at-end format for line: {Line}", parts[1], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[2]) && !decimal.TryParse(parts[2], CultureInfo.InvariantCulture, out high))
        {
            logger.LogWarning("Failed to parse High: {HighPart} in Symbol-at-end format for line: {Line}", parts[2], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[3]) && !decimal.TryParse(parts[3], CultureInfo.InvariantCulture, out low))
        {
            logger.LogWarning("Failed to parse Low: {LowPart} in Symbol-at-end format for line: {Line}", parts[3], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[4]) && !decimal.TryParse(parts[4], CultureInfo.InvariantCulture, out close))
        {
            logger.LogWarning("Failed to parse Close: {ClosePart} in Symbol-at-end format for line: {Line}", parts[4], string.Join(",", parts)); return null;
        }
        if (!string.IsNullOrWhiteSpace(parts[5]) && !long.TryParse(parts[5], CultureInfo.InvariantCulture, out volume))
        {
            logger.LogWarning("Failed to parse Volume: {VolumePart} in Symbol-at-end format for line: {Line}", parts[5], string.Join(",", parts)); return null;
        }

        return new RawMarketData
        {
            Symbol = parts[6].Trim(),
            Date = date,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            AdjustedClose = close,
            Volume = volume
        };
    }

    private string ExtractSymbolFromFileName(string fileName)
    {
        var parts = fileName.Split('_', '-');

        foreach (var part in parts)
        {
            if (part.Length is >= 2 and <= 5 && part.All(char.IsLetter) && part.Equals(part, StringComparison.CurrentCultureIgnoreCase))
                return part;
        }

        return fileName.ToUpper();
    }
}
