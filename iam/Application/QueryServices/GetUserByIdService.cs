using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Domain.Model.Aggregates;
using Jobsy.UserAuthentication.Domain.Model.Queries;
using MediatR;

namespace Jobsy.UserAuthentication.Application.QueryServices;

public class GetUserByIdService : IRequestHandler<GetUserByIdQuery, User>
{
    private readonly AppDbContext _context;

    public GetUserByIdService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<User> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Usuarios.
            FindAsync(new object[] { request.Id_Usuario }, cancellationToken);
        return user ?? throw new KeyNotFoundException($"Usuario con ID {request.Id_Usuario} no encontrado.");
    }
}