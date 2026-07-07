using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Domain.Exception;
using Jobsy.UserAuthentication.Domain.Model.Aggregates;
using Jobsy.UserAuthentication.Domain.Model.Commands;
using Jobsy.UserAuthentication.Domain.Model.ValueObjects;
using Jobsy.UserAuthentication.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.UserAuthentication.Application.CommandServices;

public class RegisterUserService : IRequestHandler<RegisterUserCommand, int>
{
    private readonly AppDbContext _context;

    public RegisterUserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Validación principal: verificar si el email ya existe
        var existingUser = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.email == request.email, cancellationToken);

        if (existingUser != null)
            throw new EmailAlreadyExistsException(request.email);

        if (request.role == Rol.EMPLOYER && !RucValidator.IsValidCompanyRuc(request.ruc))
            throw new ArgumentException("El RUC de empresa debe tener 11 digitos, iniciar con 20 y tener un digito verificador valido.");

        var nuevoUsuario = new User
        {
            name        = request.name,
            email       = request.email,
            password    = BCrypt.Net.BCrypt.HashPassword(request.password),
            role        = request.role,
            description = request.description,
            ruc = request.role == Rol.EMPLOYER ? request.ruc?.Trim() : null,
            cv_url = request.role == Rol.CANDIDATE ? request.cv_url : null,
            cv_pdf_base64 = request.role == Rol.CANDIDATE ? request.cv_pdf_base64 : null,
            created_at  = DateTime.UtcNow
        };

        _context.Usuarios.Add(nuevoUsuario);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EmailAlreadyExistsException(request.email);
        }

        return nuevoUsuario.id;
    }
}
