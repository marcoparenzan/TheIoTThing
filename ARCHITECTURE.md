# Architecture

## Project dependency graph

```
TheIoTThing.Abstractions   (IOrchestratedService, ServiceStatus, OrchestrationContext,
        ^     ^              TickingService, ServiceTypeRegistry, ServiceStore,
        |     |               IServicePlugin, IOrchestratorAccess, FlowYamlConverter)
        |     |
        |     +------------------------------------------+
        |                                                |
        |   TheFlowThing.Abstractions                     |
        |     ^                                           |
        |     |                                           |
        |   TheFlowThing   (Executive, step types, ...)   |
        |     ^     ^                                     |
        |     |     |                                     |
        |  FlowUILib   TheCounterThing (CounterService +   |
        |     ^          CounterServicePlugin, own project) |
        |     |                                           |
        |   TheFlowThing.Plugin (FlowServicePlugin,        |
        |     FlowPage.razor — bundles the flow service     |
        |     AND its UI into one loadable assembly)        |
        |                                                  |
        +--------------------------------------------------+---- TheIoTThingsApp
        |                                                            (Blazor Server
      TheIoTThing (console host, flow-only)                          orchestrator host)
```

`TheIoTThing.Abstractions` has no dependency on ASP.NET Core, Blazor, or any concrete service — it's the contract every pluggable **service** and every **host** builds on. `TheIoTThingsApp` has **no `ProjectReference` to `TheFlowThing`, `TheCounterThing`, `TheFlowThing.Plugin`, or `FlowUILib` at all** — only to `TheIoTThing.Abstractions`. Every service type (including its UI, if it has one) is loaded at runtime from a plugin assembly named in a config file — see [Dynamic plugin loading](#dynamic-plugin-loading) below. `TheCounterThing` and `TheFlowThing.Plugin` are two independent plugin assemblies that both depend on `TheIoTThing.Abstractions` and know nothing about each other or about the host.

## The service abstraction (`TheIoTThing.Abstractions`)

This is what makes `TheIoTThingsApp` an **orchestrator of services** rather than an app hardcoded around "flow" — flow is just the first service type, implemented the same way any future one would be.

