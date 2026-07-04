using Jobsy.ApplicationManagement.Domain.Model.Commands;
using Jobsy.JobsyAi.Domain.Services;
using Jobsy.Shared.Infrastructure.Persistencia.Configuration;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Jobsy.ApplicationManagement.Applications.CommandServices;

public class CalculateMatchScoreService : IRequestHandler<CalculateMatchScoreCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IDocumentAnalyzer _documentAnalyzer;
    private readonly IChatService _chatService;

    public CalculateMatchScoreService(AppDbContext context, IDocumentAnalyzer documentAnalyzer, IChatService chatService)
    {
        _context = context;
        _documentAnalyzer = documentAnalyzer;
        _chatService = chatService;
    }

    public async Task<bool> Handle(CalculateMatchScoreCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications.FindAsync(new object[] { request.application_id }, cancellationToken);
        if (application == null || string.IsNullOrEmpty(application.cv_pdf_base64))
            return false;

        var offer = await _context.JobOffers.FindAsync(new object[] { application.job_offer_id }, cancellationToken);
        if (offer == null)
            return false;

        byte[] pdfBytes;
        try
        {
            pdfBytes = Convert.FromBase64String(application.cv_pdf_base64);
        }
        catch
        {
            return false;
        }

        string textoPdf;
        using (var ms = new MemoryStream(pdfBytes))
        {
            textoPdf = _documentAnalyzer.ExtractTextFromPdf(ms);
        }

        if (string.IsNullOrWhiteSpace(textoPdf))
            return false;

        var prompt = $@"Eres un sistema que compara un CV contra los requisitos de una vacante y devuelve ÚNICAMENTE un JSON válido, sin texto adicional, sin markdown, sin explicaciones.

FORMATO EXACTO (respeta los nombres de las claves):
{{
  ""score"": <número entero entre 0 y 100>,
  ""matched_skills"": [""habilidad o requisito que SÍ coincide"", ""...""],
  ""missing_skills"": [""habilidad o requisito que FALTA o no se evidencia"", ""...""],
  ""summary"": ""resumen de 1-2 oraciones explicando el puntaje""
}}

OFERTA LABORAL:
Título: {offer.title}
Descripción: {offer.description}
Requisitos: {offer.requirements}

CV DEL CANDIDATO (texto extraído):
{textoPdf}";

        string respuestaIA;
        try
        {
            respuestaIA = await _chatService.SendMessageAsync(prompt);
        }
        catch
        {
            return false;
        }

        var jsonLimpio = LimpiarJson(respuestaIA);

        try
        {
            var parsed = JObject.Parse(jsonLimpio);
            var score = parsed["score"]?.Value<int>() ?? 0;
            score = Math.Clamp(score, 0, 100);

            application.match_score = score;
            application.match_details = jsonLimpio;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string LimpiarJson(string texto)
    {
        var t = texto.Trim();
        if (t.StartsWith("```"))
        {
            var primerSalto = t.IndexOf('\n');
            if (primerSalto != -1) t = t[(primerSalto + 1)..];
            if (t.EndsWith("```")) t = t[..^3];
        }
        var inicio = t.IndexOf('{');
        var fin = t.LastIndexOf('}');
        if (inicio >= 0 && fin > inicio)
            t = t.Substring(inicio, fin - inicio + 1);
        return t.Trim();
    }
}