using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Queries;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Recruiter.JobOfferManagement.Application.QueryServices;

public class GetJobOfferByIdService : IRequestHandler<GetJobOfferByIdQuery, JobOffer>
{
    private readonly AppDbContext _context;

    public GetJobOfferByIdService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JobOffer> Handle(GetJobOfferByIdQuery request, CancellationToken cancellationToken)
    {
        // Solo retorna la oferta si NO fue eliminada lógicamente
        var jobOffer = await _context.JobOffers
            .FirstOrDefaultAsync(j => j.id == request.id && !j.is_deleted, cancellationToken);

        return jobOffer ?? throw new KeyNotFoundException($"Oferta con ID {request.id} no encontrada.");
    }
}