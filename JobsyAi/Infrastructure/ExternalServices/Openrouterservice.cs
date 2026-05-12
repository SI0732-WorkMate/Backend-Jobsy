using System.Net.Http.Headers;
using System.Text;
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

        if (!response.IsSuccessStatusCode)
            throw new Exception($"OpenRouter error {response.StatusCode}: {responseString}");

        dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
        string result = jsonResponse.choices[0].message.content;
        return result;
    }
}