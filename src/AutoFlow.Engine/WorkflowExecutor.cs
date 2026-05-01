using AutoFlow.Domain.Entities;
using AutoFlow.Domain.Enums;
using Polly;

namespace AutoFlow.Engine;

public class WorkflowExecutor
{
    private readonly IDependencyResolver         _resolver;
    private readonly IEnumerable<IWorkflowAction> _actions;
    private readonly IStepRepository             _repository;
    private readonly ICompensationHandler        _compensation;
    private readonly Func<int, TimeSpan>         _sleepDuration;

    public WorkflowExecutor(
        IDependencyResolver          resolver,
        IEnumerable<IWorkflowAction> actions,
        IStepRepository              repository,
        ICompensationHandler         compensation,
        Func<int, TimeSpan>?         sleepDuration = null)
    {
        _resolver      = resolver;
        _actions       = actions;
        _repository    = repository;
        _compensation  = compensation;
        _sleepDuration = sleepDuration
            ?? (attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public async Task ExecuteWorkflowAsync(
        WorkflowDefinition definition,
        WorkflowInstance   instance,
        CancellationToken  ct = default)
    {
        var batches = _resolver.GetExecutionBatches(definition);
        var context = new WorkflowContext(instance);

        foreach (var batch in batches)
            await Task.WhenAll(batch.Select(step =>
                ExecuteStepAsync(step, instance, context, ct)));
    }

    private async Task ExecuteStepAsync(
        StepDefinition    step,
        WorkflowInstance  instance,
        WorkflowContext   context,
        CancellationToken ct)
    {
        await _repository.WriteStateAsync(
            instance.Id, step.Id, StepStatus.Running, ct);

        var retryPolicy = BuildRetryPolicy(instance, step, ct);

        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                // Clean dispatch — no reflection, no string.Contains
                var action = _actions.FirstOrDefault(a => a.ActionType == step.ActionType)
                    ?? throw new InvalidOperationException(
                        $"No action registered for type '{step.ActionType}'.");

                await action.ExecuteAsync(step, context, ct);
            });

            await _repository.WriteStateAsync(
                instance.Id, step.Id, StepStatus.Succeeded, ct);
        }
        catch (Exception ex)
        {
            await _repository.WriteStateAsync(
                instance.Id, step.Id, StepStatus.Failed, ct);
            await _compensation.CompensateAsync(instance, step.Id, ex, ct);
            throw;
        }
    }

    private Polly.Retry.AsyncRetryPolicy BuildRetryPolicy(
        WorkflowInstance  instance,
        StepDefinition    step,
        CancellationToken ct)
    {
        int maxRetries = step.RetryPolicy?.MaxRetries ?? 3;

        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount:            maxRetries,
                sleepDurationProvider: _sleepDuration,
                onRetry: async (_, _, attempt, _) =>
                {
                    await _repository.WriteStateAsync(
                        instance.Id, step.Id, StepStatus.Retrying, ct);
                });
    }
}