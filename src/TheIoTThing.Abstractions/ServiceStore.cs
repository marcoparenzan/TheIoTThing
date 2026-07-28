using System.Text.Json;

namespace TheIoTThing.Abstractions;

/// <summary>A service found under ServiceStore.ServicesDirectory, with the type it self-describes as.</summary>
public record ServiceDescriptor(string Name, string Type, string ConfigPath);

/// <summary>
/// Where services live on disk, shared by every host. An orchestration is a folder of services —
/// each one is either a single file (its whole config in one place) or a folder containing an
/// "index.json"/"index.yaml" plus whatever other resource files the service itself needs. Either way
/// the entry self-describes via a top-level "type" field, which is how the orchestrator picks which
/// registered IOrchestratedService implementation (see ServiceTypeRegistry) owns it.
/// </summary>
public static class ServiceStore
{
    public const string ConfigDirectory = @"D:\Configurations\TheIoTThing";
    public const string ServicesDirectory = ConfigDirectory + @"\Services";

    public static IReadOnlyCollection<ServiceDescriptor> List()
    {
        if (!Directory.Exists(ServicesDirectory)) return [];

        var descriptors = new List<ServiceDescriptor>();

        foreach (var entry in Directory.EnumerateFileSystemEntries(ServicesDirectory).OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(entry))
            {
                var index = FindIndexFile(entry);
                if (index is null) continue;

                var type = TryReadType(index);
                if (type is null) continue;

                descriptors.Add(new ServiceDescriptor(Path.GetFileName(entry), type, index));
            }
            else if (entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || entry.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            {
                var type = TryReadType(entry);
                if (type is null) continue;

                // Keep the extension in the name: Resolve/CreateAsync/routes all key services by their
                // literal file name (e.g. "flow.json"), matching the folder case's index-file sibling
                // resources being addressed relative to a name that is exactly the directory name.
                descriptors.Add(new ServiceDescriptor(Path.GetFileName(entry), type, entry));
            }
        }

        return descriptors;
    }

    /// <summary>Resolves a bare service name to its config path (the file itself, or a folder's index file).</summary>
    public static string Resolve(string name)
    {
        if (Path.IsPathRooted(name)) return name;

        var direct = Path.Combine(ServicesDirectory, name);
        if (File.Exists(direct)) return direct;

        foreach (var ext in new[] { ".json", ".yaml" })
        {
            var withExt = Path.Combine(ServicesDirectory, name + ext);
            if (File.Exists(withExt)) return withExt;
        }

        var folder = Path.Combine(ServicesDirectory, name);
        var index = Directory.Exists(folder) ? FindIndexFile(folder) : null;
        if (index is not null) return index;

        // Not found yet (e.g. about to be created) — default to a plain <name>.json file.
        return direct.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || direct.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            ? direct
            : direct + ".json";
    }

    static string? FindIndexFile(string folder)
    {
        foreach (var candidate in new[] { "index.json", "index.yaml" })
        {
            var path = Path.Combine(folder, candidate);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    static string? TryReadType(string jsonPath)
    {
        // Only JSON is sniffed here (an "index.yaml" entry is expected to be authored/edited as JSON
        // internally via the same file, same as flow files today — YAML is a view, not a storage format).
        if (!jsonPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
                ? typeEl.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
