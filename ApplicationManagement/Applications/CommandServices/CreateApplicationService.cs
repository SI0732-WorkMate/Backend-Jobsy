using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.ApplicationManagement.Domain.Model.Aggregates;
using Jobsy.ApplicationManagement.Domain.Model.Commands;
using Jobsy.Messages.Domain.Model.Aggregates;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.ValueObjects;
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

        var jobOffer = await _context.JobOffers.FindAsync(new object[] { request.job_offer_id }, cancellationToken);
        if (jobOffer == null || jobOffer.is_deleted)
            throw new KeyNotFoundException("La vacante no existe.");

        if (jobOffer.status == Status.Cerrada)
            throw new InvalidOperationException("No se puede postular a una vacante cerrada.");

        var candidateId = int.Parse(candidateIdClaim);
        var application = new Application
        {
            job_offer_id = request.job_offer_id,
            candidate_id = candidateId,
            cv_url = request.cv_url,
            cv_pdf_base64 = request.cv_pdf_base64
        };

        _context.Applications.Add(application);
        _context.Messages.Add(new Message
        {
            sender_id = candidateId,
            receiver_id = jobOffer.employer_id,
            content = $"Nueva postulacion recibida para \"{jobOffer.title}\"."
        });
        await _context.SaveChangesAsync(cancellationToken);

        return application.id;
    }
}
