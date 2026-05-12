using MediatR;

namespace Jobsy.JobsyAi.Application.CommandServices;

public class SendMessageCommand : IRequest<string>
{
    public string Prompt { get; set; }

    public SendMessageCommand(string prompt)
    {
        Prompt = prompt;
    }
}