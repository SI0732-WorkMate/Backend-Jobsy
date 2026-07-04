using System.ComponentModel.DataAnnotations;

namespace Jobsy.EvaluationManagement.Domain.Model.Aggregates;

public class SoftSkillEvaluation
{
    [Key]
    public string id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string application_id { get; set; }

    [Required]
    public int candidate_id { get; set; }

    public int overall_score { get; set; }

    [Required]
    public string skill_scores_json { get; set; } // { "comunicacion": 80, "trabajo_equipo": 65, ... }

    public DateTime completed_at { get; set; } = DateTime.UtcNow;
}