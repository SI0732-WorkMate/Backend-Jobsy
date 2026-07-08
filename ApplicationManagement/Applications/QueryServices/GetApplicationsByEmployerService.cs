using System.IdentityModel.Tokens.Jwt;
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

        var offersIds = await _context.JobOffers
            .Where(o => o.employer_id == int.Parse(employerId))
            .Select(o => o.id)
            .ToListAsync(cancellationToken);

        var applications = await _context.Applications
            .Where(a => offersIds.Contains(a.job_offer_id))
            .Join(_context.Usuarios,
                app => app.candidate_id,
                usr => usr.id,
                (app, usr) => new ApplicationSummaryDto
                {
                    application_id = app.id,
                    candidate_id = app.candidate_id,
                    candidate_name = usr.name,
                    cv_url = !string.IsNullOrWhiteSpace(app.cv_url) ? app.cv_url : usr.cv_url,
                    application_date = app.application_date,
                    job_offer_id = app.job_offer_id,
                    status = app.status,
                    match_score = app.match_score,
                    has_cv_pdf = !string.IsNullOrEmpty(app.cv_pdf_base64) || !string.IsNullOrEmpty(usr.cv_pdf_base64)
                })
            .ToListAsync(cancellationToken);

        // US016 - Ordenar por Match Score de mayor a menor (los sin calcular quedan al final)
        return applications.OrderByDescending(a => a.match_score).ToList();
    }
}
