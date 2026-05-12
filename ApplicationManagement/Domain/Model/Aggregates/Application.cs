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
}