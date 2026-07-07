using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.ApplicationManagement.Domain.Model.Commands;
using Jobsy.ApplicationManagement.Domain.Model.Queries;
using Jobsy.ApplicationManagement.Applications.CommandServices;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using Jobsy.Messages.Domain.Model.Commands;
using Jobsy.Messages.Domain.Model.Aggregates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.ApplicationManagement.Interfaces;

[ApiController]
[Route("api/applications")]
public class ApplicationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;

    public ApplicationController(IMediator mediator, AppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] string job_offer_id, [FromForm] string cv_url, IFormFile? cv_pdf)
    {
        string? cvBase64 = null;

        if (cv_pdf != null && cv_pdf.Length > 0)
        {
            if (cv_pdf.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "El PDF no puede superar 5 MB." });

            if (!cv_pdf.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Solo se aceptan archivos PDF." });

            using var ms = new MemoryStream();
            await cv_pdf.CopyToAsync(ms);
            cvBase64 = Convert.ToBase64String(ms.ToArray());
        }

        string id;
        try
        {
            id = await _mediator.Send(new CreateApplicationCommand(job_offer_id, cv_url, cvBase64));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        // US016 - Si se subió el PDF, intenta calcular el Match Score automáticamente.
        if (cvBase64 != null)
        {
            try
            {
                await _mediator.Send(new CalculateMatchScoreCommand(id));
            }
            catch
            {
                // Silencioso a propósito
            }
        }

        return Ok(new { id });
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpGet("my-applications")]
    public async Task<IActionResult> GetMyApplications()
    {
        var candidateId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        
        if (string.IsNullOrEmpty(candidateId))
        {
            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Unauthorized(new { 
                error = "No se encontró claim 'sub'",
                claims_recibidos = allClaims,
                esta_autenticado = User.Identity?.IsAuthenticated
            });
        }

        var applications = await _context.Applications
            .Where(a => a.candidate_id == int.Parse(candidateId))
            .ToListAsync();

        var jobOfferIds = applications.Select(a => a.job_offer_id).Distinct().ToList();

        var jobOffers = await _context.JobOffers
            .Where(j => jobOfferIds.Contains(j.id))
            .ToDictionaryAsync(j => j.id);

        var result = applications.Select(a =>
        {
            jobOffers.TryGetValue(a.job_offer_id, out var offer);
            return new
            {
                application_id = a.id,
                job_offer_id = a.job_offer_id,
                job_title = offer?.title ?? "Oferta no encontrada",
                job_description = offer?.description ?? "",
                cv_url = a.cv_url,
                application_date = a.application_date,
                status = a.status
            };
        });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("debug")]
    public async Task<IActionResult> Debug()
    {
        var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var roleClaim = User.FindFirst("role")?.Value;
        var isCandidate = User.IsInRole("CANDIDATE");

        int? parsedId = null;
        List<object> apps = new();

        if (subClaim != null && int.TryParse(subClaim, out int uid))
        {
            parsedId = uid;
            apps = (await _context.Applications
                .Where(a => a.candidate_id == uid)
                .ToListAsync())
                .Select(a => (object)new { a.id, a.candidate_id, a.job_offer_id, a.status })
                .ToList();
        }

        return Ok(new
        {
            claims = allClaims,
            sub_claim = subClaim,
            role_claim = roleClaim,
            is_candidate_role = isCandidate,
            candidate_id_parsed = parsedId,
            applications_en_db = apps
        });
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(string id, [FromBody] UpdateApplicationStatusRequest request)
    {
        var validStatuses = new[] { "pending", "accepted", "rejected" };
        if (string.IsNullOrEmpty(request.Status) || !validStatuses.Contains(request.Status))
            return BadRequest(new { error = "Estado inválido. Use: pending, accepted, rejected" });
    
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
            return NotFound(new { error = "Postulación no encontrada" });
    
        var employerId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var offer = await _context.JobOffers.FindAsync(application.job_offer_id);
        if (offer == null || offer.employer_id.ToString() != employerId)
            return Forbid();
    
        application.status = request.Status;
    
        if (request.Status == "rejected")
        {
            application.discard_reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
            await _context.SaveChangesAsync();
    
            var contenidoNotificacion = string.IsNullOrWhiteSpace(application.discard_reason)
                ? $"Tu postulación a \"{offer.title}\" fue descartada."
                : $"Tu postulación a \"{offer.title}\" fue descartada. Motivo: {application.discard_reason}";
    
            try
            {
                Message? mensajeExistente = null;
                if (!string.IsNullOrEmpty(application.discard_message_id))
                {
                    mensajeExistente = await _context.Messages.FindAsync(application.discard_message_id);
                }
    
                if (mensajeExistente != null)
                {
                    mensajeExistente.content = contenidoNotificacion;
                    mensajeExistente.sent_at = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var nuevoMensajeId = await _mediator.Send(new EmployerSendMessageCommand(application.candidate_id, contenidoNotificacion));
                    application.discard_message_id = nuevoMensajeId;
                    await _context.SaveChangesAsync();
                }
            }
            catch (UnauthorizedAccessException)
            {
                // No bloquea el cambio de estado si la notificación falla
            }
        }
        else
        {
            application.discard_reason = null;
            await _context.SaveChangesAsync();
        }
    
        return Ok(new { application_id = id, status = application.status, discard_reason = application.discard_reason });
    }

    // =======================================================
    // NUEVOS ENDPOINTS INSERTADOS AQUÍ (MÓDULO DE RECLUTADOR)
    // =======================================================

    // US016 - Calcula (o recalcula) el Match Score.
    [Authorize(Roles = "EMPLOYER")]
    [HttpPost("{id}/calculate-match")]
    public async Task<IActionResult> CalculateMatch(string id, IFormFile? cv_pdf)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
            return NotFound(new { error = "Postulación no encontrada" });

        var employerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var offer = await _context.JobOffers.FindAsync(application.job_offer_id);
        if (offer == null || offer.employer_id.ToString() != employerId)
            return Forbid();

        if (cv_pdf != null && cv_pdf.Length > 0)
        {
            if (cv_pdf.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "El PDF no puede superar 5 MB." });

            if (!cv_pdf.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Solo se aceptan archivos PDF." });

            using var ms = new MemoryStream();
            await cv_pdf.CopyToAsync(ms);
            // Asegúrate de que la entidad Application tenga la propiedad cv_pdf_base64
            application.cv_pdf_base64 = Convert.ToBase64String(ms.ToArray());
            await _context.SaveChangesAsync();
        }

        if (string.IsNullOrEmpty(application.cv_pdf_base64))
            return BadRequest(new { error = "No hay un PDF asociado a esta postulación. Sube uno para calcular el Match Score." });

        // Corrección aquí: se captura el resultado booleano devuelto por el comando
        bool ok = await _mediator.Send(new CalculateMatchScoreCommand(id));
        if (!ok)
            return StatusCode(500, new { error = "No se pudo calcular el Match Score. Intenta de nuevo." });

        await _context.Entry(application).ReloadAsync();
        return Ok(new { application_id = id, match_score = application.match_score });
    }

    // US016 - Detalle del match: qué habilidades/requisitos coincidieron y cuáles faltan.
    [Authorize(Roles = "EMPLOYER")]
    [HttpGet("{id}/match-detail")]
    public async Task<IActionResult> GetMatchDetail(string id)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null)
            return NotFound(new { error = "Postulación no encontrada" });

        var employerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var offer = await _context.JobOffers.FindAsync(application.job_offer_id);
        if (offer == null || offer.employer_id.ToString() != employerId)
            return Forbid();

        if (string.IsNullOrEmpty(application.match_details))
            return NotFound(new { error = "Aún no se ha calculado el Match Score de este candidato." });

        return Content(application.match_details, "application/json");
    }

    // =======================================================

    [Authorize(Roles = "EMPLOYER")]
    [HttpGet("my-offers")]
    public async Task<IActionResult> GetApplicationsByEmployer()
    {
        var result = await _mediator.Send(new GetApplicationsByEmployerQuery());
        return Ok(result);
    }

    [Authorize(Roles = "EMPLOYER")]
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var employerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(employerId))
            return Unauthorized();

        var offers = await _context.JobOffers
            .Where(j => j.employer_id == int.Parse(employerId))
            .ToListAsync();

        var offerIds = offers.Select(o => o.id).ToList();

        var applicationCounts = await _context.Applications
            .Where(a => offerIds.Contains(a.job_offer_id))
            .GroupBy(a => a.job_offer_id)
            .Select(g => new { job_offer_id = g.Key, count = g.Count() })
            .ToListAsync();

        var result = offers.Select(o =>
        {
            var apps = applicationCounts.FirstOrDefault(a => a.job_offer_id == o.id);
            return new
            {
                job_offer_id = o.id,
                title = o.title,
                application_count = apps?.count ?? 0,
                status = o.status.ToString()
            };
        });

        return Ok(result);
    }
}

public class UpdateApplicationStatusRequest
{
    public string Status { get; set; }
    public string? Reason { get; set; }
}
