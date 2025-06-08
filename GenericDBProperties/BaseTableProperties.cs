using System.ComponentModel.DataAnnotations;

namespace TBD.GenericDBProperties;

public abstract class BaseTableProperties : DateableObject, IWithId
{
    [Key] public Guid Id { get; set; }
}
