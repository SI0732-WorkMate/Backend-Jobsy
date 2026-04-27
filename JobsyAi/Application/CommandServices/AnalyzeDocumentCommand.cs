using MediatR;

namespace Jobsy.JobsyAi.Application.CommandServices;

public class AnalyzeDocumentCommand : IRequest<string>
{
    public string DocumentText { get; set; }
    public string Objective { get; set; } = "Resume este documento y dime los puntos importantes";
    
    public AnalyzeDocumentCommand(string documentText, string objective = null)
    {
        DocumentText = documentText;
        if (objective != null) Objective = objective;
    }
}