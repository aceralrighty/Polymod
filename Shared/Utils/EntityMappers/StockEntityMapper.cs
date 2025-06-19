using TBD.StockPredictionModule.Models;

namespace TBD.Shared.Utils.EntityMappers;

public class StockEntityMapper
{
    public List<Stock> TransformRawDataToStocks(List<RawData> rawData)
    {
        return rawData.Select(raw => new Stock
        {
            Id = Guid.NewGuid(),
            Symbol = raw.Symbol,
            Open = raw.Open,
            High = raw.High,
            Low = raw.Low,
            Close = raw.Close,
            Volume = raw.Volume,
            Date = DateTime.Parse(raw.Date).ToString("yyyy-MM-dd"),
            CreatedAt = raw.CreatedAt,
            UpdatedAt = raw.UpdatedAt,
            DeletedAt = raw.DeletedAt,
            UserId = ConvertToInt(Guid.NewGuid()),
            StockId = ConvertToInt(Guid.NewGuid()),
            Price = raw.Close,
        }).ToList();
    }

    private int ConvertToInt(Guid id)
    {
        var bytes = id.ToByteArray();
        return Math.Abs(BitConverter.ToInt32(bytes, 0));
    }
}
