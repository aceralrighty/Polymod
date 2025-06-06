namespace TBD.AddressModule.Exceptions;

public class UserStateGroupException : Exception
{
    public UserStateGroupException() : base("User State Grouping is not available")
    {
    }

    public UserStateGroupException(string message) : base(message)
    {
    }

    public UserStateGroupException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
