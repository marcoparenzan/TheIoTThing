namespace TheFlowThing.Editor;

/// <summary>
/// Bridges the visual editor's block palette (FlowUILib/wwwroot/flowUIInterop.js, BLOCK_DEFS)
/// with the step type keys registered in TheFlowThing.Serialization.DefaultStepDefConverter.
/// Keep the two definitions in sync when adding/removing a palette item.
/// </summary>
public static class EditorStepCatalog
{
    public sealed record Entry(string StepType, string Category, string Name, string Icon, string Color);

    static readonly Entry[] entries =
    [
        new("data-source",  "input",   "Data Source",  "🗄️", "#22c55e"),
        new("api-input",    "input",   "API Input",    "🌐", "#22c55e"),
        new("file-reader",  "input",   "File Reader",  "📖", "#22c55e"),
        new("sensor",       "input",   "Sensor",       "📡", "#22c55e"),
        new("manual-input", "input",   "Manual Input", "✏️", "#22c55e"),

        new("transform",    "process", "Transform",    "🔄", "#3b82f6"),
        new("filter",       "process", "Filter",       "🔍", "#3b82f6"),
        new("aggregate",    "process", "Aggregate",    "📊", "#3b82f6"),
        new("condition",    "process", "Condition",    "🔀", "#3b82f6"),
        new("node-script",  "process", "NodeScript",   "📜", "#3b82f6"),
        new("pysharp",      "process", "PySharp",      "🐍", "#3b82f6"),

        new("data-output",  "output",  "Data Output",  "💾", "#f97316"),
        new("api-output",   "output",  "API Output",   "📤", "#f97316"),
        new("file-writer",  "output",  "File Writer",  "📝", "#f97316"),
        new("display",      "output",  "Display",      "🖥️", "#f97316"),
        new("notification", "output",  "Notification", "🔔", "#f97316"),
    ];

    // Icon is set once from the palette and never edited afterwards in the properties panel,
    // so (unlike the free-text "name") it reliably identifies which specific step type a block was created from.
    public static IReadOnlyDictionary<string, Entry> ByIcon { get; } =
        entries.ToDictionary(e => e.Icon);

    public static IReadOnlyDictionary<string, Entry> ByStepType { get; } =
        entries.ToDictionary(e => e.StepType, StringComparer.OrdinalIgnoreCase);
}
