using System.IdentityModel.Tokens.Jwt;
using Jobsy.InterviewManagement.Domain.Model.Aggregates;
using Jobsy.InterviewManagement.Domain.Model.Commands;
using Jobsy.Messages.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.InterviewManagement.Application.CommandServices;

public class ScheduleInterviewService : IRequestHandler<ScheduleInterviewCommand, string>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMediator _mediator;

    public ScheduleInterviewService(AppDbContext context, IHttpContextAccessor accessor, IMediator mediator)
    {
        _context = context;
        _httpContextAccessor = accessor;
        _mediator = mediator;
    }

    public async Task<string> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var employerIdClaim = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(employerIdClaim))
            throw new UnauthorizedAccessException("No se pudo identificar al reclutador.");

        var employerId = int.Parse(employerIdClaim);

        var application = await _context.Applications.FindAsync(new object[] { request.application_id }, cancellationToken);
        if (application == null)
            throw new KeyNotFoundException("Postulación no encontrada.");

        var offer = await _context.JobOffers.FindAsync(new object[] { application.job_offer_id }, cancellationToken);
        if (offer == null || offer.employer_id != employerId)
            throw new UnauthorizedAccessException("No puedes agendar entrevistas para esta postulación.");

        if (application.status != "accepted")
            throw new InvalidOperationException("Solo puedes agendar entrevistas con candidatos aceptados.");

        // Validar disponibilidad: que no se solape con otra entrevista activa del reclutador
        var nuevoInicio = request.scheduled_at;
        var nuevoFin = nuevoInicio.AddMinutes(request.duration_minutes);

        var entrevistasActivas = await _context.Interviews
            .Where(i => i.employer_id == employerId && i.status == "scheduled")
            .ToListAsync(cancellationToken);

        var haySolape = entrevistasActivas.Any(i =>
        {
            var inicioExistente = i.scheduled_at;
            var finExistente = i.scheduled_at.AddMinutes(i.duration_minutes);
            return nuevoInicio < finExistente && inicioExistente < nuevoFin;
        });

        if (haySolape)
            throw new InvalidOperationException("Ya tienes una entrevista programada en ese horario.");

        var interview = new Interview
        {
            application_id = request.application_id,
            employer_id = employerId,
            candidate_id = application.candidate_id,
            scheduled_at = request.scheduled_at,
            duration_minutes = request.duration_minutes,
            notes = request.notes,
            status = "scheduled"
        };

        _context.Interviews.Add(interview);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var contenido = $"Se ha programado una entrevista para \"{offer.title}\" el {interview.scheduled_at:dd/MM/yyyy} a las {interview.scheduled_at:HH:mm}.";
            await _mediator.Send(new EmployerSendMessageCommand(application.candidate_id, contenido), cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            // No bloquea la creación si falla la notificación
        }

        return interview.id;
    }
}