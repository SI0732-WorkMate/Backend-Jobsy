namespace Jobsy.JobsyAi.Domain.Model.ValueObjects;

public class AIModel
{
    public string Name { get; }

    public AIModel(string name)
    {
        Name = name;
    }

    public static AIModel Default => new AIModel("mistralai/mistral-7b-instruct");
}