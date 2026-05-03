# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

AutoFlow is a .NET 10 distributed workflow orchestration engine that coordinates step-based workflows with DAG dependency resolution, retry policies (Polly), and saga-pattern compensation. It ingests MQTT telemetry from industrial assets, detects anomalies, and triggers remediating workflows that are persisted with a full audit trail in PostgreSQL.

## Commands

```bash
# Start infrastructure (PostgreSQL + Mosquitto MQTT broker)
docker-compose -f infra/docker-compose.yml up -d

# Build entire solution
dotnet build AutoFlow.slnx

# Run all tests
dotnet test tests/AutoFlow.Engine.Tests/AutoFlow.Engine.Tests.csproj

# Run a single test by name
dotnet test tests/AutoFlow.Engine.Tests/ --filter "FullyQualifiedName~DependencyResolverTests.ResolveBatches_DiamondDependency"

# Run individual services
dotnet run --project src/AutoFlow.Api
dotnet run --project src/AutoFlow.Consumer
dotnet run --project src/AutoFlow.Simulator
```

The API exposes a Scalar UI at `/api/openapi/scalar` when running locally.

## Architecture

The system has four runtime components:

```
AutoFlow.Simulator  →  Mosquitto MQTT  →  AutoFlow.Consumer
                                               ↓
                                        AutoFlow.Engine
                                               ↓
                                          PostgreSQL
                                               ↑
                                        AutoFlow.Api
```

- **AutoFlow.Domain** — pure domain entities (`WorkflowDefinition`, `WorkflowInstance`, `StepDefinition`, `StepStateEntry`) and status enums. No dependencies.
- **AutoFlow.Engine** — the orchestration core. `WorkflowExecutor` drives execution; `DependencyResolver` performs topological sort to produce parallel execution batches; Polly retry policies are configured per-step; `ICompensationHandler` runs on failure. Persistence is behind `IStepRepository`; `PostgreSqlStepRepository` is the live implementation.
- **AutoFlow.Api** — minimal API REST gateway. Uses `WorkflowQueryService` to read execution state. Endpoints: `POST /api/workflows/trigger`, `GET /api/workflows/recent`, `GET /api/workflows/{id}`, `GET /api/workflows/{id}/steps`.
- **AutoFlow.Consumer** — `MqttListenerService` subscribes to `factory/+/+` topics, parses JSON telemetry, and triggers a workflow when anomalies are detected (e.g. temperature > 90 °C). Uses `LinearDependencyResolver` (sequential) rather than the DAG resolver used in Engine.
- **AutoFlow.Simulator** — console app that publishes temperature/vibration/RPM for asset `cnc-01` every 2 seconds, injecting anomalies every 5th tick.

### Key Patterns

**Step execution plugin:** implement `IWorkflowAction` (in `AutoFlow.Engine/Interfaces/`) and register it by the `ActionType` string that appears in `StepDefinition`. `HttpWorkflowAction` is the built-in implementation.

**DAG resolution:** `DependencyResolver.ResolveBatches()` returns `IEnumerable<IReadOnlyList<StepDefinition>>` — each inner list is a batch that can execute concurrently. Steps in a batch share no dependency edges.

**Compensation:** when any step fails after exhausting retries, `WorkflowExecutor` invokes `ICompensationHandler.CompensateAsync()` with the steps that already succeeded. The default is `LoggingCompensationHandler`.

**Persistence:** `AutoFlowDbContext` owns three tables (`workflow_definitions`, `workflow_instances`, `step_state_entries`). JSONB columns hold flexible schema (Steps, ExecutionData). Every state change appends a `StepStateEntry` row — the table is an immutable audit log, never updated in-place.

### Database

Connection string (hardcoded in both `Api/Program.cs` and `Consumer/Program.cs`):
```
Host=localhost;Port=5432;Database=autoflow;Username=autoflow;Password=autoflow_dev
```

Migrations live in `src/AutoFlow.Engine/Persistence/Migrations/`. The Consumer applies migrations automatically on startup; the API does not.

### Testing Approach

Tests are in `tests/AutoFlow.Engine.Tests/` using xUnit. There are no integration tests — the test suite uses in-process test doubles (`InMemoryStepRepository`, `SuccessAction`, `AlwaysFailingAction`) to cover:
- DAG resolution including cycle detection and diamond dependencies (`DependencyResolverTests`)
- Retry behaviour, compensation triggering, and concurrent batch execution (`WorkflowExecutorTests`)

New persistence or API behaviour should be tested via in-process fakes following the same pattern, not by spinning up PostgreSQL.
