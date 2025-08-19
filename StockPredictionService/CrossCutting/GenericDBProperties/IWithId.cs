namespace StockPredictionService.CrossCutting.GenericDBProperties;

internal interface IWithId
{
    Guid Id { get; set; }
}
