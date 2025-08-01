using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TBD.Shared.EntityMappers;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Load;

public class LoadCsvData
{
    private static CsvConfiguration GetCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Trim(),
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
        };
    }

    private static StreamReader CreateStreamReader(string filePath)
    {
        return new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    }

    public static async Task<List<RawData>> LoadRawDataAsync(string filePath)
    {
        using var reader = CreateStreamReader(filePath);
        using var csv = new CsvReader(reader, GetCsvConfiguration());
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
        using var reader = CreateStreamReader(filePath);
        using var csv = new CsvReader(reader, GetCsvConfiguration());
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
        using var reader = CreateStreamReader(filePath);
        using var csv = new CsvReader(reader, GetCsvConfiguration());
        csv.Context.RegisterClassMap<RawDataMap>();

        var batch = new List<RawData>(batchSize);
        var recordEnumerator = csv.GetRecordsAsync<RawData>().GetAsyncEnumerator();

        try
        {
            while (await recordEnumerator.MoveNextAsync())
            {
                var record = recordEnumerator.Current;

                if (!IsValidRecord(record))
                {
                    continue;
                }

                batch.Add(record);

                if (batch.Count < batchSize)
                {
                    continue;
                }

                // Create a copy of the batch to yield (prevents holding references)
                var batchToYield = new List<RawData>(batch);
                batch.Clear(); // Reuse the same list to reduce allocations
                yield return batchToYield;
            }

            // Return the final partial batch if any records remain
            if (batch.Count > 0)
            {
                yield return [..batch];
            }
        }
        finally
        {
            await recordEnumerator.DisposeAsync();
        }
    }

    // NEW: Get total record count without loading all data
    public static async Task<int> GetRecordCountAsync(string filePath)
    {
        using var reader = CreateStreamReader(filePath);
        using var csv = new CsvReader(reader, GetCsvConfiguration());
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
