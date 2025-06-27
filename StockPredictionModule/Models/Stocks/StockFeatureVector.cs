using Microsoft.ML.Data;

namespace TBD.StockPredictionModule.Models.Stocks;

public class StockFeatureVector
{
    public float Open { get; set; }
    public float High { get; set; }
    public float Low { get; set; }
    public float Close { get; set; } // Current day's close (used as input)
    public float Volume { get; set; }

    public float MA5 { get; set; }
    public float MA10 { get; set; }
    public float Volatility5 { get; set; }
    public float Return1D { get; set; }

    [ColumnName("NextClose")]
    public float NextClose { get; set; } // Label to predict
}
