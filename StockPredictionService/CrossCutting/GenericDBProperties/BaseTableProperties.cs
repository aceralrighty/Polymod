using System.ComponentModel.DataAnnotations;

namespace StockPredictionService.CrossCutting.GenericDBProperties;

public abstract class BaseTableProperties : DateableObject, IWithId
{
    [Key] public virtual Guid Id { get; set; }
}
