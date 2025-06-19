using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Load;

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
            Date = raw.Date,
            UserId = 1,
            StockId = 1,
            Price = raw.Close,
        }).ToList();
    }
}
