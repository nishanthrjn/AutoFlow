namespace AutoFlow.Domain.Entities;

public class StepDefinition
{
    // Unique identifier for this step within the workflow
    public string Id { get; set; } = string.Empty;

    // The type of action (e.g., "JiraTicket", "SlackAlert", "DittoPatch")
    public string ActionType { get; set; } = string.Empty;

    // Key-value pairs for configuration (e.g., "ChannelName": "#alerts")
    public Dictionary<string, string> Parameters { get; set; } = new();

    // The "Edges" of our DAG: IDs of steps that must finish before this one starts
    public List<string> DependsOn { get; set; } = new();
    
    // Resilience configuration for this specific step
    public int RetryCount { get; set; } = 3;
}