using System.Globalization;
using CsvHelper;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Load;

public class LoadCsvData(StockDbContext context)
{
    public async Task<int> LoadDataAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var savedCount = 0;

        foreach (var recordBatch in csv.GetRecords<RawData>().Batch(10000))
        {
            await context.AddRangeAsync(recordBatch);
            savedCount += await context.SaveChangesAsync();
        }

        return savedCount;
    }


}
