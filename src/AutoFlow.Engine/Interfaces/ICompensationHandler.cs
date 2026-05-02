using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public interface ICompensationHandler
{
    Task CompensateAsync(WorkflowInstance instance, string failedStepId,
                         Exception reason, CancellationToken ct);
}
