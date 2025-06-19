using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TBD.Shared.EntityMappers;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Load;

public class LoadCsvData
{
    public static async Task<List<RawData>> LoadRawDataAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            MissingFieldFound = null,
            BadDataFound = null // Ignore bad data instead of throwing
        };
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<RawDataMap>();

        var records = new List<RawData>();
        await foreach (var record in csv.GetRecordsAsync<RawData>())
        {
            // Skip records with invalid or missing critical data
            if (IsValidRecord(record))
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static bool IsValidRecord(RawData record)
    {
        return !string.IsNullOrWhiteSpace(record.Date) &&
               !string.IsNullOrWhiteSpace(record.Symbol) &&
               record.Close > 0; // At minimum, we need a valid close price
    }
}
