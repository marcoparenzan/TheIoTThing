namespace TheIoTThing.Abstractions;

/// <summary>
/// A read-only view onto the other services the orchestrator currently knows about, handed to every
/// service via its OrchestrationContext so it can look up a sibling's status/snapshot. Implemented by
/// the host's orchestrator (see TheIoTThingsApp.Services.ServiceOrchestrator) — this library only
/// depends on the shape, never a concrete implementation.
/// </summary>
public interface IServiceRegistry
{
    IReadOnlyCollection<string> Names { get; }
    IOrchestratedService? TryGet(string name);
}

/// <summary>
/// The "orchestration state" injected into every service when it's loaded: who it is, where its
/// config lives, and a registry to look up its siblings.
/// </summary>
public class OrchestrationContext(string serviceName, string servicePath, IServiceRegistry services)
{
    public string ServiceName { get; } = serviceName;
    public string ServicePath { get; } = servicePath;
    public IServiceRegistry Services { get; } = services;
}
