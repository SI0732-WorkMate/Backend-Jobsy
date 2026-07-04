using System.ComponentModel.DataAnnotations;

namespace Jobsy.ApplicationManagement.Domain.Model.Aggregates;

public class Application
{
    [Key]
    public string id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string job_offer_id { get; set; }

    [Required]
    public int candidate_id { get; set; }

    [Required]
    public string cv_url { get; set; }

    public DateTime application_date { get; set; } = DateTime.UtcNow;

    [Required]
    public string status { get; set; } = "pending";

    // US017 - motivo opcional registrado al descartar al candidato
    [MaxLength(300)]
    public string? discard_reason { get; set; } = null;

    // US017/US018 - referencia al mensaje de notificación de descarte,
    // para editarlo en vez de duplicarlo si se descarta más de una vez
    public string? discard_message_id { get; set; } = null;
    
    // US016 - Match Score por IA
    public string? cv_pdf_base64 { get; set; } = null;
    public int? match_score { get; set; } = null;
    public string? match_details { get; set; } = null; // JSON: { score, matched_skills, missing_skills, summary }
}