namespace AutoFlow.Domain.Entities;

public class StepStateEntry
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public Guid     InstanceId   { get; set; }
    public string   StepId       { get; set; } = string.Empty;
    public string   Status       { get; set; } = string.Empty;
    public string?  ErrorMessage { get; set; }
    public DateTime RecordedAt   { get; set; } = DateTime.UtcNow;

    public WorkflowInstance Instance { get; set; } = null!;
}
