namespace TheIoTThing.Abstractions;

/// <summary>
/// Factory-by-key registry mapping a service's "type" string (e.g. "flow", "counter") to a concrete
/// IOrchestratedService implementation — same shape as TheFlowThing's own StepDefConverter.Add&lt;T&gt;(key)
/// for step types. Populated once, at the host's composition root, from whatever ServicePluginRegistration
/// entries the loaded plugins report (see PluginLoader) — this registry itself never references a
/// concrete service type, so this library stays free of any dependency on them.
/// </summary>
public static class ServiceTypeRegistry
{
    static readonly Dictionary<string, Func<IServiceProvider, IOrchestratedService>> factories = new();

    public static void Register(string type, Func<IServiceProvider, IOrchestratedService> factory)
    {
        factories[type] = factory;
    }

    public static bool IsRegistered(string type) => factories.ContainsKey(type);

    public static IOrchestratedService Create(string type, IServiceProvider services)
    {
        if (!factories.TryGetValue(type, out var factory))
        {
            throw new InvalidOperationException($"No service type registered for '{type}'.");
        }
        return factory(services);
    }
}
