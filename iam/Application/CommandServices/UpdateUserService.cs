using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Domain.Model.Commands;
using MediatR;

namespace Jobsy.UserAuthentication.Application.CommandServices;

public class UpdateUserService : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly AppDbContext _context;

    public UpdateUserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userInDb = await _context.Usuarios.FindAsync(new object[] { request.User.id }, cancellationToken);
    
        if (userInDb == null)
            throw new KeyNotFoundException($"Usuario con ID {request.User.id} no encontrado.");

        // Modificar propiedades directamente sobre la entidad trackeada
        userInDb.name = request.User.name;
        userInDb.email = request.User.email;
        userInDb.description = request.User.description;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}