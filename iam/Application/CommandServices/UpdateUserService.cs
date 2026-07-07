using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.UserAuthentication.Domain.Model.Commands;
using Jobsy.UserAuthentication.Domain.Model.ValueObjects;
using Jobsy.UserAuthentication.Domain.Services;
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
        var userInDb = await _context.Usuarios
            .FindAsync(new object[] { request.User.id }, cancellationToken);

        if (userInDb == null)
            throw new KeyNotFoundException($"Usuario con ID {request.User.id} no encontrado.");

        if (userInDb.role == Rol.EMPLOYER && !RucValidator.IsValidCompanyRuc(request.User.ruc))
            throw new ArgumentException("El RUC de empresa debe tener 11 digitos, iniciar con 20 y tener un digito verificador valido.");

        // Modificar solo los campos editables sobre la entidad trackeada
        userInDb.name        = request.User.name;
        userInDb.email       = request.User.email;
        userInDb.description = request.User.description;
        userInDb.ruc = request.User.ruc?.Trim();
        userInDb.cv_url = request.User.cv_url;
        userInDb.cv_pdf_base64 = request.User.cv_pdf_base64;
        userInDb.vacancy_notifications_enabled = request.User.vacancy_notifications_enabled;
        userInDb.vacancy_notification_keywords = request.User.vacancy_notification_keywords;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
