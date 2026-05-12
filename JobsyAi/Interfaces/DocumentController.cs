using Jobsy.JobsyAi.Application.CommandServices;
using Jobsy.JobsyAi.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.JobsyAi.Interfaces;

[ApiController]
[Route("api/document")]
public class DocumentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly IChatService _chatService;

    public DocumentController(IMediator mediator, IDocumentAnalyzer documentAnalyzer, IChatService chatService)
    {
        _mediator = mediator;
        _documentAnalyzer = documentAnalyzer;
        _chatService = chatService;
    }

    /// <summary>
    /// Analiza un CV en PDF contra una oferta laboral.
    /// Usado tanto por el reclutador (para evaluar candidatos) como por el postulante (para evaluar su propio CV).
    /// </summary>
    [Authorize]
    [HttpPost("analyze-cv")]
    public async Task<IActionResult> AnalyzeCV(
        IFormFile file,
        [FromForm] string offerTitle,
        [FromForm] string offerDescription,
        [FromForm] string? offerRequirements = null,
        [FromForm] string? viewerRole = "candidate")  // "candidate" | "recruiter"
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Archivo no válido o vacío." });

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return BadRequest(new { error = $"El archivo supera el límite de 5 MB ({file.Length / 1024.0 / 1024.0:F1} MB)." });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan archivos PDF." });

        try
        {
            using var stream = file.OpenReadStream();
            var pdfText = _documentAnalyzer.ExtractTextFromPdf(stream);

            if (string.IsNullOrWhiteSpace(pdfText))
                return BadRequest(new { error = "No se pudo extraer texto del PDF. Asegúrate de que no sea una imagen escaneada." });

            // Prompt diferente según quién lo pide
            string veredictoPos = viewerRole == "recruiter" ? "SE RECOMIENDA contratar" : "ERES APTO";
            string veredictoNeg = viewerRole == "recruiter" ? "NO SE RECOMIENDA contratar" : "NO ERES COMPETITIVO AÚN";
            string persona = viewerRole == "recruiter"
                ? "Eres un evaluador experto en recursos humanos que ayuda a RECLUTADORES a decidir si contratar a un candidato."
                : "Eres un asesor de carrera que ayuda a POSTULANTES a saber si su CV es competitivo para una oferta específica.";

            string instruccionFinal = viewerRole == "recruiter"
                ? "Habla en tercera persona sobre el candidato (él/ella). Sé directo y objetivo para ayudar al reclutador a tomar una decisión."
                : "Habla en segunda persona (tú). Sé alentador pero honesto para ayudar al postulante a mejorar su candidatura.";

            var prompt = $@"{persona}

INSTRUCCIONES CRÍTICAS:
- Analiza el CV REAL que se te proporciona. DEBES mencionar datos concretos: nombre, experiencia específica, tecnologías mencionadas, empresas, años de experiencia, educación.
- NUNCA des respuestas genéricas. Si el CV menciona Python, dilo. Si tiene 3 años de experiencia, dilo. Si estudió en UPC, dilo.
- Si el CV está vacío o no tiene información relevante, dilo claramente.
- {instruccionFinal}
- Responde ÚNICAMENTE en español.

FORMATO EXACTO DE RESPUESTA:

## Puntaje: [0-100]/100

### ✅ Fortalezas
- [menciona algo ESPECÍFICO del CV, con datos reales]
- [otra fortaleza específica]
- [otra fortaleza específica]

### ⚠️ Áreas de mejora
- [área concreta basada en lo que FALTA en el CV vs la oferta]
- [otra área concreta]

### 💡 Veredicto
[{veredictoPos} o {veredictoNeg} para este puesto. Explica en 2 oraciones con datos REALES del CV y la oferta.]

---

OFERTA LABORAL:
Título: {offerTitle}
Descripción: {offerDescription}
{(string.IsNullOrWhiteSpace(offerRequirements) ? "" : $"Requisitos: {offerRequirements}")}

CV DEL CANDIDATO (texto extraído del PDF):
{pdfText}";

            var resultado = await _chatService.SendMessageAsync(prompt);
            return Ok(new { result = resultado, pdfCharacters = pdfText.Length });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al procesar: " + ex.Message });
        }
    }

    /// <summary>
    /// Endpoint general para llamadas a la IA con prompt personalizado.
    /// Usado para: recomendar vacantes, sugerir candidatos, etc.
    /// </summary>
    [Authorize]
    [HttpPost("ask-ai")]
    public async Task<IActionResult> AskAI([FromBody] AskAIRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "El prompt no puede estar vacío." });

        try
        {
            var resultado = await _chatService.SendMessageAsync(request.Prompt);
            return Ok(new { result = resultado });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al conectar con la IA: " + ex.Message });
        }
    }

    [HttpPost("extract-text")]
    public IActionResult ExtractText(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Archivo no válido o vacío." });

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return BadRequest(new { error = "El archivo supera el límite de 5 MB." });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan archivos PDF." });

        try
        {
            using var stream = file.OpenReadStream();
            var text = _documentAnalyzer.ExtractTextFromPdf(stream);
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "No se pudo extraer texto del PDF." });
            return Ok(new { text, characters = text.Length });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Error al procesar el PDF: " + ex.Message });
        }
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeDocumentCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { result });
    }
}

public class AskAIRequest
{
    public string Prompt { get; set; } = "";
}