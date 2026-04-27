namespace Jobsy.Messages.Domain.Model.Entities;

public class MessageDto
{
    public string id { get; set; }
    public int sender_id { get; set; }
    public string content { get; set; }
    public DateTime sent_at { get; set; }
}