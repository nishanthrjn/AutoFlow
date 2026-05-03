using AutoFlow.Api;
using AutoFlow.Api.Models;
using AutoFlow.Domain.Entities;
using AutoFlow.Domain.Enums;
using AutoFlow.Engine;
using AutoFlow.Engine.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

var connectionString =
    "Host=localhost;Port=5432;Database=autoflow;Username=autoflow;Password=autoflow_dev";

builder.Services.AddDbContextFactory<AutoFlowDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IDependencyResolver,  DependencyResolver>();
builder.Services.AddSingleton<IStepRepository,      PostgreSqlStepRepository>();
builder.Services.AddSingleton<ICompensationHandler, LoggingCompensationHandler>();
builder.Services.AddSingleton<IWorkflowAction,      HttpWorkflowAction>();
builder.Services.AddSingleton<WorkflowExecutor>();
builder.Services.AddScoped<WorkflowQueryService>();

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// ── Endpoints ────────────────────────────────────────────────────────────────

// GET /health
app.MapGet("/health", () => new HealthResponse(
    Status:  "AutoFlow Engine Online",
    Time:    DateTime.UtcNow,
    Version: "1.0.0"
)).WithName("GetHealth").WithTags("System");

// GET /api/workflows/recent — last 20 workflow runs
app.MapGet("/api/workflows/recent", async (
    WorkflowQueryService query,
    CancellationToken ct) =>
{
    var instances = await query.GetRecentInstancesAsync(20, ct);
    return Results.Ok(instances.Select(i => new WorkflowInstanceResponse(
        i.Id, i.DefinitionId, i.Status.ToString(), i.CreatedAt)));
}).WithName("GetRecentWorkflows").WithTags("Workflows");

// GET /api/workflows/{instanceId} — single workflow status
app.MapGet("/api/workflows/{instanceId:guid}", async (
    Guid instanceId,
    WorkflowQueryService query,
    CancellationToken ct) =>
{
    var instance = await query.GetInstanceAsync(instanceId, ct);
    if (instance is null)
        return Results.NotFound(new { Error = $"Instance {instanceId} not found" });

    return Results.Ok(new WorkflowInstanceResponse(
        instance.Id,
        instance.DefinitionId,
        instance.Status.ToString(),
        instance.CreatedAt));
}).WithName("GetWorkflowInstance").WithTags("Workflows");

// GET /api/workflows/{instanceId}/steps — full audit trail
app.MapGet("/api/workflows/{instanceId:guid}/steps", async (
    Guid instanceId,
    WorkflowQueryService query,
    CancellationToken ct) =>
{
    var steps = await query.GetStepHistoryAsync(instanceId, ct);
    if (!steps.Any())
        return Results.NotFound(new { Error = $"No steps found for instance {instanceId}" });

    return Results.Ok(steps.Select(s => new StepStateResponse(
        s.StepId, s.Status, s.RecordedAt, s.ErrorMessage)));
}).WithName("GetWorkflowSteps").WithTags("Workflows");

// POST /api/workflows/trigger — manually trigger a workflow
app.MapPost("/api/workflows/trigger", async (
    TriggerWorkflowRequest request,
    WorkflowExecutor executor,
    IStepRepository repository,
    CancellationToken ct) =>
{
    var definition = new WorkflowDefinition
    {
        Id    = Guid.NewGuid(),
        Name  = $"{request.WorkflowType} — {request.AssetId}",
        Steps = new List<StepDefinition>
        {
            new()
            {
                Id         = "http-action",
                ActionType = "Http",
                Parameters = new Dictionary<string, string>
                {
                    ["Url"]    = "https://httpbin.org/post",
                    ["Method"] = "POST"
                }
            }
        }
    };

    var instance = new WorkflowInstance
    {
        Id           = Guid.NewGuid(),
        DefinitionId = definition.Id,
        Status       = WorkflowStatus.Pending,
        ExecutionData = request.Data
    };

    // Fire and forget — return immediately with instance ID
    _ = Task.Run(() => executor.ExecuteWorkflowAsync(definition, instance, ct), ct);

    return Results.Accepted($"/api/workflows/{instance.Id}",
        new WorkflowInstanceResponse(
            instance.Id,
            instance.DefinitionId,
            instance.Status.ToString(),
            instance.CreatedAt));
}).WithName("TriggerWorkflow").WithTags("Workflows");

app.Run();
