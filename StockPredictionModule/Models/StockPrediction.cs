using Microsoft.ML.Data;
using TBD.GenericDBProperties;

namespace TBD.StockPredictionModule.Models;

public class StockPrediction: BaseTableProperties
{
    [ColumnName("Score")]
    public float Price { get; set; }
    [ColumnName("Company Symbol")]
    public string Symbol { get; set; }

    [NoColumn]
    public Guid BatchId { get; set; }

    [NoColumn]
    public override Guid Id { get; set; }

    [NoColumn]
    public override DateTime CreatedAt { get; set; }

    [NoColumn]
    public override DateTime UpdatedAt { get; set; }

    [NoColumn]
    public override DateTime? DeletedAt { get; set; }
}
