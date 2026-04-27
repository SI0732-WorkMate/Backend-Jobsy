namespace Jobsy.UserAuthentication.Domain.Model.Commands;
using MediatR;


public record LoginUserCommand(string email, string password) : IRequest<string>; // Retorna el JWT
