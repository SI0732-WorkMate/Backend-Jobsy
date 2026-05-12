using MediatR;

namespace Jobsy.JobsyAi.Application.CommandServices;

public class StartChatCommand : IRequest<Guid>
{
    public string Title { get; set; }
    public string ModelName { get; set; }
    
    public StartChatCommand(string title, string modelName = null)
    {
        Title = title;
        ModelName = modelName ?? "mistralai/mistral-7b-instruct";
    }
}