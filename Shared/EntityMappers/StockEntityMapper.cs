using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;

namespace TBD.Shared.EntityMappers;

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
            UserId = HashGuid(Guid.NewGuid()),
            StockId = HashGuid(Guid.NewGuid()),
            Price = raw.Close,
        }).ToList();
    }

    private float HashGuid(Guid guid)
    {
        return Math.Abs(guid.GetHashCode()) % 100000;
    }
}
