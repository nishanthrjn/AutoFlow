# AutoFlow — Enterprise Workflow Automation + Digital Twin Platform

[![Build](https://img.shields.io/github/actions/workflow/status/nishanthrjn/AutoFlow/ci.yml?branch=main&label=build)](https://github.com/nishanthrjn/AutoFlow/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/Platform-Industrie%204.0-teal.svg)](https://github.com/nishanthrjn/AutoFlow)

> A production-grade Industrial IoT workflow automation engine built in C# / .NET 10.
> AutoFlow watches real-time machine telemetry and automatically triggers intelligent,
> fault-tolerant workflows when anomalies are detected — with a permanent audit trail.

---

## The Problem It Solves

In modern manufacturing, machines constantly broadcast sensor data.
When something goes wrong — a temperature spike, a vibration anomaly —
someone has to notice, create a ticket, alert the team, and log the event.

**AutoFlow does all of that automatically, in under 2 seconds.**

---

## Live Demo Flow

```text
CNC machine temperature spikes to 94°C
↓
MQTT broker receives telemetry (Eclipse Mosquitto)
↓
AutoFlow Consumer detects anomaly (rule: temp > 90°C)
↓
Workflow Engine triggers maintenance-response-v1
↓
Step 1: HTTP action → POST alert   Running → Succeeded
↓
PostgreSQL records full audit trail with millisecond timestamps
↓
REST API exposes workflow status + step history
---
```
## Architecture

```text
┌─────────────────────────────────────────────────────────┐
│                     Physical Edge                        │
│   CNC Machine → Temperature / Vibration / RPM sensors   │
└────────────────────────┬────────────────────────────────┘
                         │ MQTT (MQTTnet)
┌────────────────────────▼────────────────────────────────┐
│              Eclipse Mosquitto Broker                    │
└────────────────────────┬────────────────────────────────┘
                         │ Subscribe factory/+/+
┌────────────────────────▼────────────────────────────────┐
│              AutoFlow.Consumer (BackgroundService)       │
│   • MQTT listener    • Anomaly detector                  │
│   • Rule: temp > 90°C → trigger workflow                 │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│              AutoFlow.Engine                             │
│   • DAG executor     • Saga compensation                 │
│   • Polly retry      • Plugin dispatcher                 │
│   • IWorkflowAction  • IStepRepository                   │
└──────────┬─────────────────────────────┬────────────────┘
           │                             │
┌──────────▼──────────┐    ┌─────────────▼──────────────┐
│   Plugin: HTTP      │    │   PostgreSQL (EF Core)      │
│   POST alert        │    │   workflow_instances        │
│   (extensible SDK)  │    │   step_state_entries        │
└─────────────────────┘    └─────────────────────────────┘
                                         │
┌────────────────────────────────────────▼────────────────┐
│              AutoFlow.Api (ASP.NET Core Minimal API)     │
│   GET  /health                                           │
│   GET  /api/workflows/recent                             │
│   GET  /api/workflows/{id}/steps                         │
│   POST /api/workflows/trigger                            │
│   UI   /scalar/v1  (Scalar API explorer)                 │
└─────────────────────────────────────────────────────────┘
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | C# 12 / .NET 10 |
| API | ASP.NET Core Minimal API |
| IoT Transport | MQTT (MQTTnet 5.x + Eclipse Mosquitto) |
| Persistence | PostgreSQL 16 + EF Core 9 + Npgsql |
| Fault tolerance | Polly 8 (retry + exponential backoff) |
| API Explorer | Scalar UI (OAS 3.1) |
| Containerisation | Docker + docker-compose |
| Testing | xUnit + Testcontainers |

---

## Quick Start

**Prerequisites:** Docker Desktop, .NET 10 SDK

```bash
# 1. Clone
git clone https://github.com/nishanthrjn/AutoFlow.git
cd AutoFlow

# 2. Start infrastructure (MQTT broker + PostgreSQL)
docker-compose -f infra/docker-compose.yml up -d

# 3. Run the API
dotnet run --project src/AutoFlow.Api

# 4. Open API explorer in browser
http://localhost:5031/scalar/v1

# 5. Run the CNC simulator (new terminal)
dotnet run --project src/AutoFlow.Simulator

# 6. Run the anomaly consumer (new terminal)
dotnet run --project src/AutoFlow.Consumer
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Engine status and version |
| `GET` | `/api/workflows/recent` | Last 20 workflow executions |
| `GET` | `/api/workflows/{id}` | Single workflow instance status |
| `GET` | `/api/workflows/{id}/steps` | Full step audit trail with timestamps |
| `POST` | `/api/workflows/trigger` | Manually trigger any workflow |

**Example — trigger a workflow:**
```bash
curl -X POST http://localhost:5031/api/workflows/trigger \
  -H "Content-Type: application/json" \
  -d '{"assetId":"cnc-01","workflowType":"MaintenanceResponse","data":{"temperature":94}}'
```

**Response:**
```json
{
  "instanceId": "6cad0aca-a44b-48e1-87ed-9d090aef5639",
  "status": "Pending",
  "createdAt": "2026-05-03T14:53:45Z"
}
```

---

## Project Structure

```text
AutoFlow/
├── src/
│   ├── AutoFlow.Domain/          # Entities, enums, domain model
│   ├── AutoFlow.Engine/          # DAG executor, saga, Polly, plugins
│   │   ├── Interfaces/           # IWorkflowAction, IStepRepository
│   │   ├── Actions/              # HttpWorkflowAction (extensible SDK)
│   │   └── Persistence/          # EF Core DbContext, PostgreSQL repo
│   ├── AutoFlow.Api/             # ASP.NET Core Minimal API + Scalar UI
│   ├── AutoFlow.Consumer/        # MQTT listener + anomaly detector
│   └── AutoFlow.Simulator/       # CNC machine telemetry simulator
├── tests/
│   └── AutoFlow.Engine.Tests/    # xUnit, DAG, state machine, saga
└── infra/
    ├── docker-compose.yml        # Mosquitto + PostgreSQL
    └── mosquitto/                # MQTT broker config

---
```
## Key Engineering Decisions

**DAG-based execution** — workflows are defined as directed acyclic graphs.
Steps with no dependencies run in parallel via `Task.WhenAll`. Steps with
dependencies wait for their predecessors. Circular dependencies throw
a clear `InvalidOperationException` at parse time, not runtime.

**Saga compensation** — if a step fails after retries are exhausted, a
compensation handler fires to undo side effects. This is the same pattern
used by enterprise systems like Azure Logic Apps and Temporal.

**Plugin SDK** — every workflow action implements `IWorkflowAction`.
New integrations (Slack, Jira, ServiceNow) are added by registering
a new class — no changes to the engine required.

**IDbContextFactory over DbContext injection** — parallel step execution
means multiple threads write to the database simultaneously. DbContext is
not thread-safe. The factory creates a fresh context per operation.

**Fire-and-forget API trigger** — `POST /trigger` returns `202 Accepted`
immediately. The caller polls `GET /workflows/{id}/steps` for progress.
This mirrors how production workflow APIs handle long-running jobs.

---

## Test Suite

```bash
dotnet test
```

```text
total: 4, failed: 0, succeeded: 4
├── FailedStep_AfterRetries_TriggersCompensation
├── HappyPath_StepSucceeds_WritesSucceededState
├── DiamondDependency_BothBranchesCompleteBeforeD
└── GetExecutionBatches_ShouldReturnParallelBatch_ForDiamondDAG
```
---

## Why This Project

Most portfolio projects are CRUD apps. AutoFlow is an **engine** —
a system with a runtime, a plugin model, fault tolerance, and a live
IoT data pipeline.

---

## Author

**Nishanth Rajan** — Software Engineer
🔗 [linkedin.com/in/nishanthrajan](https://linkedin.com/in/nishanthrajan)
🐙 [github.com/nishanthrjn/AutoFlow](https://github.com/nishanthrjn/AutoFlow)
