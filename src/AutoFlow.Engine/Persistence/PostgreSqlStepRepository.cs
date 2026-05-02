using AutoFlow.Domain.Entities;
using AutoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoFlow.Engine.Persistence;

public class PostgreSqlStepRepository : IStepRepository
{
    private readonly IDbContextFactory<AutoFlowDbContext> _contextFactory;

    public PostgreSqlStepRepository(IDbContextFactory<AutoFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync(ct);
        Console.WriteLine($"  [DB] Instance {instance.Id} saved");
    }

    public async Task WriteStateAsync(
        Guid instanceId, string stepId,
        StepStatus status, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        db.StepStateEntries.Add(new StepStateEntry
        {
            Id         = Guid.NewGuid(),
            InstanceId = instanceId,
            StepId     = stepId,
            Status     = status.ToString(),
            RecordedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        Console.WriteLine($"  [DB] {stepId} → {status} @ {DateTime.UtcNow:HH:mm:ss.fff}");
    }
}
