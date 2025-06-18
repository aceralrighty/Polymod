using Microsoft.ML.Data;

namespace TBD.StockPredictionModule.Models;

public class Stock
{
    [LoadColumn(0)]
    public float UserId { get; set; }
    [LoadColumn(1)]
    public float StockId { get; set; }
    [LoadColumn(2)]
    public float Price { get; set; }
}
