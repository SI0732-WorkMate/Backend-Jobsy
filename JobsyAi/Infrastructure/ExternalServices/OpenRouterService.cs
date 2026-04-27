using System.Net.Http.Headers;
using System.Text;
using Jobsy.JobsyAi.Domain.Services;
using Newtonsoft.Json;

namespace Jobsy.JobsyAi.Infrastructure.ExternalServices;

public class OpenRouterService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = ""; //remplaza tu apikey que te da

    public OpenRouterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> SendMessageAsync(string prompt, string model = "mistralai/mistral-7b-instruct")
    {
        var request = new
        {
            model = model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost:5173"); // o tu dominio
        _httpClient.DefaultRequestHeaders.Add("X-Title", "JobsyAi");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
        string result = jsonResponse.choices[0].message.content;

        return result;
    }
}