using System.ComponentModel.DataAnnotations;
using Microsoft.ML.Data;
using TBD.Shared.GenericDBProperties;

namespace TBD.StockPredictionModule.Models.Stocks;

public class Stock : BaseTableProperties
{
    [MaxLength(10)] public string Symbol { get; set; }
    public float Open { get; set; }
    public float High { get; set; }
    public float Low { get; set; }
    public float Close { get; set; }
    public float Volume { get; set; }
    public string Date { get; set; }

    // ML.NET specific properties
    [LoadColumn(0)] public float UserId { get; set; }
    [LoadColumn(1)] public float StockId { get; set; }
    [LoadColumn(2)] public float Price { get; set; }

    [NoColumn] public override Guid Id { get; set; }
    [NoColumn] public override DateTime CreatedAt { get; set; }
    [NoColumn] public override DateTime UpdatedAt { get; set; }
}
