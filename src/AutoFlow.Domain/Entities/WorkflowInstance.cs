using System.Text.Json.Serialization;

namespace AutoFlow.Domain.Entities;

public enum WorkflowStatus
{
    Pending, Running, Succeeded, Failed, Compensating
}

public class WorkflowInstance
{
    public Guid           Id            { get; set; } = Guid.NewGuid();
    public Guid           DefinitionId  { get; set; }
    public WorkflowStatus Status        { get; set; } = WorkflowStatus.Pending;
    public DateTime       CreatedAt     { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ExecutionData { get; set; } = new();

    [JsonIgnore]
    public List<StepStateEntry> StepStateEntries { get; set; } = new();
}
