using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jobsy.ApplicationManagement.Domain.Model.Commands;
using Jobsy.ApplicationManagement.Domain.Model.Queries;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
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
    public async Task<IActionResult> Create([FromBody] CreateApplicationCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpGet("my-applications")]
    public async Task<IActionResult> GetMyApplications()
    {
        var candidateId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        
        // LOG: devuelve info de diagnóstico si algo falla
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

    // ENDPOINT DE DIAGNÓSTICO: GET /api/applications/debug
    // Úsalo desde el navegador estando logueado para ver qué claims tiene tu token
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

        // Verificar que la oferta pertenece al empleador autenticado
        var employerId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var offer = await _context.JobOffers.FindAsync(application.job_offer_id);
        if (offer == null || offer.employer_id.ToString() != employerId)
            return Forbid();

        application.status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { application_id = id, status = application.status });
    }

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
}