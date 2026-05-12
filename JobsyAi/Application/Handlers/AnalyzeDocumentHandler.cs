using Jobsy.JobsyAi.Application.CommandServices;
using Jobsy.JobsyAi.Domain.Services;
using MediatR;

namespace Jobsy.JobsyAi.Application.Handlers;

public class AnalyzeDocumentHandler : IRequestHandler<AnalyzeDocumentCommand, string>
{
    private readonly IChatService _chatService;
    
    public AnalyzeDocumentHandler(IChatService chatService)
    {
        _chatService = chatService;
    }
    
    public async Task<string> Handle(AnalyzeDocumentCommand request, CancellationToken cancellationToken)
    {
        var prompt = $"{request.Objective}:\n\n{request.DocumentText}";
        var result = await _chatService.SendMessageAsync(prompt);
        return result;
    }
}