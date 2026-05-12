using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.ApplicationManagement.Domain.Model.Aggregates;
using Jobsy.ApplicationManagement.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;

namespace Jobsy.ApplicationManagement.Applications.CommandServices;

public class CreateApplicationService : IRequestHandler<CreateApplicationCommand, string>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateApplicationService(AppDbContext context, IHttpContextAccessor accessor)
    {
        _context = context;
        _httpContextAccessor = accessor;
    }

    public async Task<string> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var candidateIdClaim = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var role = user?.FindFirst("role")?.Value;

        if (role != "CANDIDATE")
            throw new UnauthorizedAccessException("Solo los candidatos pueden postular.");

        var application = new Application
        {
            job_offer_id = request.job_offer_id,
            candidate_id = int.Parse(candidateIdClaim),
            cv_url = request.cv_url
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync(cancellationToken);

        return application.id;
    }
}