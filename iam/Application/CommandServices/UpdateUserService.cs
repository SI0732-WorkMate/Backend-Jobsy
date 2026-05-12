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
        _context.Usuarios.Update(request.User);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}