using Microsoft.ML.Data;
using TBD.GenericDBProperties;

namespace TBD.StockPredictionModule.Models;

public class StockPrediction: BaseTableProperties
{
    [ColumnName("Score")] public float Price { get; set; }

    public Guid BatchId { get; set; }
}
