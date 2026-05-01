namespace AutoFlow.Domain.Entities;

public class WorkflowDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; } = 1;

    // The collection of all steps in this workflow
    public List<StepDefinition> Steps { get; set; } = new();
}