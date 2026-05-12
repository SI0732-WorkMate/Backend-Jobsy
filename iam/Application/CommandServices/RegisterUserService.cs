using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Domain.Exception;
using Jobsy.UserAuthentication.Domain.Model.Aggregates;
using Jobsy.UserAuthentication.Domain.Model.Commands;
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

        var nuevoUsuario = new User
        {
            name        = request.name,
            email       = request.email,
            password    = BCrypt.Net.BCrypt.HashPassword(request.password),
            role        = request.role,
            description = request.description,
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