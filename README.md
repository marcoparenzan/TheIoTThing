# TheIoTThing

A **service orchestrator**: it discovers, runs, and manages any number of pluggable services concurrently, either inside a Blazor Server web app or a standalone console host, and now exposes the same control surface as a minimal HTTP API. **Flow** — a dataflow graph of **steps** connected by **pipes**, edited visually or as YAML — is the first service type, implemented in its own library (`TheFlowThing`); a minimal, unrelated second one (`TheCounterThing`) exists to prove a new service type is just a plugin, not a special case.

Every service is independent and self-contained (config, autostart, lifecycle), so any mix of them can run side by side in the same process. Every service type is a **plugin, loaded at runtime, not compiled into the host**: the orchestrator host (`TheIoTThingsApp`) has no `ProjectReference` to `TheFlowThing`, `TheCounterThing`, or `FlowUILib` at all, only to the shared `TheIoTThing.Abstractions` contract. Which plugins exist — including each one's own visual editor UI, if it has one — comes entirely from a config file (`plugins.json`), loaded by reflection. Adding a new kind of service means writing a small library against `TheIoTThing.Abstractions` and adding one entry to that config file; removing a plugin entry and restarting makes that service type disappear, with zero code changes either way. See [Dynamic plugin loading](ARCHITECTURE.md#dynamic-plugin-loading) for how.

## Solution layout

| Project | Purpose |
|---|---|
| `src/TheIoTThing.Abstractions` | The orchestrator's core contract: `IOrchestratedService`, `ServiceStatus`, `OrchestrationContext`, `TickingService` (shared Start/Pause/Stop + tick-loop base class), `ServiceTypeRegistry`, `ServiceStore` (where services live on disk: a file, or a folder with an index + resources), `IServicePlugin`/`IOrchestratorAccess` (the plugin contract), and `FlowYamlConverter` (generic JSON⇄YAML, used by the common Config editor). No dependency on Blazor/ASP.NET or on any concrete service. |
| `src/TheFlowThing.Abstractions` | Base types shared by every flow step/pipe implementation: `Def`, `StepDef`, `PipeDef`, `State`. |
| `src/TheFlowThing` | The `flow` service logic: `Executive`, the built-in step types, JSON serialization, the editor⇄flow translation layer. Has its own [Architecture doc](src/TheFlowThing/ARCHITECTURE.md). Not referenced by `TheIoTThingsApp` directly — only via `TheFlowThing.Plugin`. |
| `src/FlowUILib` | Razor component (`FlowCanvas`) + vanilla-JS drag-and-drop flow editor (`flowUIInterop.js`) — flow's own specific UI, reusable from any Blazor app. Same non-referenced-by-the-host rule as `TheFlowThing`. |
| `src/TheFlowThing.Plugin` | The actual loadable **flow plugin**: bundles `TheFlowThing` + `FlowUILib` behind `IServicePlugin` (`FlowServicePlugin`) and ships the flow visual editor (`FlowPage.razor`, `@page "/flow/{FlowName}"`) — this is the one assembly path that goes into `plugins.json`. |
| `src/TheCounterThing` | The `counter` service type (`CounterService` + `CounterServicePlugin`) — a minimal, deliberately Flow-unrelated worked example of building a new pluggable service, with no UI of its own. |
| `src/TheIoTThingsApp` | The orchestrator host (Blazor Server + minimal API): `ServiceOrchestrator` runs one instance per discovered service; `/` lists and controls all of them, `/config/{name}` edits any of them as YAML, `/api/services/...` exposes the same control surface over HTTP. Loads every service-type plugin named in `plugins.json` at startup — see [Architecture](ARCHITECTURE.md#dynamic-plugin-loading). |
| `src/TheIoTThing` | Console CLI ([Spectre.Console.Cli](https://spectreconsole.net/cli/)) with `run`/`list` commands to pick and run one `flow`-type service at a time, with a [Spectre.Console](https://spectreconsole.net/) UI. References `TheFlowThing` directly (not plugin-loaded) — a dedicated, flow-only tool, deliberately not generalized to every service type yet. |
| `__other__/` | Older/experimental libraries (MQTT, OPC UA, Blockly, workspace management) not part of the active solution — kept for reference, not built by `TheIoTThing.sln`. |

## Documentation

- [Architecture](ARCHITECTURE.md) — the orchestrator: the service abstraction, how to build a new orchestrable service, the host, the API. Flow-specific internals (steps, the editor bridge, YAML conversion) have their own doc at [`src/TheFlowThing/ARCHITECTURE.md`](src/TheFlowThing/ARCHITECTURE.md).
- [Usage](USAGE.md) — configuring, running, and authoring services (flows and otherwise) in the web app, the console host, and the API.

## Quick start

```
dotnet build src/TheIoTThing.sln
```

The web app and the console host both read external configuration from `D:\Configurations\TheIoTThing` (not part of this repo — see [Usage](USAGE.md) for what to put there before running, and for the local NuGet feed `TheFlowThing` needs to build at all).

## License

See [LICENSE](LICENSE).
