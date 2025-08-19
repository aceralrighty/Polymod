using System.ComponentModel.DataAnnotations;

namespace StockPredictionService.Shared.GenericDBProperties;

public abstract class BaseTableProperties : DateableObject, IWithId
{
    [Key] public virtual Guid Id { get; set; }
}
