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
        var employerId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(employerId))
            throw new UnauthorizedAccessException("No se pudo identificar al employer.");

        // Buscar ofertas del employer
        var offersIds = await _context.JobOffers
            .Where(o => o.employer_id == int.Parse(employerId))
            .Select(o => o.id)
            .ToListAsync(cancellationToken);

        // Buscar postulaciones solo de esas ofertas
        var applications = await _context.Applications
            .Where(a => offersIds.Contains(a.job_offer_id))
            .Select(a => new ApplicationSummaryDto
            {
                application_id = a.id,
                candidate_id = a.candidate_id,
                cv_url = a.cv_url,
                application_date = a.application_date,
                job_offer_id = a.job_offer_id
            })
            .ToListAsync(cancellationToken);

        return applications;
    }
}