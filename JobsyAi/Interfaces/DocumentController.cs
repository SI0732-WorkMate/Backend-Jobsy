using Jobsy.JobsyAi.Application.CommandServices;
using Jobsy.JobsyAi.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.JobsyAi.Interfaces;

[ApiController]
[Route("api/document")]
public class DocumentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    
    
    public DocumentController(IMediator mediator, IDocumentAnalyzer documentAnalyzer)
    {
        _mediator = mediator;
        _documentAnalyzer = documentAnalyzer;
    }

    
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeDocumentCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { result });
    }
    
    [HttpPost("analyze-pdf")]
    public async Task<IActionResult> AnalyzePdf(IFormFile file, [FromQuery] string objective = "Resume el contenido del documento")
    {
        if (file == null || file.Length == 0)
            return BadRequest("Archivo no válido");

        using var stream = file.OpenReadStream();
        var text = _documentAnalyzer.ExtractTextFromPdf(stream);

        var command = new AnalyzeDocumentCommand(text, objective);
        var result = await _mediator.Send(command);

        return Ok(new { result });
    }
}