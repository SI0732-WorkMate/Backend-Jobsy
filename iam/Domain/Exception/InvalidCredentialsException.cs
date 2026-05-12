namespace Jobsy.UserAuthentication.Domain.Exception;

public class InvalidCredentialsException : ApplicationException
{
    public InvalidCredentialsException(string message) : base(message) { }

}