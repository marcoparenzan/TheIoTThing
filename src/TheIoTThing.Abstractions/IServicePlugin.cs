using Microsoft.Extensions.DependencyInjection;

namespace TheIoTThing.Abstractions;

/// <summary>
/// One service type a plugin assembly registers: the "type" key ServiceStore's "type" field maps to,
/// a factory that creates a fresh IOrchestratedService instance, and (optionally) the route template
/// of a specific editor page the plugin itself ships (e.g. "/flow/{0}") — a plugin without one only
/// ever gets the common Config (YAML) editor every service type gets for free.
///
/// Factory takes the host's IServiceProvider so a plugin can resolve an ILogger&lt;T&gt;/other shared
/// framework services for its instance (safe: Microsoft.Extensions.* stays a shared assembly across
/// the plugin/host AssemblyLoadContexts — see PluginLoader.cs) without the plugin needing its own DI
/// container or referencing the host's concrete services.
/// </summary>
public record ServicePluginRegistration(
    string Type,
    Func<IServiceProvider, IOrchestratedService> Factory,
    string? EditorRouteTemplate = null);

/// <summary>
/// The one thing every plugin assembly must expose — discovered by reflection after PluginLoader
/// loads the assembly into its own AssemblyLoadContext (see TheIoTThingsApp/Services/PluginLoader.cs).
/// Implement this on a public, non-abstract, parameterless-constructible class; a plugin can register
/// more than one service type from the same assembly.
/// </summary>
public interface IServicePlugin
{
    IEnumerable<ServicePluginRegistration> GetRegistrations();

    /// <summary>
    /// Register whatever DI services this plugin's own UI needs (e.g. FlowUILib's FlowUIInterop for
    /// FlowCanvas's JS interop) — called against the host's IServiceCollection before it's built, since
    /// the host can't write these registrations itself without referencing the plugin's concrete types.
    /// Default no-op: most plugins (anything without its own UI) don't need this.
    /// </summary>
    void ConfigureServices(IServiceCollection services) { }
}
