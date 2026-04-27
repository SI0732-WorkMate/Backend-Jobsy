using Jobsy.UserAuthentication.Domain.Model.Aggregates;
using MediatR;

namespace Jobsy.UserAuthentication.Domain.Model.Queries;

public record GetUserByIdQuery(int Id_Usuario) : IRequest<User>
{
    
}