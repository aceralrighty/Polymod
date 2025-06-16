using TBD.GenericDBProperties;

namespace TBD.TradingModule.Core.Entities;

public class ApiRequestLog : BaseTableProperties
{
    public new int Id { get; set; }
    public string ApiProvider { get; set; } // "Yahoo", "AlphaVantage", etc.
    public string RequestType { get; set; } // "HistoricalData", "Quote", etc.
    public string Symbol { get; set; }
    public DateTime RequestTime { get; set; }
    public int ResponseCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RequestCount { get; set; } = 1; // For batched requests
}
