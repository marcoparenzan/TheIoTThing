namespace TheIoTThing.Abstractions;

/// <summary>
/// The minimal orchestrator surface a plugin's own UI (e.g. a routed editor page it ships) can depend
/// on via DI — plugins may only reference TheIoTThing.Abstractions, never the host project, so this is
/// what they inject instead of the concrete orchestrator type. Implemented by the host's orchestrator
/// (TheIoTThingsApp.Services.ServiceOrchestrator), registered in DI under this interface too.
/// </summary>
public interface IOrchestratorAccess
{
    Task<IOrchestratedService> GetOrCreateAsync(string name);
    Task ReloadAsync(string name);
}
