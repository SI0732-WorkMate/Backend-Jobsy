using System.Text;
using Jobsy.JobsyAi.Domain.Services;
using UglyToad.PdfPig;

namespace Jobsy.JobsyAi.Infrastructure.ExternalServices;

public class DocumentProcessingService : IDocumentAnalyzer
{
    public string ExtractTextFromPdf(Stream pdfStream)
    {
        var textBuilder = new StringBuilder();

        using (var document = PdfDocument.Open(pdfStream))
        {
            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
        }

        return textBuilder.ToString();
    }
}