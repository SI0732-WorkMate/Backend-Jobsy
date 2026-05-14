using Jobsy.Recruiter.JobOfferManagement.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Recruiter.JobOfferManagement.Application.CommandServices;

public class DeleteJobOfferService : IRequestHandler<DeleteJobOfferCommand, Unit>
{
    private readonly AppDbContext _context;

    public DeleteJobOfferService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteJobOfferCommand request, CancellationToken cancellationToken)
    {
        var jobOffer = await _context.JobOffers
            .FirstOrDefaultAsync(j => j.id == request.id && !j.is_deleted, cancellationToken);

        if (jobOffer == null)
            throw new KeyNotFoundException($"Oferta con ID {request.id} no encontrada.");

        // ── Eliminación LÓGICA ───────────────────────────────────────────────
        // NO se borra el registro de la BD — solo se marca como eliminado.
        // Esto preserva la trazabilidad de postulaciones y estadísticas.
        jobOffer.is_deleted = true;
        jobOffer.deleted_at = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}