using AutoFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Engine;

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
