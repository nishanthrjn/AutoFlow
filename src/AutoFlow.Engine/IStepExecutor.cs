namespace AutoFlow.Engine;

using AutoFlow.Domain.Entities;

public interface IStepExecutor
{
    Task<StepResult> ExecuteAsync(StepDefinition step, WorkflowInstance instance);
}

public class StepResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}