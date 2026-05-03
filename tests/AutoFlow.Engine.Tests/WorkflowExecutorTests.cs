using AutoFlow.Domain.Entities;
using AutoFlow.Engine;
using Xunit;

namespace AutoFlow.Engine.Tests;

public class WorkflowExecutorTests
{
    [Fact]
    public async Task FailedStep_AfterRetries_TriggersCompensation()
    {
        var action       = new AlwaysFailingAction();
        var repository   = new InMemoryStepRepository();
        var compensation = new RecordingCompensationHandler();
        var resolver     = new FakeResolver(singleStep: "slack-notify");
        var executor     = new WorkflowExecutor(resolver, action, repository, compensation);

        await Assert.ThrowsAsync<Exception>(() =>
            executor.ExecuteWorkflowAsync(TestData.SimpleWorkflow, TestData.NewInstance()));

        var states = repository.StatesFor("slack-notify");
        Assert.Equal(StepStatus.Running,  states[0]);
        Assert.Equal(StepStatus.Retrying, states[1]);
        Assert.Equal(StepStatus.Retrying, states[2]);
        Assert.Equal(StepStatus.Retrying, states[3]);
        Assert.Equal(StepStatus.Failed,   states[4]);
        Assert.Single(compensation.RecordedCompensations);
    }

    [Fact]
    public async Task HappyPath_StepSucceeds_WritesSucceededState()
    {
        var action       = new SuccessAction();
        var repository   = new InMemoryStepRepository();
        var compensation = new RecordingCompensationHandler();
        var resolver     = new FakeResolver(singleStep: "step-1");
        var executor     = new WorkflowExecutor(resolver, action, repository, compensation);

        await executor.ExecuteWorkflowAsync(TestData.SimpleWorkflow, TestData.NewInstance());

        var states = repository.StatesFor("step-1");
        Assert.Equal(2, states.Count);
        Assert.Equal(StepStatus.Running,   states[0]);
        Assert.Equal(StepStatus.Succeeded, states[1]);
        Assert.Empty(compensation.RecordedCompensations);
    }

    [Fact]
    public async Task DiamondDependency_BothBranchesCompleteBeforeD()
    {
        var repository   = new InMemoryStepRepository();
        var action       = new SuccessAction();
        var compensation = new RecordingCompensationHandler();
        var resolver     = new DiamondResolver();
        var executor     = new WorkflowExecutor(resolver, action, repository, compensation);

        await executor.ExecuteWorkflowAsync(TestData.DiamondWorkflow, TestData.NewInstance());

        var history      = repository.GetGlobalTimeline();
        var dStartedAt   = history.FindIndex(x => x.StepId == "D" && x.Status == StepStatus.Running);
        var bSucceededAt = history.FindIndex(x => x.StepId == "B" && x.Status == StepStatus.Succeeded);
        var cSucceededAt = history.FindIndex(x => x.StepId == "C" && x.Status == StepStatus.Succeeded);

        Assert.True(bSucceededAt < dStartedAt, "B must succeed before D starts");
        Assert.True(cSucceededAt < dStartedAt, "C must succeed before D starts");
    }
}

// ── Test doubles ──────────────────────────────────────────────────────────────

public class AlwaysFailingAction : IWorkflowAction
{
    public Task ExecuteAsync(StepDefinition step, WorkflowContext context, CancellationToken ct)
        => throw new Exception("Simulated network failure");
}

public class SuccessAction : IWorkflowAction
{
    public Task ExecuteAsync(StepDefinition step, WorkflowContext context, CancellationToken ct)
        => Task.CompletedTask;
}

public class InMemoryStepRepository : IStepRepository
{
    private readonly Dictionary<string, List<StepStatus>> _perStep = new();
    private readonly List<(string StepId, StepStatus Status)> _globalTimeline = new();

    public Task WriteStateAsync(Guid instanceId, string stepId,
                                StepStatus status, CancellationToken ct)
    {
        if (!_perStep.ContainsKey(stepId))
            _perStep[stepId] = new();
        _perStep[stepId].Add(status);
        _globalTimeline.Add((stepId, status));
        return Task.CompletedTask;
    }

    public List<StepStatus> StatesFor(string stepId) =>
        _perStep.TryGetValue(stepId, out var list) ? list : new();

    public List<(string StepId, StepStatus Status)> GetGlobalTimeline() =>
        _globalTimeline;
}

public class RecordingCompensationHandler : ICompensationHandler
{
    public List<string> RecordedCompensations { get; } = new();
    public Task CompensateAsync(WorkflowInstance instance, string failedStepId,
                                Exception reason, CancellationToken ct)
    {
        RecordedCompensations.Add(failedStepId);
        return Task.CompletedTask;
    }
}

public class FakeResolver : IDependencyResolver
{
    private readonly StepDefinition _step;
    public FakeResolver(string singleStep) => _step = new StepDefinition { Id = singleStep };
    public IReadOnlyList<IReadOnlyList<StepDefinition>> GetExecutionBatches(WorkflowDefinition definition)
        => new List<List<StepDefinition>> { new() { _step } };
}

public class DiamondResolver : IDependencyResolver
{
    public IReadOnlyList<IReadOnlyList<StepDefinition>> GetExecutionBatches(WorkflowDefinition definition)
        => new List<List<StepDefinition>>
        {
            new() { new StepDefinition { Id = "A" } },
            new() { new StepDefinition { Id = "B" }, new StepDefinition { Id = "C" } },
            new() { new StepDefinition { Id = "D" } }
        };
}

public static class TestData
{
    public static WorkflowDefinition SimpleWorkflow => new()
    {
        Id = "simple-wf",
        Name = "Simple Workflow",
        Steps = new List<StepDefinition>
        {
            new() { Id = "slack-notify" },
            new() { Id = "step-1" }
        }
    };

    public static WorkflowDefinition DiamondWorkflow => new()
    {
        Id = "diamond-wf",
        Name = "Diamond Workflow",
        Steps = new List<StepDefinition>
        {
            new() { Id = "A" },
            new() { Id = "B", DependsOn = new List<string> { "A" } },
            new() { Id = "C", DependsOn = new List<string> { "A" } },
            new() { Id = "D", DependsOn = new List<string> { "B", "C" } }
        }
    };

    public static WorkflowInstance NewInstance() => new()
    {
        Id = Guid.NewGuid(),
        WorkflowDefinitionId = "simple-wf",
        Status = StepStatus.Pending
    };
}