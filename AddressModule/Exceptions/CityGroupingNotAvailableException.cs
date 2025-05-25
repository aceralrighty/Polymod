namespace TBD.AddressModule.Exceptions;

public class CityGroupingNotAvailableException: Exception
{
    public CityGroupingNotAvailableException() : base("City Grouping is not available")
    {
    }

    public CityGroupingNotAvailableException(string message) : base(message)
    {
    }

    public CityGroupingNotAvailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}