using AutoFlow.Domain.Entities;
using AutoFlow.Engine.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoFlow.Api;

public class WorkflowQueryService
{
    private readonly IDbContextFactory<AutoFlowDbContext> _contextFactory;

    public WorkflowQueryService(IDbContextFactory<AutoFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<WorkflowInstance?> GetInstanceAsync(
        Guid instanceId, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.WorkflowInstances
            .FirstOrDefaultAsync(x => x.Id == instanceId, ct);
    }

    public async Task<List<StepStateEntry>> GetStepHistoryAsync(
        Guid instanceId, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.StepStateEntries
            .Where(x => x.InstanceId == instanceId)
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(ct);
    }

    public async Task<List<WorkflowInstance>> GetRecentInstancesAsync(
        int count, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.WorkflowInstances
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<List<StepStateEntry>> GetAssetHistoryAsync(
        string assetId, int count, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        // Use raw SQL to query JSONB column for assetId value
        var instanceIds = await db.WorkflowInstances
            .FromSqlRaw(
                "SELECT * FROM workflow_instances WHERE execution_data->>'assetId' = {0}",
                assetId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        return await db.StepStateEntries
            .Where(x => instanceIds.Contains(x.InstanceId))
            .OrderByDescending(x => x.RecordedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}
