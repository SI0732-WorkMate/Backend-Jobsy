namespace Jobsy.UserAuthentication.Domain.Exception;

public class EmailAlreadyExistsException : ApplicationException
{
    public EmailAlreadyExistsException(string email)
        : base($"Ya existe un usuario con el email {email}") { }
}