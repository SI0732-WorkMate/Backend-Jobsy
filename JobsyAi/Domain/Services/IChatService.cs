namespace Jobsy.JobsyAi.Domain.Services;

public interface IChatService
{
    Task<string> SendMessageAsync(string prompt, string model = "mistralai/mistral-7b-instruct");
}