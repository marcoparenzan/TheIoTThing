namespace TheIoTThing.Abstractions;

/// <summary>
/// The contract every pluggable service type (flow, counter, ...) implements so the orchestrator can
/// load, run, and manage it without knowing anything about what the service actually does. See
/// TickingService for a base class that implements the Start/Pause/Stop state machine for services
/// that run on a fixed tick — most services should derive from that instead of implementing this
/// directly.
/// </summary>
public interface IOrchestratedService
{
    ServiceStatus Status { get; }

    bool Autostart { get; }

    /// <summary>
    /// Loads this service's config from <paramref name="path"/> (a single file, or a folder's index
    /// file — resolved by ServiceStore before this is called) and stores <paramref name="context"/>
    /// for later use (e.g. looking up a sibling service).
    /// </summary>
    Task LoadAsync(string path, OrchestrationContext context);

    Task StartAsync();
    Task PauseAsync();
    Task StopAsync();

    /// <summary>In-memory only; persisting the flag to the service's own config file is the host's job.</summary>
    void SetAutostart(bool value);

    /// <summary>
    /// An opaque, service-specific snapshot of current state/output (e.g. a flow's FlowState, a
    /// counter's current count) for a sibling that knows what to do with it. Null if not applicable.
    /// </summary>
    object? GetSnapshot();
}
