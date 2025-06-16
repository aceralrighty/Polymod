namespace TBD.TradingModule.Orchestration.Supporting;

public class PipelineResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int DataPointsFetched { get; set; }
    public int FeaturesGenerated { get; set; }
    public int PredictionsGenerated { get; set; }
    public List<PredictionSummary> Predictions { get; set; } = new();
}
