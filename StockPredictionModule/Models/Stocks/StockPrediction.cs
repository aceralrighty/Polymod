using System.ComponentModel.DataAnnotations;
using Microsoft.ML.Data;
using TBD.GenericDBProperties;

namespace TBD.StockPredictionModule.Models.Stocks;

public class StockPrediction : BaseTableProperties
{
    [ColumnName("Score")] public float PredictedPrice { get; set; }

    [ColumnName("Company Symbol")]
    [MaxLength(10)]
    public string Symbol { get; set; }

    [NoColumn] public Guid BatchId { get; set; }

    [NoColumn] public override Guid Id { get; set; }

    [NoColumn]
    [DisplayFormat(DataFormatString = "{0:d}")]
    public override DateTime CreatedAt { get; set; }

    [NoColumn] public override DateTime UpdatedAt { get; set; }

    [NoColumn] public override DateTime? DeletedAt { get; set; }
}
