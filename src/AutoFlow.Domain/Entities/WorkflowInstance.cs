namespace AutoFlow.Domain.Entities;

public enum WorkflowStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Compensating // Part of the Saga pattern for rollbacks
}

public class WorkflowInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DefinitionId { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ExecutionData { get; set; } = new();
}