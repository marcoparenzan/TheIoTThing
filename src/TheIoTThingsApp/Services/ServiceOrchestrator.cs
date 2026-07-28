using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TheIoTThing.Abstractions;

namespace TheIoTThingsApp.Services;

public record ServiceInfo(string Name, string Type, ServiceStatus Status, bool Autostart);

/// <summary>
/// Owns one IOrchestratedService per service found under ServiceStore.ServicesDirectory, so several
/// services (of any registered type) can run concurrently in the same process. Also implements
/// IServiceRegistry (so it's what every service's OrchestrationContext looks siblings up through) and
/// IOrchestratorAccess (so a plugin's own UI can depend on it without referencing this concrete type).
/// Registered as a singleton; ServiceOrchestratorHostedService drives its startup/shutdown.
/// </summary>
public class ServiceOrchestrator(IServiceProvider serviceProvider, ILogger<ServiceOrchestrator> logger) : IServiceRegistry, IOrchestratorAccess
{
    static readonly JsonSerializerOptions writeOptions = new() { WriteIndented = true };

    readonly ConcurrentDictionary<string, IOrchestratedService> services = new();
    readonly ConcurrentDictionary<string, string> types = new();
    readonly SemaphoreSlim gate = new(1, 1);

    public IReadOnlyCollection<string> Names => services.Keys.ToArray();

    public IOrchestratedService? TryGet(string name) => services.TryGetValue(name, out var service) ? service : null;

    public async Task InitializeAsync()
    {
        await RefreshAsync();

        foreach (var service in services.Values)
        {
            if (service.Autostart)
            {
                await service.StartAsync();
            }
        }
    }

    public async Task RefreshAsync()
    {
        var descriptors = ServiceStore.List().ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in descriptors.Values)
        {
            if (!services.ContainsKey(descriptor.Name))
            {
                // A service on disk whose type's plugin isn't currently loaded (e.g. removed from
                // plugins.json) shouldn't take the whole host down — it just stays invisible until its
                // plugin is loaded again.
                if (!ServiceTypeRegistry.IsRegistered(descriptor.Type))
                {
                    logger.LogWarning("Skipping service '{Name}': no plugin registered for type '{Type}'.", descriptor.Name, descriptor.Type);
                    continue;
                }

                await GetOrCreateAsync(descriptor.Name);
            }
        }

        // A service removed from disk outside the app (or by DeleteAsync racing a concurrent
        // GetOrCreateAsync) shouldn't linger in the registry.
        foreach (var trackedName in services.Keys)
        {
            if (!descriptors.ContainsKey(trackedName) && services.TryRemove(trackedName, out var stale))
            {
                types.TryRemove(trackedName, out _);
                await stale.StopAsync();
            }
        }
    }

    public async Task<IOrchestratedService> GetOrCreateAsync(string name)
    {
        if (services.TryGetValue(name, out var existing)) return existing;

        await gate.WaitAsync();
        try
        {
            if (services.TryGetValue(name, out existing)) return existing;

            var descriptor = ServiceStore.List().FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No service named '{name}' found in {ServiceStore.ServicesDirectory}.");

            var service = ServiceTypeRegistry.Create(descriptor.Type, serviceProvider);
            var context = new OrchestrationContext(descriptor.Name, descriptor.ConfigPath, this);
            await service.LoadAsync(descriptor.ConfigPath, context);

            services[name] = service;
            types[name] = descriptor.Type;
            return service;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Re-reads a service's config file into its already-running (or stopped/paused) instance,
    /// e.g. after the Config/Flow editors write a new version of the file to disk.</summary>
    public async Task ReloadAsync(string name)
    {
        var descriptor = ServiceStore.List().FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
        if (descriptor is null) return;

        var service = await GetOrCreateAsync(name);
        var context = new OrchestrationContext(descriptor.Name, descriptor.ConfigPath, this);
        await service.LoadAsync(descriptor.ConfigPath, context);
    }

    public async Task<IReadOnlyCollection<ServiceInfo>> GetAllAsync()
    {
        await RefreshAsync();

        return services
            .Select(kv => new ServiceInfo(kv.Key, types.GetValueOrDefault(kv.Key, "?"), kv.Value.Status, kv.Value.Autostart))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task SetAutostartAsync(string name, bool autostart)
    {
        var service = await GetOrCreateAsync(name);

        var path = ServiceStore.Resolve(name);
        var json = await File.ReadAllTextAsync(path);
        var node = JsonNode.Parse(json)!.AsObject();
        node["autostart"] = autostart;
        await File.WriteAllTextAsync(path, node.ToJsonString(writeOptions));

        service.SetAutostart(autostart);
    }

    public async Task<string> CreateAsync(string name, string type)
    {
        if (!ServiceTypeRegistry.IsRegistered(type))
        {
            throw new InvalidOperationException($"Unknown service type '{type}'.");
        }

        var fileName = NormalizeFileName(name);
        var path = ServiceStore.Resolve(fileName);

        if (File.Exists(path))
        {
            throw new InvalidOperationException($"A service named '{fileName}' already exists.");
        }

        Directory.CreateDirectory(ServiceStore.ServicesDirectory);

        var config = new JsonObject { ["type"] = type, ["autostart"] = false };
        if (type == "flow")
        {
            config["steps"] = new JsonArray();
            config["pipes"] = new JsonArray();
            config["scale"] = 1;
        }
        await File.WriteAllTextAsync(path, config.ToJsonString(writeOptions));

        await GetOrCreateAsync(fileName);
        return fileName;
    }

    public async Task DeleteAsync(string name)
    {
        if (services.TryRemove(name, out var service))
        {
            types.TryRemove(name, out _);
            await service.StopAsync();
        }

        var path = ServiceStore.Resolve(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    static string NormalizeFileName(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Service name cannot be empty.");
        }
        if (name.IndexOfAny(['/', '\\']) >= 0 || name.Contains(".."))
        {
            throw new ArgumentException("Service name cannot contain path separators or '..'.");
        }

        return name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? name : name + ".json";
    }

    public Task StopAllAsync() => Task.WhenAll(services.Values.Select(e => e.StopAsync()));
}
