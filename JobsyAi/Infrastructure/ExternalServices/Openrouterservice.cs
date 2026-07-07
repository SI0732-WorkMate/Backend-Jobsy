using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.JobsyAi.Domain.Services;
using Newtonsoft.Json;

namespace Jobsy.JobsyAi.Infrastructure.ExternalServices;

public class OpenrouterService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly string ApiKey;

    public OpenrouterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        ApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
    }

    public async Task<string> SendMessageAsync(string prompt, string model = "openrouter/free")
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return BuildLocalFallback(prompt);

        var requestBody = new
        {
            model = "openrouter/free",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        requestMessage.Headers.Add("HTTP-Referer", "http://localhost:5173");
        requestMessage.Headers.Add("X-Title", "JobsyAi");
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return BuildLocalFallback(prompt);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"OpenRouter error {response.StatusCode}: {responseString}");

        dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
        string result = jsonResponse.choices[0].message.content;
        return result;
    }

    private static string BuildLocalFallback(string prompt)
    {
        var cvText = ExtractAfter(prompt, "CV DEL CANDIDATO");
        var requirements = ExtractAfter(prompt, "Requisitos:");
        var score = EstimateScore(cvText, requirements);

        if (prompt.Contains("\"score\"", StringComparison.OrdinalIgnoreCase))
        {
            return JsonConvert.SerializeObject(new
            {
                score,
                matched_skills = ExtractKeywords(cvText).Take(5).ToArray(),
                missing_skills = ExtractKeywords(requirements).Except(ExtractKeywords(cvText), StringComparer.OrdinalIgnoreCase).Take(5).ToArray(),
                summary = "Evaluacion local generada porque OpenRouter no esta configurado. El puntaje se estima comparando palabras clave del CV con la vacante."
            });
        }

        return $"""
## Puntaje: {score}/100

### Fortalezas
- Se genero una evaluacion local porque OpenRouter no esta configurado.
- El CV contiene informacion analizable para contrastarla con la oferta.
- Se identificaron coincidencias preliminares por palabras clave.

### Areas de mejora
- Configura OPENROUTER_API_KEY para obtener una evaluacion completa con IA externa.
- Revisa que el CV mencione tecnologias, experiencia y logros alineados a la vacante.

### Veredicto
Resultado preliminar generado localmente. Usa esta respuesta como respaldo de demo hasta configurar la clave de OpenRouter.
""";
    }

    private static int EstimateScore(string cvText, string requirements)
    {
        var cvKeywords = ExtractKeywords(cvText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requirementKeywords = ExtractKeywords(requirements).ToList();

        if (requirementKeywords.Count == 0)
            return cvKeywords.Count > 0 ? 70 : 40;

        var matches = requirementKeywords.Count(k => cvKeywords.Contains(k));
        return Math.Clamp(45 + (int)Math.Round(matches * 55.0 / requirementKeywords.Count), 0, 100);
    }

    private static IEnumerable<string> ExtractKeywords(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "para", "con", "del", "las", "los", "una", "uno", "por", "que", "este", "esta",
            "como", "and", "the", "de", "la", "el", "en", "y", "o", "a", "un"
        };

        return Regex.Matches(text ?? "", @"[A-Za-z0-9+#.]{3,}")
            .Select(m => m.Value.Trim().ToLowerInvariant())
            .Where(w => !stopWords.Contains(w))
            .Distinct();
    }

    private static string ExtractAfter(string text, string marker)
    {
        var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? "" : text[(index + marker.Length)..];
    }
}
