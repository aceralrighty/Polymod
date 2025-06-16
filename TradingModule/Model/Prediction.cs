using TBD.GenericDBProperties;

namespace TBD.TradingModule.Model;

public class Prediction : BaseTableProperties
{
    public string Symbol { get; set; }
    public DateTime DatePredicted { get; set; }
    public float PredictionReturn { get; set; }
    public float ConfidenceScore { get; set; }
    public float? ActualReturn { get; set; }
}
