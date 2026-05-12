namespace Jobsy.JobsyAi.Domain.Services;

public interface IDocumentAnalyzer
{
    string ExtractTextFromPdf(Stream pdfStream);
}