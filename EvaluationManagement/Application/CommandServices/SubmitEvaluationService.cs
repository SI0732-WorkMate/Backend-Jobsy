using System.IdentityModel.Tokens.Jwt;
using Jobsy.EvaluationManagement.Domain.Model.Aggregates;
using Jobsy.EvaluationManagement.Domain.Model.Commands;
using Jobsy.EvaluationManagement.Domain.Model.ValueObjects;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Newtonsoft.Json;

namespace Jobsy.EvaluationManagement.Application.CommandServices;

public class SubmitEvaluationService : IRequestHandler<SubmitEvaluationCommand, object>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SubmitEvaluationService(AppDbContext context, IHttpContextAccessor accessor)
    {
        _context = context;
        _httpContextAccessor = accessor;
    }

    public async Task<object> Handle(SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var candidateIdClaim = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(candidateIdClaim))
            throw new UnauthorizedAccessException("No se pudo identificar al candidato.");

        var candidateId = int.Parse(candidateIdClaim);

        var application = await _context.Applications.FindAsync(new object[] { request.application_id }, cancellationToken);
        if (application == null || application.candidate_id != candidateId)
            throw new UnauthorizedAccessException("No puedes completar la evaluación de esta postulación.");

        var yaExiste = _context.SoftSkillEvaluations.Any(e => e.application_id == request.application_id);
        if (yaExiste)
            throw new InvalidOperationException("Ya completaste la evaluación para esta postulación.");

        var skillScores = new Dictionary<string, List<int>>();
        var feedbackPorRespuesta = new List<object>();

        foreach (var respuesta in request.answers)
        {
            var escenario = EvaluationScenarios.Find(respuesta.scenario_id);
            if (escenario == null) continue;

            var opcion = escenario.options.FirstOrDefault(o => o.id == respuesta.option_id);
            if (opcion == null) continue;

            if (!skillScores.ContainsKey(escenario.skill))
                skillScores[escenario.skill] = new List<int>();
            skillScores[escenario.skill].Add(opcion.score);

            feedbackPorRespuesta.Add(new
            {
                scenario_id = escenario.id,
                skill_label = escenario.skill_label,
                selected_option = opcion.text,
                score = opcion.score,
                feedback = opcion.feedback
            });
        }

        var promedios = skillScores.ToDictionary(kv => kv.Key, kv => (int)Math.Round(kv.Value.Average()));
        var overallScore = promedios.Count > 0 ? (int)Math.Round(promedios.Values.Average()) : 0;

        var evaluacion = new SoftSkillEvaluation
        {
            application_id = request.application_id,
            candidate_id = candidateId,
            overall_score = overallScore,
            skill_scores_json = JsonConvert.SerializeObject(promedios)
        };

        _context.SoftSkillEvaluations.Add(evaluacion);
        await _context.SaveChangesAsync(cancellationToken);

        return new
        {
            overall_score = overallScore,
            skill_scores = promedios,
            feedback = feedbackPorRespuesta
        };
    }
}