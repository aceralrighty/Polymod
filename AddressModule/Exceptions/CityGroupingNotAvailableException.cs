namespace TBD.AddressModule.Exceptions;

public class CityGroupingNotAvailableException : Exception
{
    public CityGroupingNotAvailableException(string message) : base(message)
    {
    }

    public CityGroupingNotAvailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
