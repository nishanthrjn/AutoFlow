using System.Net.Http.Json;
using AutoFlow.Domain.Entities;

namespace AutoFlow.Engine;

public class HttpWorkflowAction : IWorkflowAction
{
    public string ActionType => "Http";

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
            throw new ArgumentException("HTTP action requires a Url parameter.", nameof(step));

        var request = new HttpRequestMessage(
            new HttpMethod(method ?? "POST"), url);

        request.Content = JsonContent.Create(context.Instance.ExecutionData);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
