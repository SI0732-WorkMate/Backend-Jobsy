using System.ComponentModel.DataAnnotations;

namespace Jobsy.InterviewManagement.Domain.Model.Aggregates;

public class Interview
{
    [Key]
    public string id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string application_id { get; set; }

    [Required]
    public int employer_id { get; set; }

    [Required]
    public int candidate_id { get; set; }

    [Required]
    public DateTime scheduled_at { get; set; }

    public int duration_minutes { get; set; } = 30;

    [Required]
    public string status { get; set; } = "scheduled"; // scheduled | completed | cancelled

    [MaxLength(500)]
    public string? notes { get; set; }

    // Para que el servicio de recordatorio (W-46) no notifique dos veces
    public bool reminder_sent { get; set; } = false;

    public DateTime created_at { get; set; } = DateTime.UtcNow;
}