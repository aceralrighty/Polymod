using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TBD.Shared.EntityMappers;

public abstract class NullableFloatConverter : SingleConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0f; // Return 0 for empty values, or you could return float.NaN
        }

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0f; // Return 0 for invalid values
    }
}
