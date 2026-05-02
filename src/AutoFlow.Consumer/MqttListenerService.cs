using System.Text.Json;
using AutoFlow.Domain.Entities;
using AutoFlow.Engine;
using MQTTnet;

namespace AutoFlow.Consumer;

public record TelemetryData(double Value, string Unit, DateTimeOffset Timestamp);

public class MqttListenerService : BackgroundService
{
    private readonly WorkflowExecutor _executor;
    private readonly ILogger<MqttListenerService> _logger;

    public MqttListenerService(WorkflowExecutor executor,
                               ILogger<MqttListenerService> logger)
    {
        _executor = executor;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic   = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            var parts = topic.Split('/');
            if (parts.Length < 3) return;

            var assetId  = parts[1];
            var property = parts[2];

            if (property != "temperature") return;

            try
            {
                var data = JsonSerializer.Deserialize<TelemetryData>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data is null) return;

                _logger.LogInformation("[{Asset}] temp: {Value}C", assetId, data.Value);

                if (data.Value > 90)
                {
                    _logger.LogWarning(
                        "ANOMALY — {Asset} at {Value}C — triggering workflow",
                        assetId, data.Value);

                    var definition = BuildMaintenanceWorkflow(assetId);
                    var instance   = new WorkflowInstance
                    {
                        Id           = Guid.NewGuid(),
                        DefinitionId = definition.Id,
                        Status       = WorkflowStatus.Pending,
                        ExecutionData = new Dictionary<string, object>
                        {
                            ["assetId"]     = assetId,
                            ["temperature"] = data.Value,
                            ["detectedAt"]  = data.Timestamp
                        }
                    };

                    _ = Task.Run(() =>
                        _executor.ExecuteWorkflowAsync(definition, instance, ct), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process telemetry from {Topic}", topic);
            }
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId("autoflow-consumer")
            .Build();

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("factory/+/+")
            .Build();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await client.ConnectAsync(options, ct);
                _logger.LogInformation("Connected. Listening on factory/+/+");
                await client.SubscribeAsync(subscribeOptions, ct);
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection lost — retrying in 5s");
                await Task.Delay(5000, ct);
            }
        }
    }

    private static WorkflowDefinition BuildMaintenanceWorkflow(string assetId) => new()
    {
        Id    = Guid.NewGuid(),
        Name  = $"Maintenance Response — {assetId}",
        Steps = new List<StepDefinition>
        {
            new()
            {
                Id         = "log-alert",
                ActionType = "Http",
                Parameters = new Dictionary<string, string>
                {
                    ["Url"]    = "https://httpbin.org/post",
                    ["Method"] = "POST"
                }
            }
        }
    };
}
