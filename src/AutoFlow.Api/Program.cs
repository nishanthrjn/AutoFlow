using AutoFlow.Engine;
using AutoFlow.Engine.Actions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Standard API Services ---
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// --- 2. Infrastructure & HTTP ---
// This prevents socket exhaustion for our HTTP plugins
builder.Services.AddHttpClient(); 

// --- 3. AutoFlow Brain & Logic ---
// We use Singletons because these classes are stateless logic providers
builder.Services.AddSingleton<IDependencyResolver, DependencyResolver>();
builder.Services.AddSingleton<WorkflowExecutor>();

// --- 4. Action Plugin Registration ---
// We register these as IWorkflowAction so the Executor can find them 
// via IEnumerable<IWorkflowAction>
builder.Services.AddTransient<IWorkflowAction, HttpWorkflowAction>();
// Note: When you add SlackWorkflowAction, simply add another line here!

// --- 5. Background Execution ---
// This starts the worker thread that processes the DAGs
builder.Services.AddHostedService<WorkflowBackgroundWorker>();

var app = builder.Build();

// --- 6. HTTP Pipeline Configuration ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- 7. API Endpoints ---

// Basic Health Check to see if the Engine is breathing
app.MapGet("/health", () => new { Status = "AutoFlow Engine Online", Time = DateTime.UtcNow })
   .WithName("GetHealth");

// Future Endpoint: POST /api/workflows to trigger a new automation
// app.MapPost("/api/workflows", async (WorkflowDefinition def, WorkflowExecutor executor) => { ... });

app.Run();