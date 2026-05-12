using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;

namespace Jobsy.Recruiter.JobOfferManagement.Application.CommandServices;

public class CreateJobOfferService : IRequestHandler<CreateJobOfferCommand, string>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateJobOfferService(AppDbContext context, IHttpContextAccessor accessor)
    {
        _context = context;
        _httpContextAccessor = accessor;
    }

    public async Task<string> Handle(CreateJobOfferCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("No se pudo identificar al usuario.");

        var nuevaOferta = new JobOffer
        {
            employer_id = int.Parse(userId),
            title = request.title,
            description = request.description,
            requirements = request.requirements,
            location = request.location,
            salary_range = request.salary_range
            // created_at y status ya se setean por defecto
        };

        _context.JobOffers.Add(nuevaOferta);
        await _context.SaveChangesAsync(cancellationToken);

        return nuevaOferta.id;
    }
}