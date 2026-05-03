namespace AutoFlow.Api.Models;

// POST /api/workflows/trigger
public record TriggerWorkflowRequest(
    string AssetId,
    string WorkflowType,
    Dictionary<string, object> Data
);

// Response for workflow instance
public record WorkflowInstanceResponse(
    Guid   InstanceId,
    Guid   DefinitionId,
    string Status,
    DateTime CreatedAt
);

// Response for individual step state
public record StepStateResponse(
    string   StepId,
    string   Status,
    DateTime RecordedAt,
    string?  ErrorMessage
);

// Response for health check
public record HealthResponse(
    string Status,
    DateTime Time,
    string Version
);
