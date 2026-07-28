# Usage

## Prerequisites

- .NET 10 SDK (preview — the projects target `net10.0`).
- Windows, with a `D:\` drive — configuration paths below are hardcoded (`D:\Configurations\TheIoTThing`), matching how this solution is actually deployed today. There's no environment-variable override; if you need a different drive/OS, that's a code change in `ServiceStore` (`src/TheIoTThing.Abstractions/ServiceStore.cs`) and `launchSettings.json`.

## Adding the local NuGet feed

`TheFlowThing` depends on `PySharp.Interpreter` (the `pysharp` step's Python engine, see [TheFlowThing's docs](src/TheFlowThing/ARCHITECTURE.md#pysharp-contract)), published to a local feed rather than nuget.org. `src/NuGet.Config` already registers that feed for anything built from within `src/`:

```xml
<packageSources>
  <add key="NuGetLocalFeed" value="D:\Dev\NuGetLocalFeed" />
</packageSources>
```

This only works if the package is actually sitting in that folder — `dotnet pack` `PySharp.Interpreter.csproj` with `-o D:\Dev\NuGetLocalFeed` (adjust the path to wherever that project lives on your machine) before building this solution for the first time, or restore will fail to find it. The same applies to any other project you want to consume this way — `dotnet pack <project> -c Release -o D:\Dev\NuGetLocalFeed` drops a `.nupkg` there, `dotnet nuget list source` (run from `src/`) confirms the feed is registered.

## Configuration

Nothing environment-specific lives inside the repo. Before running either host, create:

**`D:\Configurations\TheIoTThing\device31-dev.json`** (or whatever `launchSettings.json`'s `TenantConfigurationFile` points at) — the tenant configuration merged into `IConfiguration` at startup:

```json
{
  "HostName": "...",
  "CustomerName": "...",
  "DeviceName": "...",
  "CertificateUrl": "...",
  "CertificatePassword": "..."
}
```

An **`ApiKey`** setting (e.g. in `appsettings.Development.json`, already set there to `dev-orchestrator-key` for local runs) — required by the `/api/services/...` endpoints (see [The orchestrator API](#the-orchestrator-api) below).

**`D:\Configurations\TheIoTThing\plugins.json`** — which service-type plugins the host loads at startup; **this is what "flow" or "counter" being a valid `"type"` actually means** — no plugin, no type, regardless of what's in `Services\`. See [Architecture](ARCHITECTURE.md#dynamic-plugin-loading) for the full mechanics; the two plugins this repo ships:

```json
{
  "plugins": [
    {
      "assembly": "D:\\Dev\\2026\\IoTHub\\TheIoTThing\\src\\TheFlowThing.Plugin\\bin\\Debug\\net10.0\\TheFlowThing.Plugin.dll",
      "staticAssetsPath": "D:\\Dev\\2026\\IoTHub\\TheIoTThing\\src\\FlowUILib\\wwwroot",
      "staticAssetsRequestPath": "/_content/FlowUILib"
    },
    {
      "assembly": "D:\\Dev\\2026\\IoTHub\\TheIoTThing\\src\\TheCounterThing\\bin\\Debug\\net10.0\\TheCounterThing.dll"
    }
  ]
}
```

Paths point at each plugin project's own build output, so build the solution before first run. `staticAssetsPath`/`staticAssetsRequestPath` are only needed for a plugin that ships its own JS/CSS (like the flow editor). Removing the `TheFlowThing.Plugin` entry and restarting makes `flow` disappear from the type dropdown entirely and any existing `flow`-type service on disk gets skipped (logged as a warning, not a crash) until the entry comes back — no code change either way.

**`D:\Configurations\TheIoTThing\Services\`** — an orchestration is a folder of services; every service both hosts can see lives directly under this one folder, as either a single file (`myservice.json`, the whole config in one place) or a folder (`myservice\index.json` + whatever other resource files that service needs — see [Architecture](ARCHITECTURE.md#the-service-abstraction-theiotthingabstractions)). Either way the file's name (or the folder's name) is the service's identity — in the web app's URLs and API, in the console's `list`/`run`. The web app runs every service in the folder at once (see [The pages](#the-pages) below); the console host runs one flow per process, so "at once" there means one `dotnet run -- run ...` per terminal.

Every service config has a top-level `"type"` telling the orchestrator which registered implementation owns it. A flow's `type` is `"flow"`, and its shape is otherwise the canonical `FlowDef` (see [TheFlowThing's docs](src/TheFlowThing/ARCHITECTURE.md#the-flow-model-theflowthingabstractions)):

```json
{
  "type": "flow",
  "steps": [
    { "id": "s1", "type": "data-source", "name": "Source", "x": 100, "y": 100, "width": 160, "height": 90,
      "outputs": [{ "id": "out", "name": "out" }], "properties": { "source": "hello" } },
    { "id": "s2", "type": "node-script", "name": "Upper", "x": 320, "y": 100, "width": 160, "height": 90,
      "inputs": [{ "id": "in", "name": "in" }], "outputs": [{ "id": "out", "name": "out" }],
      "properties": { "code": "outputs.out = inputs.in.toUpperCase();" } }
  ],
  "pipes": [
    { "id": "p1", "sourceStep": "s1", "sourceOutput": "out", "targetStep": "s2", "targetInput": "in", "properties": {} }
  ],
  "scale": 1,
  "autostart": false
}
```

A minimal, unrelated example — the `counter` service type (see [Architecture](ARCHITECTURE.md#how-to-build-an-orchestrable-service)), shipped as a folder (`Services\counter\index.json`) to demonstrate that shape too:

```json
{
  "type": "counter",
  "intervalMilliseconds": 1000,
  "autostart": false
}
```

`autostart` (default `false` if omitted) is a property of each service's own config, not a separate setting — the orchestrator starts every service with `autostart: true` when it boots; toggling the checkbox on Home rewrites just that flag in that service's own file.

You normally won't hand-write a flow file from scratch — see [Editing a flow](src/TheFlowThing/ARCHITECTURE.md#editing-a-flow) in TheFlowThing's own docs.

## Running the web app

```
dotnet run --project src/TheIoTThingsApp/TheIoTThingsApp.csproj
```

`launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` and `TenantConfigurationFile` for you when running via `dotnet run`/Visual Studio; if you launch it another way, set both yourself. The app listens on `https://localhost:9172` and `http://localhost:9154`. If `TenantConfigurationFile` is missing/unreadable, the app logs an error and exits immediately (`Program.cs` treats it as a hard startup requirement, not optional).

