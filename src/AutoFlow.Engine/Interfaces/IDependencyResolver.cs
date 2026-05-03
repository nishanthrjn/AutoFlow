using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public interface IDependencyResolver
{
    IReadOnlyList<IReadOnlyList<StepDefinition>> GetExecutionBatches(
        WorkflowDefinition definition);
}