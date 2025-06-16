using TBD.GenericDBProperties;

namespace TBD.TradingModule.Core.Entities;

public class PredictionResult : BaseTableProperties
{
    public int Id { get; set; }
    public string Symbol { get; set; }
    public DateTime PredictionDate { get; set; }
    public DateTime TargetDate { get; set; } // The date being predicted

    // Predictions
    public float PredictedReturn { get; set; }
    public float PredictedVolatility { get; set; }
    public float ConfidenceScore { get; set; } // 0-1
    public float RiskAdjustedScore { get; set; } // Return/Risk ratio

    // Actual results (filled in next day)
    public float? ActualReturn { get; set; }
    public float? ActualVolatility { get; set; }

    // Model metadata
    public string ModelVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
