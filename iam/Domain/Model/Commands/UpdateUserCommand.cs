using Jobsy.UserAuthentication.Domain.Model.Aggregates;
using MediatR;

namespace Jobsy.UserAuthentication.Domain.Model.Commands;

public record UpdateUserCommand(User User) : IRequest<Unit>;