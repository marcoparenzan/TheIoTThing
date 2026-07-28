# TheFlowThing

`TheFlowThing` is one pluggable service type for [TheIoTThing orchestrator](../../ARCHITECTURE.md) — "flow", a dataflow graph of **steps** connected by **pipes**, evaluated on a fixed tick. This doc covers everything specific to flows; see the root [Architecture](../../ARCHITECTURE.md) for the orchestrator itself (`IOrchestratedService`, `ServiceOrchestrator`, the API, multi-service hosting) and [Usage](../../USAGE.md) for running things day to day.

This library is pure flow logic — it has no `IServicePlugin` and is never loaded directly by `TheIoTThingsApp`. The actual thing that goes into `plugins.json` is the sibling project `TheFlowThing.Plugin`, which bundles this library together with `FlowUILib` (the visual editor) behind `FlowServicePlugin : IServicePlugin` — see [Dynamic plugin loading](../../ARCHITECTURE.md#dynamic-plugin-loading) in the root doc for why the split exists and how loading works. The one exception is `TheIoTThing` (the console host), which references `TheFlowThing` directly — it isn't a plugin host at all, just a flow-only CLI.

## The flow model (`TheFlowThing.Abstractions`)

- `Def` — base of every step/pipe definition: `Id`, `Type`, `Name`, `Color`, `Script`.
- `StepDef` / `StepDef<TProperties>` — a node in the graph. Has `X`/`Y`/`Width`/`Height` (canvas layout), `Inputs`/`Outputs` (named connectors), and a strongly-typed `Properties` object specific to the step type (e.g. `NodeScriptPropertiesDef.Code`).
- `PipeDef` / `PipeDef<TProperties>` — a directed edge: `SourceStep`/`SourceOutput` → `TargetStep`/`TargetInput`.
- `State` — the runtime counterpart of a `Def`: `StepState`/`PipeState` carry live values and implement `AdvanceAsync(QuantumOfTime, inputValues) -> outputValues`, the one method every step type must implement to actually do something.
- `FlowDef` — the whole flow: `Type` (always `"flow"` — the orchestrator-level discriminator `ServiceStore` reads; unrelated to, and one level above, each *step's own* `Type` like `"data-source"` — easy to conflate but a different concept at a different level), `Steps`, `Pipes`, `Scale` (canvas zoom), `Autostart`. `FlowDef.CreateState()` turns a deserialized `FlowDef` into a `FlowState` ready to run.

## `Executive` — the flow service

`Executive` (`src/TheFlowThing/Executive.cs`) is `TheIoTThing.Abstractions.TickingService` specialized for flows — the `Stopped/Running/Paused` state machine and the tick loop itself live in the shared base class (see the root architecture doc); `Executive` only owns:

- **`LoadAsync(path, context)`**/**`LoadFromFileAsync(path)`** — reads a `FlowDef` JSON file and rebuilds the in-memory `FlowState`. Can be called at any time, including while `Paused`, to pick up edits without losing the paused state. `LoadFromFileAsync` (no `OrchestrationContext`) is the standalone entry point the console host and tests use directly.
- **`OnTickAsync()`** (once a second — `TickInterval => TimeSpan.FromSeconds(1)`) — walks `state.Steps` in order, gathers each step's `inputValues` from its inbound pipes, calls `AdvanceAsync`, and pushes the result onto the outbound pipes (`pipe.NextValue` → `pipe.Update()` at the end of the tick, so all steps in a tick observe the *previous* tick's values — a synchronous, single-threaded dataflow update, not true parallel stream processing). On any exception, the step index resets to 0 so the *next* tick restarts the walk from the top rather than resuming mid-flow.
- **`GetSnapshot()`** — returns the current `FlowState`, for a sibling service that wants to inspect a flow's pipe values.

Any exception a step's `AdvanceAsync` throws (`NotImplementedException` for a stub, a Jint/PySharp error, ...) is caught per-tick by the base class, logged via `ILogger<Executive>`, and the loop continues — one broken step does not stop the flow.

## Step types (`TheFlowThing.Steps`)

Registered in `TheFlowThing.Serialization.DefaultStepDefConverter` under a `type` key used both in the `FlowDef` JSON and by the editor bridge. Category matches the visual editor's palette section (see [Editing a flow](#editing-a-flow) below); `processor` and `timer` have no palette entry (no icon → not reachable from the visual editor, only from raw JSON/YAML).

| `type` | Category | Properties | Status |
|---|---|---|---|
| `data-source` | input | `source: string` | **implemented** — emits `source` verbatim on every output, every tick |
| `api-input` | input | `url: string`, `method: string` | stub |
| `file-reader` | input | `path: string`, `format: string` | stub |
| `sensor` | input | `sensorId: string`, `interval: long` | stub |
| `manual-input` | input | `fields: string[]` | stub |
| `transform` | process | `mapping: Dictionary<string, object>` | stub |
| `filter` | process | `condition: string` | stub |
| `aggregate` | process | `function: string`, `field: string` | stub |
| `condition` | process | `if: string`, `then: string`, `else: string` | stub |
| `node-script` | process | `code: string` (JavaScript) | **implemented** — see [contract](#node-script-contract) |
| `pysharp` | process | `code: string` (Python) | **implemented** — see [contract](#pysharp-contract) |
| `data-output` | output | `destination: string` | stub |
| `api-output` | output | `url: string`, `method: string` | **implemented** — see [contract](#api-output) |
| `file-writer` | output | `path: string`, `format: string` | stub |
| `display` | output | `template: string` | stub |
| `notification` | output | `channel: string`, `message: string` | stub |
| `processor` | — (no palette entry) | *(none)* | **implemented** — passes its first input through to its first output |
| `timer` | — (no palette entry) | `intervalMilliseconds: long` | stub |

A stub's `AdvanceAsync` just `throw new NotImplementedException()` — caught and logged by `Executive` each tick, not fatal. They need integrations that weren't specified yet (hardware sensors, notification channels, MQTT/OPC UA transports, an expression language for `transform`/`filter`/`condition`/`aggregate`...). `node-script`/`pysharp` are the escape hatch until they are: either language can script any of those behaviors today.

### `node-script` contract

Connector ids (typically `"in"`/`"out"`) aren't guaranteed to be valid, non-reserved JavaScript identifiers (`in` is a JS keyword), so a script does **not** see its inputs as bare globals. Instead:

- Inputs are exposed as a global `inputs` object, keyed by connector id: `inputs.in`.
- The script must write its outputs into a global `outputs` object, keyed by connector id: `outputs.out = ...`.
- The engine ([Jint](https://github.com/sebastienros/jint)) runs with a 5-second timeout so a runaway script can't hang the tick loop forever; it has no filesystem/network/process access unless explicitly added.

```js
outputs.out = inputs.in.toUpperCase();
```

### `pysharp` contract

Same reasoning as `node-script` (`in` is a Python keyword too — `for x in y`), same shape, just Python: the step prepends an `inputs = {...}` dict literal (built from `inputValues`) and an empty `outputs = {}` ahead of `Properties.Code`, runs the whole thing as one module via `new PyEngine(TextWriter.Null).Run(script, "flow")`, then reads `outputs[id]` back out of the executed module's `Dict` for each declared output.

```python
outputs['out'] = inputs['in'].upper()
```

[PySharp](https://www.nuget.org/packages/PySharp.Interpreter) is a Python 3.x interpreter written from scratch in C# (no CPython dependency), consumed from `D:\Dev\NuGetLocalFeed` (see `src/NuGet.Config` and root [Usage](../../USAGE.md#adding-the-local-nuget-feed)) rather than nuget.org. Two conversions glue it to CLR-typed pipe values, both in `PySharpStepState`:

- **`ToPyLiteral`** (CLR → Python source text, for building the `inputs = {...}` literal): JSON literal syntax already *is* valid Python literal syntax for strings/numbers, so it's reused as-is via `JsonSerializer.Serialize`; only `bool`/`null` need translating to `True`/`False`/`None`.
- **`FromPyValue`** (Python runtime value → CLR, for reading `outputs` back): PySharp represents Python `str`/`float`/`bool` as plain CLR `string`/`double`/`bool` already (no wrapper types), so only `PyNone` (→ `null`) and `BigInteger` (Python ints are arbitrary-precision; narrowed to `long` when it fits, else `double`) need converting. Composite values (list/dict/tuple/...) are passed through as raw PySharp runtime objects rather than deep-converted — out of scope for what a pipe value needs today.

A Python syntax or runtime error surfaces as a `PySharpLib` exception, same as a Jint `JavaScriptException` for `node-script` — caught and logged per-tick, not fatal to the flow.

### `api-output`

Uses a single `static readonly HttpClient` (10s timeout) rather than `IHttpClientFactory` — `StepState` instances are created by `FlowDef.CreateState()` outside of any DI container, so there's no factory to inject. Steps run sequentially within a tick, so a slow endpoint delays the rest of that tick's steps; the timeout bounds how long.

## Serialization (`TheFlowThing.Serialization`)

`StepDef`/`PipeDef` are polymorphic (one C# type per `type` string), so a hand-rolled `JsonConverter<StepDef>`/`JsonConverter<PipeDef>` picks the concrete type from the `"type"` property before delegating to `JsonSerializer.Deserialize<TConcrete>`:

- `StepDefConverter`/`PipeDefConverter` — generic factory-registry base classes (`Add<TDef>(key)`) — the same shape `TheIoTThing.Abstractions.ServiceTypeRegistry` uses one level up, for service types instead of step types.
- `DefaultStepDefConverter`/`DefaultPipeDefConverter` — the concrete registries used everywhere in this solution (see the step table above; pipes only have one type, `"default"`).
- `FlowDef.CreateJsonOptions()` bundles both converters into a `JsonSerializerOptions` — the one place that configures how a flow file is read/written.

An unregistered `type` deserializes to a `null` array entry rather than throwing (`StepDefConverter.Read` returns `null` when no factory matches) — `FlowDef.CreateState()` then throws a `NullReferenceException` when it tries to call `.CreateState()` on that `null` entry. This is why the root Config editor's Save path treats a load failure as "invalid config" rather than crashing — an unknown step type should never be able to reach the flow file `Executive` loads at startup.

## The editor bridge (`TheFlowThing.Editor`)

The visual editor (`FlowUILib`) and the `FlowDef` model are two different shapes:

- **Editor document**: `{ name, version, blocks: [{ id, type: "input"|"process"|"output", name, icon, x, y, config, color }], connections: [{ id, from, to }] }` — blocks have no notion of named connectors, just one implicit input dot and one implicit output dot each.
- **FlowDef**: `{ type: "flow", steps: [...], pipes: [...], scale, autostart }` — steps have explicit `Inputs`/`Outputs` connector lists, pipes reference a specific `sourceOutput`/`targetInput` id.

Two pieces bridge them:

- **`EditorStepCatalog`** — a table mirroring the JS palette (`FlowUILib/wwwroot/flowUIInterop.js`, `BLOCK_DEFS`) that maps each palette item's **icon** (not its display name, which the user can freely rename in the properties panel) to a `(StepType, Category, Name, Icon, Color)` entry. Icon is the stable key because it's set once at block creation and never edited afterwards. Keep this table and `BLOCK_DEFS` in sync when adding a palette item.
- **`EditorFlowConverter.ToFlowDefJson`/`ToEditorJson`** — pure JSON-to-JSON transforms (via `System.Text.Json.Nodes`) between the two shapes. Every block gets a single fixed `"in"`/`"out"` connector pair (input blocks: output only; output blocks: input only; process blocks: both); a block's `config` passes straight through to a step's `properties` (and back), relying on JSON property-name matching against the step's typed `PropertiesDef` — fields that don't match the target type are silently dropped, by design (no manual per-type mapping). `ToFlowDefJson` takes an optional second argument (the existing on-disk `FlowDef` JSON) to carry `scale`/`autostart` over — the editor doesn't know about either, so without this a save from the visual editor would silently reset scale and wipe the autostart flag every time.

`FlowPage.razor` (in `TheFlowThing.Plugin`, not `TheIoTThingsApp` — see the root [Architecture doc](../../ARCHITECTURE.md#dynamic-plugin-loading) for why flow's own editor page ships inside a runtime-loaded plugin assembly rather than the host) calls `ToEditorJson` when loading the page and `ToFlowDefJson` before writing back to the flow file — so the file on disk is always canonical `FlowDef` JSON, never the editor's own shape.

## YAML editing (`TheIoTThing.Abstractions.FlowYamlConverter`)

The root Config editor (`/config/{name}`, common to every service type, living in the host itself) edits any config as YAML for a human to read/type; for a flow that means the canonical `FlowDef` JSON, round-tripped through `ToYaml`/`ToJson`. Despite the name, it's fully generic — it round-trips *any* JSON object, not just a `FlowDef` shape — which is exactly why it lives in `TheIoTThing.Abstractions` rather than here: the Config editor needs it independent of whether the flow plugin (or any plugin at all) is even loaded.

Converts via `YamlDotNet`'s node model (`YamlDocument`/`YamlMappingNode`/...), with one deliberate convention: **every JSON string is emitted double-quoted**, everything else (numbers/booleans/`null`) as a plain scalar. This is what lets the reverse conversion tell `""` apart from a bare `null` without guessing — a quoted scalar is always a string, a plain `null`/`~`/empty scalar is always `null`. A YAML syntax error raises `FlowYamlException` with a readable message instead of the raw `YamlDotNet` exception.

## `FlowUILib` — the visual editor

- **`FlowCanvas.razor`** — the Blazor wrapper: renders the toolbar/palette/canvas/properties-panel DOM, forwards an optional initial `State` (editor-shape JSON) to the JS side on first render, and exposes `[Parameter] EventCallback<string> OnSave`.
- **`FlowUIInterop.cs`** — thin JS-interop wrapper (`InitFlowAsync`, `GetFlowJsonAsync`) around the JS module.
- **`wwwroot/flowUIInterop.js`** — the actual editor: drag-and-drop from a palette, pan/zoom, drawing connections between connector dots, a YAML-based properties panel (block `config` is edited as YAML in the UI, parsed with `js-yaml` loaded lazily from a CDN). Multi-instance-safe (state keyed by `name`, nothing touches global `document` ids).
- The toolbar's **Save** button calls back into .NET via a `DotNetObjectReference<FlowCanvas>` (`NotifySaveRequested`), which raises `OnSave` — it does not download a `.json` blob to the browser. The **Open**/**New** buttons still work against local files in the browser and are unrelated to the server-side "current flow file" concept.

## Editing a flow

The canvas palette groups step types into three categories, each corresponding to a `type` key in the table above:

| Category | Palette items |
|---|---|
| Input (source, no inputs) | Data Source, API Input, File Reader, Sensor, Manual Input |
| Process (has inputs and outputs) | Transform, Filter, Aggregate, Condition, NodeScript, PySharp |
| Output (sink, no outputs) | Data Output, API Output, File Writer, Display, Notification |

A block's properties panel edits its `config` as YAML; for the four implemented types:

- **Data Source**: `source: "<any string>"`.
- **NodeScript**: `code: "<javascript>"` — see the [contract](#node-script-contract) above.
- **PySharp**: `code: "<python>"` — see the [contract](#pysharp-contract) above.
- **API Output**: `url: "..."`, `method: "POST"`.

Any block whose type is still a stub will make `Executive` log an error every tick it runs (caught, not fatal) until you either avoid using it or implement it — see below.

## Extending with a new step type

1. Add a `YourStepDef : StepDef<YourPropertiesDef>` / `YourStepState : StepState` pair under `src/TheFlowThing/Steps/` (copy an existing implemented one, e.g. `DataSourceStepDef.cs`, as a template) and implement `AdvanceAsync`.
2. Register it: `Add<YourStepDef>("your-type")` in `DefaultStepDefConverter` (`src/TheFlowThing/Serialization/DefaultStepDefConverter.cs`).
3. To make it reachable from the visual editor, add a matching entry (`StepType`, `Category`, `Name`, a **unique** `Icon`, `Color`) to `EditorStepCatalog` (`src/TheFlowThing/Editor/EditorStepCatalog.cs`) *and* a matching palette item in `BLOCK_DEFS` in `src/FlowUILib/wwwroot/flowUIInterop.js` — the icon is what ties the two together, so keep it in sync in both places.

(This is about adding a new *step* inside a flow. To add a whole new *service type* alongside "flow" — something that isn't a dataflow graph at all — see [How to build an orchestrable service](../../ARCHITECTURE.md#how-to-build-an-orchestrable-service) in the root architecture doc instead.)
