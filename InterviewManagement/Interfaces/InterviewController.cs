using System.IdentityModel.Tokens.Jwt;
using Jobsy.InterviewManagement.Domain.Model.Commands;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.InterviewManagement.Interfaces;

[ApiController]
[Route("api/interviews")]
public class InterviewController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;

    public InterviewController(IMediator mediator, AppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpPost]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewRequest request)
    {
        try
        {
            var duracion = request.duration_minutes <= 0 ? 30 : request.duration_minutes;
            var id = await _mediator.Send(new ScheduleInterviewCommand(
                request.application_id, request.scheduled_at, duracion, request.notes));
            return Ok(new { id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpGet("my-interviews")]
    public async Task<IActionResult> GetMyInterviews()
    {
        var employerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(employerId))
            return Unauthorized();

        var interviews = await _context.Interviews
            .Where(i => i.employer_id == int.Parse(employerId))
            .OrderBy(i => i.scheduled_at)
            .ToListAsync();

        var candidateIds = interviews.Select(i => i.candidate_id).Distinct().ToList();
        var usuarios = await _context.Usuarios
            .Where(u => candidateIds.Contains(u.id))
            .ToDictionaryAsync(u => u.id);

        var applicationIds = interviews.Select(i => i.application_id).ToList();
        var applications = await _context.Applications
            .Where(a => applicationIds.Contains(a.id))
            .ToDictionaryAsync(a => a.id);

        var offerIds = applications.Values.Select(a => a.job_offer_id).Distinct().ToList();
        var offers = await _context.JobOffers
            .Where(o => offerIds.Contains(o.id))
            .ToDictionaryAsync(o => o.id);

        var result = interviews.Select(i =>
        {
            usuarios.TryGetValue(i.candidate_id, out var candidato);
            applications.TryGetValue(i.application_id, out var app);
            string? tituloOferta = null;
            if (app != null && offers.TryGetValue(app.job_offer_id, out var offer))
                tituloOferta = offer.title;

            return new
            {
                interview_id = i.id,
                application_id = i.application_id,
                candidate_id = i.candidate_id,
                candidate_name = candidato?.name ?? $"Candidato #{i.candidate_id}",
                job_title = tituloOferta ?? "Oferta",
                scheduled_at = i.scheduled_at,
                duration_minutes = i.duration_minutes,
                status = i.status,
                notes = i.notes
            };
        });

        return Ok(result);
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpGet("my-schedule")]
    public async Task<IActionResult> GetMySchedule()
    {
        var candidateId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(candidateId))
            return Unauthorized();

        var interviews = await _context.Interviews
            .Where(i => i.candidate_id == int.Parse(candidateId))
            .OrderBy(i => i.scheduled_at)
            .ToListAsync();

        var applicationIds = interviews.Select(i => i.application_id).ToList();
        var applications = await _context.Applications
            .Where(a => applicationIds.Contains(a.id))
            .ToDictionaryAsync(a => a.id);

        var offerIds = applications.Values.Select(a => a.job_offer_id).Distinct().ToList();
        var offers = await _context.JobOffers
            .Where(o => offerIds.Contains(o.id))
            .ToDictionaryAsync(o => o.id);

        var result = interviews.Select(i =>
        {
            applications.TryGetValue(i.application_id, out var app);
            string? tituloOferta = null;
            if (app != null && offers.TryGetValue(app.job_offer_id, out var offer))
                tituloOferta = offer.title;

            return new
            {
                interview_id = i.id,
                job_title = tituloOferta ?? "Oferta",
                scheduled_at = i.scheduled_at,
                duration_minutes = i.duration_minutes,
                status = i.status,
                notes = i.notes
            };
        });

        return Ok(result);
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateInterviewStatus(string id, [FromBody] UpdateInterviewStatusRequest request)
    {
        var validStatuses = new[] { "scheduled", "completed", "cancelled" };
        if (string.IsNullOrEmpty(request.Status) || !validStatuses.Contains(request.Status))
            return BadRequest(new { error = "Estado inválido. Use: scheduled, completed, cancelled" });

        var employerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var interview = await _context.Interviews.FindAsync(id);
        if (interview == null)
            return NotFound(new { error = "Entrevista no encontrada" });

        if (interview.employer_id.ToString() != employerId)
            return Forbid();

        interview.status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { interview_id = id, status = interview.status });
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpPut("{id}")]
    public async Task<IActionResult> RescheduleInterview(string id, [FromBody] RescheduleInterviewRequest request)
    {
        var employerIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(employerIdClaim)) return Unauthorized();

        var employerId = int.Parse(employerIdClaim);
        var interview = await _context.Interviews.FindAsync(id);
        if (interview == null) return NotFound(new { error = "Entrevista no encontrada" });

        if (interview.employer_id != employerId) return Forbid();
        if (interview.status != "scheduled") return BadRequest(new { error = "Solo puedes editar entrevistas programadas." });

        var duracion = request.duration_minutes <= 0 ? 30 : request.duration_minutes;
        var nuevoInicio = request.scheduled_at;
        var nuevoFin = nuevoInicio.AddMinutes(duracion);

        var otras = await _context.Interviews
            .Where(i => i.employer_id == employerId && i.status == "scheduled" && i.id != id)
            .ToListAsync();

        var haySolape = otras.Any(i =>
        {
            var inicioExistente = i.scheduled_at;
            var finExistente = i.scheduled_at.AddMinutes(i.duration_minutes);
            return nuevoInicio < finExistente && inicioExistente < nuevoFin;
        });

        if (haySolape) return BadRequest(new { error = "Ya tienes una entrevista programada en ese horario." });

        interview.scheduled_at = nuevoInicio;
        interview.duration_minutes = duracion;
        interview.notes = request.notes;
        interview.reminder_sent = false; // si se reprograma, se vuelve a habilitar el recordatorio

        await _context.SaveChangesAsync();

        try
        {
            var application = await _context.Applications.FindAsync(interview.application_id);
            var offer = application != null ? await _context.JobOffers.FindAsync(application.job_offer_id) : null;
            var contenido = $"Tu entrevista para \"{offer?.title ?? "la oferta"}\" fue reprogramada al {interview.scheduled_at:dd/MM/yyyy} a las {interview.scheduled_at:HH:mm}.";

            _context.Messages.Add(new Jobsy.Messages.Domain.Model.Aggregates.Message
            {
                sender_id = employerId,
                receiver_id = interview.candidate_id,
                content = contenido
            });
            await _context.SaveChangesAsync();
        }
        catch
        {
            // No bloquea la edición si falla la notificación
        }

        return Ok(new { interview_id = id, scheduled_at = interview.scheduled_at, duration_minutes = interview.duration_minutes, notes = interview.notes });
    }
}

public class ScheduleInterviewRequest
{
    public string application_id { get; set; }
    public DateTime scheduled_at { get; set; }
    public int duration_minutes { get; set; } = 30;
    public string? notes { get; set; }
}

public class UpdateInterviewStatusRequest
{
    public string Status { get; set; }
}

public class RescheduleInterviewRequest
{
    public DateTime scheduled_at { get; set; }
    public int duration_minutes { get; set; } = 30;
    public string? notes { get; set; }
}