using AutoFlow.Domain.Entities;
using AutoFlow.Domain.Enums;

namespace AutoFlow.Engine;

public interface IStepRepository
{
    Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken ct);
    Task WriteStateAsync(Guid instanceId, string stepId,
                         StepStatus status, CancellationToken ct);
}
