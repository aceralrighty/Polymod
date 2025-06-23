using Microsoft.ML.Data;
using TBD.GenericDBProperties;

namespace TBD.StockPredictionModule.Models;

public class RawData : BaseTableProperties
{
    [LoadColumn(0)] public string Date { get; set; }

    [LoadColumn(1)] public float Open { get; set; } = 0;
    [LoadColumn(2)] public float High { get; set; } = 0;
    [LoadColumn(3)] public float Low { get; set; } = 0;
    [LoadColumn(4)] public float Close { get; set; }
    [LoadColumn(5)] public float Volume { get; set; }
    [LoadColumn(6)] public string Symbol { get; set; }

    [NoColumn] public override Guid Id { get; set; }

    [NoColumn] public override DateTime CreatedAt { get; set; }

    [NoColumn] public override DateTime UpdatedAt { get; set; }

    [NoColumn] public override DateTime? DeletedAt { get; set; }
}
