namespace Jobsy.JobsyAi.Domain.Model.ValueObjects;

public class Message
{
    public string Role { get; private set; } // "user" o "assistant"
    public string Content { get; private set; }
    public DateTime Timestamp { get; private set; }

    public Message(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.UtcNow;
    }
}