using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData;

public static class MarketDataExtensions
{
    public static Dictionary<string, List<RawMarketData>> ToMarketDataDictionary(
         List<RawMarketData> rawData)
    {
        return rawData
            .GroupBy(d => d.Symbol)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Date).ToList());
    }

    public static List<RawMarketData> ValidateAndClean(this List<RawMarketData> data, ILogger logger)
    {
        var validData = new List<RawMarketData>();

        foreach (var record in data)
        {
            if (record.Open <= 0 || record.High <= 0 || record.Low <= 0 || record.Close <= 0)
            {
                logger.LogWarning("Invalid price data for {Symbol} on {Date}", record.Symbol, record.Date);
                continue;
            }

            if (record.High < record.Low)
            {
                logger.LogWarning("High < Low for {Symbol} on {Date}", record.Symbol, record.Date);
                continue;
            }

            if (record.Volume < 0)
            {
                logger.LogWarning("Negative volume for {Symbol} on {Date}", record.Symbol, record.Date);
                record.Volume = 0;
            }

            validData.Add(record);
        }

        return validData;
    }
}
