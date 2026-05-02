using AutoFlow.Domain.Entities;
using AutoFlow.Domain.Enums;
using AutoFlow.Engine;

namespace AutoFlow.Consumer;

public class LinearDependencyResolver : IDependencyResolver
{
    public IReadOnlyList<IReadOnlyList<StepDefinition>> GetExecutionBatches(
        WorkflowDefinition definition)
    {
        return definition.Steps
            .Select(step => (IReadOnlyList<StepDefinition>)new List<StepDefinition> { step })
            .ToList();
    }
}

public class InMemoryStepRepository : IStepRepository
{
    private readonly Dictionary<string, List<StepStatus>> _store = new();

    public Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken ct)
    {
        Console.WriteLine($"  [Mem] Instance {instance.Id} saved");
        return Task.CompletedTask;
    }

    public Task WriteStateAsync(Guid instanceId, string stepId,
                                StepStatus status, CancellationToken ct)
    {
        var key = $"{instanceId}:{stepId}";
        if (!_store.ContainsKey(key)) _store[key] = new();
        _store[key].Add(status);
        Console.WriteLine($"  [State] {stepId} → {status}");
        return Task.CompletedTask;
    }
}
