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
            BadDataFound = null
        };
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<RawDataMap>();

        var records = new List<RawData>();
        await foreach (var record in csv.GetRecordsAsync<RawData>())
        {
            if (IsValidRecord(record))
            {
                records.Add(record);
            }
        }

        return records;
    }

    // NEW: Streaming method to avoid large memory allocation
    public static async IAsyncEnumerable<RawData> LoadRawDataStreamingAsync(string filePath, int batchSize = 1000)
    {
        using var reader = new StreamReader(filePath);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            MissingFieldFound = null,
            BadDataFound = null
        };
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<RawDataMap>();

        await foreach (var record in csv.GetRecordsAsync<RawData>())
        {
            if (IsValidRecord(record))
            {
                yield return record;
            }
        }
    }

    public static async IAsyncEnumerable<List<RawData>> LoadRawDataBatchedAsync(string filePath, int batchSize = 1000)
    {
        using var reader = new StreamReader(filePath);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            MissingFieldFound = null,
            BadDataFound = null
        };
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<RawDataMap>();

        var batch = new List<RawData>(batchSize);

        await foreach (var record in csv.GetRecordsAsync<RawData>())
        {
            if (!IsValidRecord(record))
            {
                continue;
            }

            batch.Add(record);

            if (batch.Count < batchSize)
            {
                continue;
            }

            yield return batch;
            batch = new List<RawData>(batchSize);
        }

        // Return the final partial batch if any records remain
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    // NEW: Get total record count without loading all data
    public static async Task<int> GetRecordCountAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            MissingFieldFound = null,
            BadDataFound = null
        };
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<RawDataMap>();

        var count = 0;
        await foreach (var record in csv.GetRecordsAsync<RawData>())
        {
            if (IsValidRecord(record))
            {
                count++;
            }
        }
        return count;
    }

    private static bool IsValidRecord(RawData record)
    {
        return !string.IsNullOrWhiteSpace(record.Date) &&
               !string.IsNullOrWhiteSpace(record.Symbol) &&
               record.Close > 0;
    }
}
