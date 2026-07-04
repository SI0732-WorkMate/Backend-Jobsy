using System.IdentityModel.Tokens.Jwt;
using Jobsy.EvaluationManagement.Domain.Model.Commands;
using Jobsy.EvaluationManagement.Domain.Model.ValueObjects;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Jobsy.EvaluationManagement.Interfaces;

[ApiController]
[Route("api/evaluations")]
public class EvaluationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;

    public EvaluationController(IMediator mediator, AppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpGet("scenarios")]
    public IActionResult GetScenarios()
    {
        var scenarios = EvaluationScenarios.All.Select(s => new
        {
            id = s.id,
            skill_label = s.skill_label,
            situation = s.situation,
            options = s.options.Select(o => new { id = o.id, text = o.text })
        });
        return Ok(scenarios);
    }

    // ==========================================================
    // ADICIONADO: Endpoint para obtener feedback de una respuesta
    // ==========================================================
    public record AnswerFeedbackRequest(string scenario_id, string option_id);

    [Authorize(Roles = "CANDIDATE")]
    [HttpPost("answer-feedback")]
    public IActionResult GetAnswerFeedback([FromBody] AnswerFeedbackRequest request)
    {
        var escenario = EvaluationScenarios.Find(request.scenario_id);
        var opcion = escenario?.options.FirstOrDefault(o => o.id == request.option_id);
        if (escenario == null || opcion == null)
            return NotFound(new { error = "Escenario u opción no encontrados." });

        return Ok(new { feedback = opcion.feedback, score = opcion.score, skill_label = escenario.skill_label });
    }
    // ==========================================================

    [Authorize(Roles = "CANDIDATE")]
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string application_id)
    {
        var candidateId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var evaluacion = await _context.SoftSkillEvaluations
            .FirstOrDefaultAsync(e => e.application_id == application_id && e.candidate_id.ToString() == candidateId);

        return Ok(new { completed = evaluacion != null });
    }

    [Authorize(Roles = "CANDIDATE")]
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitEvaluationCommand command)
    {
        try
        {
            var resultado = await _mediator.Send(command);
            return Ok(resultado);
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
    [HttpGet("by-application/{applicationId}")]
    public async Task<IActionResult> GetByApplication(string applicationId)
    {
        var employerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        var application = await _context.Applications.FindAsync(applicationId);
        if (application == null) return NotFound(new { error = "Postulación no encontrada" });

        var offer = await _context.JobOffers.FindAsync(application.job_offer_id);
        if (offer == null || offer.employer_id.ToString() != employerId) return Forbid();

        var evaluacion = await _context.SoftSkillEvaluations
            .FirstOrDefaultAsync(e => e.application_id == applicationId);

        if (evaluacion == null)
            return NotFound(new { error = "El candidato aún no completó la evaluación." });

        var skillScores = JsonConvert.DeserializeObject<Dictionary<string, int>>(evaluacion.skill_scores_json);

        return Ok(new
        {
            overall_score = evaluacion.overall_score,
            skill_scores = skillScores,
            completed_at = evaluacion.completed_at
        });
    }
}