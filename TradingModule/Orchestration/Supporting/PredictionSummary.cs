namespace TBD.TradingModule.Orchestration.Supporting;

public class PredictionSummary
{
    public string Symbol { get; set; } = string.Empty;
    public float PredictedReturn { get; set; }
    public float ConfidenceScore { get; set; }
    public float RiskAdjustedScore { get; set; }
    public DateTime PredictionDate { get; set; }
    public DateTime TargetDate { get; set; }
}
