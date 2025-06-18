using System.Globalization;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Core.Entities.Interfaces;

namespace TBD.TradingModule.Infrastructure.MarketData;

public class RawCsvData(ITradingRepository repository, ILogger<RawCsvData> logger)
{
    private async Task<List<RawMarketData>> LoadFromCsvAsync(string filePath, string symbol = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        var marketDataList = new List<RawMarketData>();
        var lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length == 0)
        {
            logger.LogWarning("CSV file is empty: {FilePath}", filePath);
            return marketDataList;
        }

        var header = lines[0].Split(',');
        var format = DetermineFormat(header);
        logger.LogInformation("Detected CSV format: {Format} for file: {FilePath}", format, filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var data = ParseCsvLine(lines[i], format, symbol);
                marketDataList.Add(data);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse line {LineNumber}: {Line}", i + 1, lines[i]);
            }
        }

        logger.LogInformation("Successfully parsed {Count} records from CSV", marketDataList.Count);
        return marketDataList;
    }

    public async Task<List<RawMarketData>> LoadAndSaveFromCsvAsync(string filePath, string symbol = null)
    {
        var marketData = await LoadFromCsvAsync(filePath, symbol);

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
                var data = await LoadFromCsvAsync(file, symbolFromFile);

                if (data.Count != 0)
                    results[data.First().Symbol] = data;
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
        var allData = await LoadFromCsvAsync(csvFilePath);
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

    private RawMarketData ParseCsvLine(string line, CsvFormat format, string defaultSymbol)
    {
        var parts = line.Split(',');

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

    private RawMarketData ParseYahooFormat(string[] parts, string defaultSymbol)
    {
        if (parts.Length < 7)
            throw new ArgumentException("Yahoo Finance format requires 7 columns");

        return new RawMarketData
        {
            Symbol = defaultSymbol ?? "UNKNOWN",
            Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
            Open = decimal.Parse(parts[1], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Close = decimal.Parse(parts[4], CultureInfo.InvariantCulture),
            AdjustedClose = decimal.Parse(parts[5], CultureInfo.InvariantCulture),
            Volume = long.Parse(parts[6], CultureInfo.InvariantCulture)
        };
    }

    private RawMarketData ParseSymbolWithAdjCloseFormat(string[] parts)
    {
        if (parts.Length < 8)
            throw new ArgumentException("Symbol with Adj Close format requires 8 columns");

        return new RawMarketData
        {
            Symbol = parts[1].Trim(),
            Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
            Open = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[4], CultureInfo.InvariantCulture),
            Close = decimal.Parse(parts[5], CultureInfo.InvariantCulture),
            AdjustedClose = decimal.Parse(parts[6], CultureInfo.InvariantCulture),
            Volume = long.Parse(parts[7], CultureInfo.InvariantCulture)
        };
    }

    private RawMarketData ParseSymbolBasicFormat(string[] parts)
    {
        if (parts.Length < 7)
            throw new ArgumentException("Symbol basic format requires 7 columns");

        var close = decimal.Parse(parts[5], CultureInfo.InvariantCulture);

        return new RawMarketData
        {
            Symbol = parts[1].Trim(),
            Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
            Open = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[4], CultureInfo.InvariantCulture),
            Close = close,
            AdjustedClose = close,
            Volume = long.Parse(parts[6], CultureInfo.InvariantCulture)
        };
    }

    private RawMarketData ParseBasicFormat(string[] parts, string defaultSymbol)
    {
        if (parts.Length < 6)
            throw new ArgumentException("Basic format requires 6 columns");

        var close = decimal.Parse(parts[4], CultureInfo.InvariantCulture);

        return new RawMarketData
        {
            Symbol = defaultSymbol ?? "UNKNOWN",
            Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
            Open = decimal.Parse(parts[1], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Close = close,
            AdjustedClose = close,
            Volume = long.Parse(parts[5], CultureInfo.InvariantCulture)
        };
    }

    private RawMarketData ParseSymbolAtEndFormat(string[] parts)
    {
        if (parts.Length < 7)
            throw new ArgumentException("Symbol-at-end format requires 7 columns");

        var close = decimal.Parse(parts[4], CultureInfo.InvariantCulture);

        return new RawMarketData
        {
            Symbol = parts[6].Trim(),
            Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
            Open = decimal.Parse(parts[1], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Close = close,
            AdjustedClose = close,
            Volume = long.Parse(parts[5], CultureInfo.InvariantCulture)
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
