using System.Text;
using System.Text.Json;
using MQTTnet;

const string BROKER = "localhost";
const int    PORT   = 1883;
const string ASSET  = "cnc-01";

var factory = new MqttClientFactory();

while (true) // outer retry loop — survives Docker restarts
{
    using var client = factory.CreateMqttClient();

    try
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(BROKER, PORT)
            .WithClientId($"autoflow-simulator-{ASSET}")
            .Build();

        Console.WriteLine($"[{Now()}] Connecting to Mosquitto at {BROKER}:{PORT}...");
        await client.ConnectAsync(options, CancellationToken.None);
        Console.WriteLine($"[{Now()}] Connected. Streaming CNC telemetry for {ASSET}.");
        Console.WriteLine("─────────────────────────────────────────────────────");

        int tick = 0;

        while (client.IsConnected)
        {
            tick++;

            // Every 5th tick inject a temperature spike — anomaly detector bait
            bool isFault  = tick % 5 == 0;
            int  temp     = isFault ? 94  : 45;   // °C — spike threshold is 90
            int  vibration = isFault ? 18 : 4;    // mm/s — normal < 8
            int  rpm      = isFault ? 820 : 1200; // RPM — fault = underspeed

            // ── Publish each property on its own topic ──────────────────────
            // Topic structure: factory/{assetId}/{property}
            // Ditto maps each topic to a Feature property on the Thing

            await PublishAsync(client, $"factory/{ASSET}/temperature", new
            {
                value     = temp,
                unit      = "celsius",
                timestamp = DateTimeOffset.UtcNow
            });

            await PublishAsync(client, $"factory/{ASSET}/vibration", new
            {
                value     = vibration,
                unit      = "mm/s",
                timestamp = DateTimeOffset.UtcNow
            });

            await PublishAsync(client, $"factory/{ASSET}/rpm", new
            {
                value     = rpm,
                unit      = "rpm",
                timestamp = DateTimeOffset.UtcNow
            });

            // ── Console output ──────────────────────────────────────────────
            string flag = isFault ? "🔴 ANOMALY" : "🟢 Normal ";
            Console.WriteLine(
                $"[{Now()}] {flag} | Temp: {temp,3}°C | " +
                $"Vib: {vibration,2} mm/s | RPM: {rpm,4}");

            await Task.Delay(2000);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{Now()}] Connection lost: {ex.Message}");
        Console.WriteLine($"[{Now()}] Retrying in 5 seconds — is 'autoflow-mqtt' running?");
        await Task.Delay(5000);
    }
}

// ── Helpers ──────────────────────────────────────────────────────────────────

static async Task PublishAsync(IMqttClient client, string topic, object payload)
{
    var json    = JsonSerializer.Serialize(payload);
    var message = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(Encoding.UTF8.GetBytes(json))
        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
        .Build();

    await client.PublishAsync(message, CancellationToken.None);
}

static string Now() => DateTime.Now.ToString("HH:mm:ss");