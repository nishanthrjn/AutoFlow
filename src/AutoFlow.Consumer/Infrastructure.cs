using AutoFlow.Domain.Entities;
using AutoFlow.Domain.Enums;
using AutoFlow.Engine;
using Microsoft.Extensions.Logging;

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

public class LoggingCompensationHandler : ICompensationHandler
{
    private readonly ILogger<LoggingCompensationHandler> _logger;

    public LoggingCompensationHandler(ILogger<LoggingCompensationHandler> logger)
        => _logger = logger;

    public Task CompensateAsync(WorkflowInstance instance, string failedStepId,
                                Exception reason, CancellationToken ct)
    {
        _logger.LogError(
            "COMPENSATION triggered for step [{StepId}] on instance [{InstanceId}]. Reason: {Reason}",
            failedStepId, instance.Id, reason.Message);
        return Task.CompletedTask;
    }
}
