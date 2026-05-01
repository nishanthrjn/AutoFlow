using AutoFlow.Domain.Enums;

namespace AutoFlow.Engine;

public interface IStepRepository
{
    Task WriteStateAsync(Guid instanceId, string stepId,
                         StepStatus status, CancellationToken ct);
}
