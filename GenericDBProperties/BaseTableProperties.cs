namespace TBD.GenericDBProperties;

public abstract class BaseTableProperties: DateableObject, IWithId
{
    public Guid Id { get; set; }
}