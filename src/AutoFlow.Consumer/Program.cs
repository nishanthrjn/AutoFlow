using AutoFlow.Consumer;
using AutoFlow.Engine;
using AutoFlow.Engine.Persistence;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString =
            "Host=localhost;Port=5432;Database=autoflow;Username=autoflow;Password=autoflow_dev";

        services.AddDbContextFactory<AutoFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHttpClient();
        services.AddSingleton<IDependencyResolver,  LinearDependencyResolver>();
        services.AddSingleton<IStepRepository,      PostgreSqlStepRepository>();
        services.AddSingleton<ICompensationHandler, LoggingCompensationHandler>();
        services.AddSingleton<IWorkflowAction,      HttpWorkflowAction>();
        services.AddSingleton<WorkflowExecutor>();
        services.AddHostedService<MqttListenerService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var factory = scope.ServiceProvider
                       .GetRequiredService<IDbContextFactory<AutoFlowDbContext>>();
    await using var db = factory.CreateDbContext();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
