using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public interface IWorkflowAction
{
    string ActionType { get; }
    Task ExecuteAsync(StepDefinition step, WorkflowContext context, CancellationToken ct);
}