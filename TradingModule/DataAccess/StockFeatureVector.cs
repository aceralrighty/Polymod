using TBD.GenericDBProperties;

namespace TBD.TradingModule.DataAccess;

public class StockFeatureVector : BaseTableProperties
{
    public string Symbol { get; set; }
    public DateTime Date { get; set; }

    // Price-based features
    public float PriceReturn1Day { get; set; }
    public float PriceReturn5Day { get; set; }
    public float PriceReturn20Day { get; set; }

    // Moving averages (normalized as ratios to current price)
    public float MA5Ratio { get; set; } // Current price / 5-day MA
    public float MA10Ratio { get; set; }
    public float MA20Ratio { get; set; }
    public float MA50Ratio { get; set; }

    // Technical indicators
    public float RSI { get; set; } // 0-100
    public float MACD { get; set; }
    public float MACDSignal { get; set; }
    public float BollingerPosition { get; set; } // Where price sits in BB bands

    // Volume features
    public float VolumeRatio20Day { get; set; } // Current vs 20-day avg volume
    public float VolumeRatioMA { get; set; } // Volume MA ratio

    // Volatility features
    public float Volatility20Day { get; set; } // 20-day rolling std of returns
    public float HighLowRatio { get; set; } // (High-Low)/Close

    // Market context (you'll add these later)
    public float MarketBeta { get; set; }
    public float SectorPerformance { get; set; }

    // Target variables (for training)
    public float? NextDayReturn { get; set; } // What we're predicting
    public float? NextDayVolatility { get; set; } // Risk measure

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
