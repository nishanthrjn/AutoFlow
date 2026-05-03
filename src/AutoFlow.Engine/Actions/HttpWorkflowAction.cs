using System.Net.Http.Json;
using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public class HttpWorkflowAction : IWorkflowAction
{
    public string ActionType => "Http";  // ← tells the executor which steps to handle

    private readonly IHttpClientFactory _httpClientFactory;

    public HttpWorkflowAction(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task ExecuteAsync(
        StepDefinition    step,
        WorkflowContext   context,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(nameof(HttpWorkflowAction));

        step.Parameters.TryGetValue("Url",    out var url);
        step.Parameters.TryGetValue("Method", out var method);

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("HTTP action requires a 'Url' parameter.", nameof(step));

        var request = new HttpRequestMessage(
            new HttpMethod(method ?? "POST"),
            url);

        // Safely serialize whatever execution data exists on the instance
        var payload = context.Instance.ExecutionData;
        request.Content = JsonContent.Create(payload);

        var response = await client.SendAsync(request, ct);

        // Non-2xx triggers Polly retry in the executor — this is intentional
        response.EnsureSuccessStatusCode();
    }
}