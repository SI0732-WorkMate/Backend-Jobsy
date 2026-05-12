using Jobsy.JobsyAi.Application.CommandServices;
using Jobsy.JobsyAi.Domain.Services;
using MediatR;

namespace Jobsy.JobsyAi.Application.Handlers;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, string>
{
    private readonly IChatService _chatService;

    public SendMessageHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var response = await _chatService.SendMessageAsync(request.Prompt);
        return response;
    }
}