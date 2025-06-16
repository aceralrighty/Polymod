using TBD.GenericDBProperties;

namespace TBD.TradingModule.Core.Entities;

public class RawMarketData : BaseTableProperties
{
    public int MarketId { get; set; }
    public string Symbol { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal AdjustedClose { get; set; } // Accounts for splits/dividends
    public long Volume { get; set; }
}
