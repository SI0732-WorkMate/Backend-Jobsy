using Jobsy.UserAuthentication.Domain.Model.ValueObjects;
using MediatR;

namespace Jobsy.UserAuthentication.Domain.Model.Commands;

public record RegisterUserCommand(
    string name,
    string email,
    string password,
    Rol role,
    string description,
    string? ruc = null,
    string? cv_url = null,
    string? cv_pdf_base64 = null
) : IRequest<int>;
