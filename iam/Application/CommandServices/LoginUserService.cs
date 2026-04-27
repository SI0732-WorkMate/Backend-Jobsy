using Jobsy.iam.Infrastructure.Security;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Domain.Exception;
using Jobsy.UserAuthentication.Domain.Model.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.UserAuthentication.Application.CommandServices;

public class LoginUserService : IRequestHandler<LoginUserCommand, string>
{
    private readonly AppDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    
    public LoginUserService(AppDbContext context, JwtTokenGenerator tokenGenerator)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.email == request.email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.password, user.password))
            throw new InvalidCredentialsException("Credenciales incorrectas");

        var token = _tokenGenerator.GenerateToken(user);
        return token;
    }

}