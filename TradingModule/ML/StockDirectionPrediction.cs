using Microsoft.ML.Data;

namespace TBD.TradingModule.Model;

public class StockDirectionPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}
