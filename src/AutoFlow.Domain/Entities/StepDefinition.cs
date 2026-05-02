namespace AutoFlow.Domain.Entities;

public class StepDefinition
{
    public string Id                             { get; set; } = string.Empty;
    public string ActionType                     { get; set; } = string.Empty;
    public List<string> DependsOn                { get; set; } = new();
    public Dictionary<string, string> Parameters { get; set; } = new();
    public StepRetryPolicy? RetryPolicy          { get; set; }
}

public class StepRetryPolicy
{
    public int MaxRetries   { get; set; } = 3;
    public int DelaySeconds { get; set; } = 2;
}
