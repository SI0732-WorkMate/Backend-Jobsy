using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jobsy.Recruiter.JobOfferManagement.Domain.Model.ValueObjects;
using Jobsy.UserAuthentication.Domain.Model.Aggregates;

namespace Jobsy.Recruiter.JobOfferManagement.Domain.Model.Aggregates;

public class JobOffer
{
    [Key]
    public string id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public int employer_id { get; set; }

    [Required, StringLength(60)]
    public string title { get; set; }

    [Required]
    public string description { get; set; }

    [Required]
    public string requirements { get; set; }

    [Required, StringLength(100)]
    public string location { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal salary_range { get; set; }

    public DateTime created_at { get; set; } = DateTime.UtcNow;

    [Required]
    public Status status { get; set; } = Status.Activa;

    // ── Eliminación lógica ───────────────────────────────────────────────────
    // En vez de borrar el registro físicamente, se marca como eliminado.
    // Esto preserva la trazabilidad de postulaciones y auditoría.
    public bool is_deleted { get; set; } = false;

    public DateTime? deleted_at { get; set; } = null;
}