- **`ServiceStatus`** — `Stopped`/`Running`/`Paused`, the lifecycle every service has.
- **`IOrchestratedService`** — the contract: `Status`, `Autostart`, `Task LoadAsync(path, context)`, `StartAsync()`/`PauseAsync()`/`StopAsync()`, `SetAutostart(bool)`, `object? GetSnapshot()` (an opaque, service-specific snapshot of current state/output — a flow's `FlowState`, a counter's current count — for a sibling that knows what to do with it).
- **`OrchestrationContext`** — the "orchestration state" injected into a service when it's loaded: `ServiceName`, `ServicePath`, and `Services` (an `IServiceRegistry` — `Names`, `TryGet(name) -> IOrchestratedService?`) so a service can look up a sibling's status or snapshot. Implemented by the host's `ServiceOrchestrator` (see below); this library only depends on the interface.
- **`TickingService`** (abstract base) — most services just need to do something on a fixed interval, so the `Stopped/Running/Paused` state machine, the pause gate (`SemaphoreSlim`), and the tick loop (`PeriodicTimer`, per-tick try/catch/log) live here once. A subclass implements `TickInterval`, `IsLoaded`, `OnTickAsync()`, plus its own `LoadAsync`/`GetSnapshot`. Both `TheFlowThing.Executive` and `TheCounterThing.CounterService` derive from this — see [How to build an orchestrable service](#how-to-build-an-orchestrable-service) below.
- **`ServiceTypeRegistry`** — `Register(type, factory)` / `Create(type, serviceProvider)` / `IsRegistered(type)`, a factory-by-key registry (same shape as `TheFlowThing`'s own `StepDefConverter.Add<T>(key)` for step types, one level down). The factory is `Func<IServiceProvider, IOrchestratedService>` — a plugin gets the host's `IServiceProvider` to resolve its own logger etc., without the registry itself ever referencing a concrete service type. Populated at runtime by `PluginLoader`/`Program.cs` from `plugins.json`, not by any hardcoded call — see [Dynamic plugin loading](#dynamic-plugin-loading) below.
- **`IServicePlugin`** / **`ServicePluginRegistration`** — the contract a plugin assembly implements to describe itself (which types it registers, their factories, and an optional routed-editor URL template) — see [Dynamic plugin loading](#dynamic-plugin-loading).
- **`IOrchestratorAccess`** — the minimal orchestrator surface (`GetOrCreateAsync`, `ReloadAsync`) a plugin's *own* UI can `@inject`, so a plugin never needs a compile-time reference to the host's concrete `ServiceOrchestrator` (wrong dependency direction — a plugin may only depend on `TheIoTThing.Abstractions`). `ServiceOrchestrator` implements this alongside `IServiceRegistry`.
- **`FlowYamlConverter`** — a fully generic JSON⇄YAML converter (`ToYaml`/`ToJson`, plus `FlowYamlException`). Despite the name (it started life next to `FlowDef`), it round-trips *any* JSON object, which is exactly why it lives here rather than in `TheFlowThing` — the common `/config/{name}` editor needs it independent of whether any plugin is even loaded.
- **`ServiceStore`** — where services live on disk and how they're discovered:
  ```csharp
  public const string ConfigDirectory = @"D:\Configurations\TheIoTThing";
  public const string ServicesDirectory = ConfigDirectory + @"\Services";
  ```
  An **orchestration is a folder of services** (`ServicesDirectory`); each entry is either a single file (`myservice.json`/`.yaml` — the whole config in one place) or a folder (`myservice\index.json`/`.yaml` + whatever other resource files that service needs alongside it, referenced by the service itself). Either way, `List()` peeks the top-level `"type"` field to know which registered `IOrchestratedService` owns it, returning a `ServiceDescriptor(Name, Type, ConfigPath)` per entry (skipping anything that doesn't parse or has no `"type"`). `Resolve(name)` turns a bare name into the file to load (the file itself, or a folder's index file).

## How to build an orchestrable service

`TheCounterThing` (`src/TheCounterThing/CounterService.cs`) is the minimal worked example — deliberately unrelated to Flow, to prove this isn't secretly Flow-shaped:

1. Depend on `TheIoTThing.Abstractions` only (a plain `Microsoft.NET.Sdk` class library — use `Microsoft.NET.Sdk.Razor` instead if the service also ships its own UI, like `TheFlowThing.Plugin` does).
2. Derive from `TickingService` (or implement `IOrchestratedService` directly if your service doesn't fit a fixed-tick model):
   ```csharp
   public class CounterService(ILogger<CounterService>? logger = null) : TickingService(logger)
   {
       CounterConfig? config;
       int count;

       protected override TimeSpan TickInterval => TimeSpan.FromMilliseconds(config?.IntervalMilliseconds ?? 1000);
       protected override bool IsLoaded => config is not null;

       public override async Task LoadAsync(string path, OrchestrationContext context)
       {
           config = JsonSerializer.Deserialize<CounterConfig>(await File.ReadAllTextAsync(path), ...) ?? new();
           count = 0;
           SetAutostart(config.Autostart);
       }

       public override object? GetSnapshot() => count;
       protected override Task OnTickAsync() { count++; return Task.CompletedTask; }
   }
   ```
3. Give your config a `"type"` field (`ServiceStore` needs it to know this file is yours) — a `CounterConfig` class with `[JsonPropertyName("type")] public string Type { get; set; } = "counter";` plus whatever else the service needs.
4. Add an `IServicePlugin` implementation to the same assembly so the host can discover it by reflection:
   ```csharp
   public class CounterServicePlugin : IServicePlugin
   {
       public IEnumerable<ServicePluginRegistration> GetRegistrations()
       {
           yield return new ServicePluginRegistration(
               "counter",
               services => new CounterService(services.GetService<ILogger<CounterService>>()));
       }
   }
   ```
5. Set `<GenerateDependencyFile>true</GenerateDependencyFile>` and `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in the project file — see [Dynamic plugin loading](#dynamic-plugin-loading) for why both are required for a class library to be loadable as a plugin at all.
6. Add the built assembly's path to `plugins.json` (see [Dynamic plugin loading](#dynamic-plugin-loading) and [Usage](USAGE.md#configuration)) — that's the *only* place that needs to change to add a brand new service type; nothing in `TheIoTThingsApp`'s source, `ServiceOrchestrator`, the pages, or the API ever changes, and the host is never recompiled.
7. Optional: if your service deserves its own visual editor (like `TheFlowThing.Plugin` ships via `FlowUILib`'s `FlowCanvas`), pass an `EditorRouteTemplate` (e.g. `"/counter/{0}"`) in your `ServicePluginRegistration` and ship a `@page`-routed Razor component with that route in the same assembly — otherwise the service automatically gets the common `/config/{name}` YAML editor for free.

## Dynamic plugin loading

**Nothing about a service type — not its logic, not its UI — is ever referenced at compile time by `TheIoTThingsApp`.** Which plugins exist comes entirely from a config file (`plugins.json`), loaded by reflection at startup. This is what makes "removing a plugin" a config change rather than a code change (see the plugins.json example in [Usage](USAGE.md#configuration)) — deleting its entry and restarting makes that service type (and any of its routes/UI) disappear with zero recompilation.

### The plugin contract (`TheIoTThing.Abstractions`)

```csharp
public interface IServicePlugin
{
    IEnumerable<ServicePluginRegistration> GetRegistrations();
    void ConfigureServices(IServiceCollection services) { } // default no-op
}

public record ServicePluginRegistration(
    string Type,
    Func<IServiceProvider, IOrchestratedService> Factory,
    string? EditorRouteTemplate = null);
```

A plugin assembly contains exactly one public, non-abstract class implementing `IServicePlugin` (a plugin *could* register more than one `Type` from `GetRegistrations()`, though none does today). `ConfigureServices` exists so a plugin can register whatever DI services its own UI needs (e.g. `TheFlowThing.Plugin` registers `FlowUILib.FlowUIInterop` there) — it's called **before** the host builds its `IServiceCollection`, since that's the only point at which anything can still be added to the container.

### Loading a plugin assembly in isolation (`TheIoTThingsApp/Services/PluginLoader.cs`)

Each plugin's main assembly is loaded into its own `System.Runtime.Loader.AssemblyLoadContext`, using an `AssemblyDependencyResolver` (built from the plugin's own `.deps.json`) to resolve the plugin's *private* dependencies (its own NuGet packages, its own `ProjectReference`s) from its build output folder:

```csharp
class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: false)
{
    readonly AssemblyDependencyResolver resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsShared(assemblyName.Name)) return null; // fall back to the Default ALC's already-loaded copy
        var path = resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }
}
```

A short, explicit list of assembly-name prefixes is **deliberately not private-loaded** — `TheIoTThing.Abstractions`, `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, `Microsoft.JSInterop*`, `System.*`, `netstandard`, `mscorlib` — returning `null` from `Load` for these tells the runtime to fall back to the Default `AssemblyLoadContext`'s copy instead of loading a second one. This is not an optimization; it's required for correctness. If a plugin's `Executive : IOrchestratedService` were checked against a *different* loaded copy of `IOrchestratedService` than the one `ServiceOrchestrator` itself uses, `is IOrchestratedService` would return `false` even though the type names match exactly — CLR type identity includes the loading context. The same reasoning applies to ASP.NET Core Components (`ComponentBase`) and `Microsoft.Extensions.DependencyInjection` — a plugin's Razor component needs to be a real `ComponentBase` to the host's renderer, and its constructor injection needs to resolve against the same DI abstractions the host's container uses.

Two MSBuild properties are required on every plugin project for this to work at all:
- **`<GenerateDependencyFile>true</GenerateDependencyFile>`** — class libraries don't emit a `.deps.json` by default (only executables do); without it, `AssemblyDependencyResolver` has nothing to resolve from.
- **`<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>`** — class libraries also don't copy their full transitive dependency closure (NuGet packages, other `ProjectReference`s) into their own output folder by default, only executables do. Without this, `.deps.json` is generated correctly but the DLLs it points at (e.g. `Jint.dll`, `YamlDotNet.dll`, `PySharpLib.dll` for the flow plugin) are simply missing from the output directory.

`PluginLoader.Load(assemblyPath, hostServices)` loads the assembly, reflects for its `IServicePlugin` implementation, instantiates it, calls `ConfigureServices(hostServices)`, and returns a `LoadedPlugin(Assembly, Registrations)` — the `Assembly` is needed for Blazor routing (below), the `Registrations` for `ServiceTypeRegistry` and the editor route table.

### Config file (`plugins.json`)

```json
{
  "plugins": [
    {
      "assembly": "D:\\...\\TheFlowThing.Plugin\\bin\\Debug\\net10.0\\TheFlowThing.Plugin.dll",
      "staticAssetsPath": "D:\\...\\FlowUILib\\wwwroot",
      "staticAssetsRequestPath": "/_content/FlowUILib"
    },
    { "assembly": "D:\\...\\TheCounterThing\\bin\\Debug\\net10.0\\TheCounterThing.dll" }
  ]
}
```

Lives alongside the other environment-specific files under `ServiceStore.ConfigDirectory` (see [Usage](USAGE.md#configuration)). `staticAssetsPath`/`staticAssetsRequestPath` are optional — only a plugin that ships its own JS/CSS (like `FlowUILib`'s `flowUIInterop.js`) needs them.

### Routing to a plugin's own UI

A plugin's Razor page (e.g. `TheFlowThing.Plugin/FlowPage.razor`, `@page "/flow/{FlowName}"`) is a real `@page`-annotated component sitting in a runtime-loaded assembly — Blazor needs telling about it in **two** separate places, both driven by the same `PluginRegistry.Assemblies`:

- `Routes.razor`'s `<Router AdditionalAssemblies="pluginRegistry.Assemblies">` — for client-side navigation *within* an already-loaded page.
- `Program.cs`'s `app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(pluginRegistry.Assemblies.ToArray())` — for the **first** HTTP request to that route. This one is easy to miss: ASP.NET Core's endpoint routing (which decides whether a request 404s before Blazor's `Router` component ever runs) builds its route table from `MapRazorComponents<App>()`'s own configured assemblies, not from the `Router` component's `AdditionalAssemblies` parameter — that parameter only affects in-app navigation after the initial page has already loaded. Omitting `AddAdditionalAssemblies` here makes every plugin route 404 on a fresh navigation/refresh while still "working" if you got there by clicking a link from an already-loaded page — a easy-to-miss trap.

A plugin's static web assets (`wwwroot/*.js`/`*.css`) hit a related but separate problem: ASP.NET Core's static web asset pipeline is a **build-time** feature — a runtime-loaded Razor Class Library's `wwwroot` isn't automatically served. `Program.cs` instead registers one `app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(entry.StaticAssetsPath), RequestPath = entry.StaticAssetsRequestPath })` per plugin entry that declares `staticAssetsPath`/`staticAssetsRequestPath`, matching the `_content/{Library}/...` convention the framework would have used at compile time (so JS/CSS references inside the plugin's own components don't need to change).

### `PluginRegistry` (`TheIoTThingsApp/Services/PluginRegistry.cs`)

The host-side singleton that accumulates what every loaded plugin registered: `Types` (every service type key — what Home's "New Service" dropdown offers), `TryGetEditRoute(type, name)` (the specific-editor URL for a type, or `null` if it only has the common Config editor), and `Assemblies` (for the two routing registrations above). Replaces what used to be two hardcoded `Program.cs` lines per service type.

## `TheIoTThingsApp` — the orchestrator host

### Composition root (`Program.cs`)

The only place that ever touches a concrete plugin — and only by reflection, driven entirely by `plugins.json`:

```csharp
var loadedPlugins = new List<(LoadedPlugin Plugin, PluginConfigEntry Entry)>();
foreach (var entry in pluginsConfig.Plugins)
{
    loadedPlugins.Add((PluginLoader.Load(entry.Assembly, builder.Services), entry));
}

var app = builder.Build();

var pluginRegistry = app.Services.GetRequiredService<PluginRegistry>();
foreach (var (plugin, entry) in loadedPlugins)
{
    foreach (var registration in plugin.Registrations)
        ServiceTypeRegistry.Register(registration.Type, registration.Factory);
    pluginRegistry.Add(plugin);

    if (entry.StaticAssetsPath is not null)
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(entry.StaticAssetsPath), RequestPath = entry.StaticAssetsRequestPath });
}
```

Plugins load **before** `builder.Build()` (so `ConfigureServices` can still register into the container) but their registrations only get wired into `ServiceTypeRegistry`/`PluginRegistry` **after** `Build()` (since the factory closures capture `app.Services`). Adding a whole new service type from a brand new library never touches this file — only `plugins.json`.

### `ServiceOrchestrator` (`Services/ServiceOrchestrator.cs`)

Singleton; owns one `IOrchestratedService` per entry in `ServiceStore.List()`, and implements both `IServiceRegistry` (so it's exactly what every service's `OrchestrationContext.Services` looks siblings up through) and `IOrchestratorAccess` (so a plugin's own UI can depend on it without a compile-time reference to this concrete type). Everything the pages and the API do goes through this:

- `GetOrCreateAsync(name)` — lazy get-or-create-and-load (via `ServiceTypeRegistry.Create` + `ServiceStore.Resolve`), guarded by a `SemaphoreSlim` so concurrent callers can't create the same service twice.
- `RefreshAsync()`/`GetAllAsync()` — rescan `ServiceStore.List()`, register anything new (so a service dropped into the folder by hand shows up without a restart — it still needs a manual Start), and drop/stop anything whose file disappeared. A service whose `"type"` has no loaded plugin (e.g. its plugin entry was removed from `plugins.json`) is logged as a warning and skipped rather than thrown — one missing plugin must never take the whole host down, since that's exactly the situation the "remove a plugin" scenario produces on every restart until the config is fixed.
- `SetAutostartAsync(name, value)` — patches just the `"autostart"` key of that service's config file (`JsonNode`, not a full round-trip) and mirrors it onto the live instance via `SetAutostart`.
- `CreateAsync(name, type)` — validates the name (rejects empty/`/`/`\`/`..`, appends `.json`, fails on a duplicate) and the type (must be registered), writes a minimal valid config (`{ "type": ..., "autostart": false }`, plus empty `steps`/`pipes`/`scale` for `"flow"` specifically since that's the one type with required extra fields today), then registers it.
- `DeleteAsync(name)` — stops the instance *before* deleting its file.
- `ReloadAsync(name)` — re-`LoadAsync`s an already-tracked instance from its current file, used by the Config/Flow editors after they write a new version to disk.
- `StopAllAsync()` — stops everything in parallel, called on shutdown.

`ServiceOrchestratorHostedService` (`BackgroundService`) drives startup (`InitializeAsync`: discover everything, start what has `Autostart: true` — files that appear *later* are not retroactively autostarted) and shutdown (`StopAllAsync`).

### Minimal API (`Services/OrchestratorEndpoints.cs`)

Everything the Blazor pages can do, over HTTP too — for a future dedicated orchestration CLI (or any other external tool); the Blazor pages call `ServiceOrchestrator` in-process directly, they don't loop back through these endpoints. Guarded by the pre-existing-but-previously-unused `ApiKeyEndpointFilter`/`IApiKeyValidation` (`Services/ApiKeyEndpointFilter.cs`/`ApiKeyValidation.cs`) — send the key configured under `ApiKey` in config (see [Usage](USAGE.md#configuration)) as an `X-API-Key` header or a `?code=` query param:

| Endpoint | Does |
|---|---|
| `GET /api/services` | List every service: name, type, status, autostart |
| `POST /api/services` | Create (`{ name, type }`) |
| `DELETE /api/services/{name}` | Delete |
| `POST /api/services/{name}/start\|pause\|stop` | Lifecycle |
| `GET /api/services/{name}/snapshot` | `GetSnapshot()` |
| `GET`/`PUT /api/services/{name}/config` | Raw config file text |

### Pages

- **`/` (Home.razor)** — the service list: Name, **Type**, status badge, Start/Pause/Stop, an autostart checkbox, edit links (**Visual** only if `PluginRegistry.TryGetEditRoute` has a route for that type; **Config**, the common YAML editor, always), a delete button (JS `confirm()` gated). A name field + a type dropdown populated from `PluginRegistry.Types` (never hardcoded — a type only appears here if some loaded plugin registered it) + **+ New Service** button creates one and jumps straight into its editor. Refreshed every second via the same `Timer` pattern used throughout this app.
- **`/config/{ServiceName}` (Config.razor)** — the *common* editor, works for any service type, lives in the host itself (not a plugin): YAML ⇄ JSON via `TheIoTThing.Abstractions.FlowYamlConverter`. Save validates YAML syntax only (there's no shape to check generically across arbitrary service types); a malformed-but-syntactically-valid config surfaces as a load error from `ReloadAsync` instead, shown inline.
- **`/flow/{FlowName}` (`FlowPage.razor`, shipped inside the `TheFlowThing.Plugin` assembly, not `TheIoTThingsApp`)** — flow's own specific editor: loads/saves through `TheFlowThing.Editor.EditorFlowConverter` and hosts `FlowUILib`'s `FlowCanvas`, resolving *which* flow via `@inject IOrchestratorAccess orchestrator` (never the concrete `ServiceOrchestrator` — a plugin may only depend on `TheIoTThing.Abstractions`) and `ServiceStore`. Routable at all only because `TheFlowThing.Plugin`'s assembly is in `PluginRegistry.Assemblies` — see [Dynamic plugin loading](#dynamic-plugin-loading).

## `TheIoTThing` — the console host

Unchanged in spirit, and deliberately not generalized yet (a dedicated orchestration CLI is a separate, future piece of work) — still a [Spectre.Console.Cli](https://spectreconsole.net/cli/) app with `run [file]` (default command) and `list`, both scoped to `type == "flow"` entries in `ServiceStore.List()`, running a bare `TheFlowThing.Executive` directly (no `ServiceOrchestrator`, no `OrchestrationContext` — just `LoadFromFileAsync` → `StartAsync` → block on a keypress → `StopAsync`, one `Executive` per process invocation). Uses [Spectre.Console](https://spectreconsole.net/) purely for presentation.

## Configuration

Every environment-specific file lives outside the repo, under `D:\Configurations\TheIoTThing` (see [Usage](USAGE.md#configuration) for the exact contents). Services themselves live one level deeper, under `Services\` (`ServiceStore.ServicesDirectory`) — no separate settings file mapping names to files, since every file *is* a service, self-describing its `type` and `autostart`. `plugins.json`, directly under `ConfigDirectory`, is what makes those `"type"`s mean anything at all — see [Dynamic plugin loading](#dynamic-plugin-loading).

## Flow-specific internals

Everything about how a *flow* actually works — the steps/pipes model, the full step type table, the `node-script`/`pysharp`/`api-output` contracts, the editor↔FlowDef bridge, YAML conversion mechanics — now lives in **[`src/TheFlowThing/ARCHITECTURE.md`](src/TheFlowThing/ARCHITECTURE.md)**, since `TheFlowThing` is "just" one pluggable service library now, not the whole system.
