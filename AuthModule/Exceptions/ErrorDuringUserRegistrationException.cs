namespace TBD.AuthModule.Exceptions;

public class ErrorDuringUserRegistrationException : Exception
{
    public ErrorDuringUserRegistrationException() : base() { }
    public ErrorDuringUserRegistrationException(string message) : base(message) { }
    public ErrorDuringUserRegistrationException(string message, Exception inner) : base(message, inner) { }
}
