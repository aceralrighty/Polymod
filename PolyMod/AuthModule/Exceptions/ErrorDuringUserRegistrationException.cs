namespace TBD.AuthModule.Exceptions;

public abstract class ErrorDuringUserRegistrationException : Exception
{
    protected ErrorDuringUserRegistrationException(string message) : base(message) { }
    protected ErrorDuringUserRegistrationException(string message, Exception inner) : base(message, inner) { }
}
