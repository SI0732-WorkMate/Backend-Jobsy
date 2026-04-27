namespace Jobsy.ApplicationManagement.Domain.Model.Entities;

public class ApplicationSummaryDto
{
    public string application_id { get; set; }
    public int candidate_id { get; set; }
    public string cv_url { get; set; }
    public DateTime application_date { get; set; }
    public string job_offer_id { get; set; }
}