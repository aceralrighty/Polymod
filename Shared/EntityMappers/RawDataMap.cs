using CsvHelper.Configuration;
using TBD.StockPredictionModule.Models;

namespace TBD.Shared.EntityMappers;

public sealed class RawDataMap: ClassMap<RawData>
{
    public RawDataMap()
    {
        // Map CSV header 'date' to RawData.Date property
        Map(m => m.Date).Name("date");
        Map(m => m.Open).Name("open").TypeConverter<NullableFloatConverter>();
        Map(m => m.High).Name("high").TypeConverter<NullableFloatConverter>();
        Map(m => m.Low).Name("low").TypeConverter<NullableFloatConverter>();
        Map(m => m.Close).Name("close").TypeConverter<NullableFloatConverter>();
        Map(m => m.Volume).Name("volume").TypeConverter<NullableFloatConverter>();
        // Map CSV header 'Name' to RawData.Symbol property
        Map(m => m.Symbol).Name("Name");

        // Ignore properties not present in the CSV file
        Map(m => m.Id).Ignore();
        Map(m => m.CreatedAt).Ignore();
        Map(m => m.UpdatedAt).Ignore();
        Map(m => m.DeletedAt).Ignore();
    }
}
