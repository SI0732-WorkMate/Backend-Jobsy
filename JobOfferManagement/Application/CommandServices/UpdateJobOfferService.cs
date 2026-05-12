using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Recruiter.JobOfferManagement.Application.CommandServices;

public class UpdateJobOfferService : IRequestHandler<UpdateJobOfferCommand, Unit>
{
    private readonly AppDbContext _context;

    public UpdateJobOfferService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateJobOfferCommand request, CancellationToken cancellationToken)
    {
        var jobOffer = await _context.JobOffers
            .FirstOrDefaultAsync(j => j.id == request.id, cancellationToken);

        if (jobOffer == null)
            throw new KeyNotFoundException($"Oferta con ID {request.id} no encontrada.");

        // Actualizar campos
        jobOffer.title = request.title;
        jobOffer.description = request.description;
        jobOffer.requirements = request.requirements;
        jobOffer.location = request.location;
        jobOffer.salary_range = request.salary_range;
        jobOffer.status = request.status;

        _context.JobOffers.Update(jobOffer);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}