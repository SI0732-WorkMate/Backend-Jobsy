using Jobsy.JobsyAi.Domain.Model.ValueObjects;


namespace Jobsy.JobsyAi.Domain.Model.Entities;

public class Chat
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public AIModel Model { get; private set; }
    public List<Message> Messages { get; private set; }

    public Chat(string title, AIModel model)
    {
        Id = Guid.NewGuid();
        Title = title;
        Model = model;
        Messages = new List<Message>();
    }

    public void AddMessage(Message message)
    {
        Messages.Add(message);
    }
}