### The pages

- **`/` — Home**: a name field, a type dropdown populated from whatever plugins are currently loaded (see [`plugins.json`](#configuration) above — never hardcoded), and a **+ New Service** button to create an empty service of that type and jump straight into its editor; below that, one row per service found in `Services\` — name, **type**, status badge (`Stopped`/`Running`/`Paused`), Start/Pause/Stop buttons, an autostart checkbox, edit links (**Visual** only for types whose plugin registered one — today just `flow`; **Config**, the common YAML editor, always), and a 🗑 delete button (asks for confirmation, stops the service first if it's running, then removes its file). Every service with `autostart: true` is already `Running` the moment the app finishes starting; the rest sit `Stopped` until you press Start. The list refreshes every second and picks up services dropped into `Services\` without restarting the app (they show up `Stopped` — autostart only applies at boot), and drops anything whose file disappears from disk outside the app.
- **`/config/{name}` — Config** (every service's **Config** link): a common YAML editor for any service type. If the service is `Running`, an **Edit (pauses the service)** button appears — click it before making changes; saving also pauses regardless, so you can't accidentally write over a running service. Save validates the YAML syntax only — there's no shape to check generically across arbitrary service types, so a malformed-but-syntactically-valid edit surfaces as an inline "invalid config" error only when the service actually tries to reload it.
- **`/flow/{name}` — Flow** (a flow's **Visual** link): the visual drag-and-drop editor, specific to the `flow` service type. Same Edit/pause discipline as Config. **Save** in the canvas toolbar writes straight back to the file and reloads that flow — it does not download a file to your browser.

In both editors, saving leaves that service **paused** — go back to Home and press Start to run the edited version. Other services are unaffected either way, since each has its own independent instance.

### The orchestrator API

Everything the pages do is also available over HTTP, guarded by the `ApiKey` from [Configuration](#configuration) above (as an `X-API-Key` header or a `?code=` query param) — meant for scripting/automation today, and for a future dedicated orchestration CLI. See the table in [Architecture](ARCHITECTURE.md#minimal-api-servicesorchestratorendpointscs) for the full list of endpoints. Example:

```
curl -H "X-API-Key: dev-orchestrator-key" https://localhost:9172/api/services
curl -H "X-API-Key: dev-orchestrator-key" -X POST https://localhost:9172/api/services/counter/start
```

## Running the console host

No web UI, no autostart, and (for now — a dedicated orchestration CLI is future work) scoped to `flow`-type services only: just a flow's `Executive` driven directly from a terminal against the same `Services\` folder the web app uses, useful for a quick local check of a flow file. Run from anywhere (it doesn't depend on the current working directory):

```
cd src/TheIoTThing

# see what flow services are available in D:\Configurations\TheIoTThing\Services
dotnet run -- list

# run one by name (resolved against D:\Configurations\TheIoTThing\Services)
dotnet run -- run flow.json

# or omit the file and pick one interactively
dotnet run -- run
dotnet run              # "run" is the default command
```

`run` starts the flow and waits for a keypress to stop it. Only entries whose `"type"` is `"flow"` show up in `list` and in the interactive picker — a `counter` service, for instance, would not.

## Extending

- **A new step type** *inside* a flow (e.g. a real `transform`/`filter` implementation) — see [TheFlowThing's docs](src/TheFlowThing/ARCHITECTURE.md#extending-with-a-new-step-type).
- **A whole new service type** alongside `flow` (something that isn't a dataflow graph at all) — see [How to build an orchestrable service](ARCHITECTURE.md#how-to-build-an-orchestrable-service) in the root architecture doc; `TheCounterThing` is the worked example.

## Troubleshooting

- **`NotImplementedException` spamming the log every second** — the flow uses a stub step type (see the table in [TheFlowThing's docs](src/TheFlowThing/ARCHITECTURE.md#step-types-theflowthingsteps)). Either avoid that type or implement it.
- **App won't start, "Failed to load configuration. Startup interrupted."** — `TenantConfigurationFile` isn't set or the file it points to doesn't exist/isn't readable.
- **"Skipping service '...': no plugin registered for type '...'" in the log at startup** — a service under `Services\` has a `"type"` whose plugin isn't listed in `plugins.json` (or the path in `plugins.json` doesn't point at a built DLL). Not fatal — that one service is just invisible until you fix the entry and restart; every other service keeps running.
- **`/api/services/...` returns 400/401** — the `X-API-Key` header (or `?code=` query param) is missing or doesn't match the `ApiKey` configured (see [Configuration](#configuration)).
- **"Invalid YAML: ..." on Config Save** — a YAML syntax error; nothing was written, fix the text and save again.
- **"Invalid config: ..." on Config Save** — the YAML parsed fine but the service failed to load the resulting config (commonly, for a flow: a step `type` that isn't registered — see [Extending](#extending)); nothing was written.
