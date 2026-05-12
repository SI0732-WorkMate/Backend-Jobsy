using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.ApplicationManagement.Domain.Model.Entities;
using Jobsy.ApplicationManagement.Domain.Model.Queries;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.ApplicationManagement.Applications.QueryServices;

public class GetApplicationsByEmployerService : IRequestHandler<GetApplicationsByEmployerQuery, IEnumerable<ApplicationSummaryDto>>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public GetApplicationsByEmployerService(AppDbContext context, IHttpContextAccessor accessor)
    {
        _context = context;
        _httpContextAccessor = accessor;
    }
    
    public async Task<IEnumerable<ApplicationSummaryDto>> Handle(GetApplicationsByEmployerQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var employerId = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(employerId))
            throw new UnauthorizedAccessException("No se pudo identificar al employer.");

        // Buscar ofertas del employer
        var offersIds = await _context.JobOffers
            .Where(o => o.employer_id == int.Parse(employerId))
            .Select(o => o.id)
            .ToListAsync(cancellationToken);

        // Buscar postulaciones solo de esas ofertas, join con Usuarios para traer el nombre
        var applications = await _context.Applications
            .Where(a => offersIds.Contains(a.job_offer_id))
            .Join(_context.Usuarios,
                app => app.candidate_id,
                user => user.id,
                (app, user) => new ApplicationSummaryDto
                {
                    application_id = app.id,
                    candidate_id = app.candidate_id,
                    candidate_name = user.name,
                    cv_url = app.cv_url,
                    application_date = app.application_date,
                    job_offer_id = app.job_offer_id,
                    status = app.status
                })
            .ToListAsync(cancellationToken);

        return applications;
    }
}