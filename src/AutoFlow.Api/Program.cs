using AutoFlow.Engine;
using AutoFlow.Engine.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.MapGet("/health", () => new
{
    Status = "AutoFlow Engine Online",
    Time   = DateTime.UtcNow
}).WithName("GetHealth");

app.Run();
