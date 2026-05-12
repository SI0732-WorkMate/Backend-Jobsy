using System.ComponentModel.DataAnnotations;

namespace Jobsy.Messages.Domain.Model.Aggregates;

public class Message
{
    [Key]
    public string id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public int sender_id { get; set; }

    [Required]
    public int receiver_id { get; set; }

    [Required]
    public string content { get; set; }

    public DateTime sent_at { get; set; } = DateTime.UtcNow;
}