using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.Loader;
using TheIoTThing.Abstractions;

namespace TheIoTThingsApp.Services;

public record LoadedPlugin(Assembly Assembly, IReadOnlyCollection<ServicePluginRegistration> Registrations);

/// <summary>
/// Loads a plugin assembly (and its private dependencies) into its own AssemblyLoadContext, then
/// reflects for its IServicePlugin implementation. This is the only place in the solution that knows
/// how plugins are physically loaded — everything downstream just sees IOrchestratedService/
/// ServicePluginRegistration/Assembly.
/// </summary>
public static class PluginLoader
{
    /// <summary>
    /// Loads the plugin and immediately calls its ConfigureServices — this must happen before the
    /// host's IServiceCollection is built, since that's the only chance to register whatever DI
    /// services the plugin's own UI needs (see IServicePlugin.ConfigureServices).
    /// </summary>
    public static LoadedPlugin Load(string assemblyPath, IServiceCollection hostServices)
    {
        var context = new PluginLoadContext(assemblyPath);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);

        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IServicePlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            ?? throw new InvalidOperationException($"No IServicePlugin implementation found in '{assemblyPath}'.");

        var plugin = (IServicePlugin)Activator.CreateInstance(pluginType)!;
        plugin.ConfigureServices(hostServices);

        return new LoadedPlugin(assembly, plugin.GetRegistrations().ToArray());
    }
}

/// <summary>
/// A plugin's own dependencies (its NuGet packages, its own supporting assemblies) resolve from its
/// build output folder via AssemblyDependencyResolver. Anything "shared" — TheIoTThing.Abstractions
/// (so IOrchestratedService/etc. are the *same type* the host checks against, not a look-alike from a
/// second copy), plus the ASP.NET Core/BCL/Microsoft.Extensions families (so a plugin's Razor page is
/// a real ComponentBase to the host's renderer and DI resolves) — falls back to the Default context's
/// already-loaded copy instead (returning null from Load tells the runtime to do that).
/// </summary>
class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: false)
{
    readonly AssemblyDependencyResolver resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsShared(assemblyName.Name)) return null;

        var path = resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    static bool IsShared(string? name) =>
        name is not null && (
            name == "TheIoTThing.Abstractions" ||
            name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.JSInterop", StringComparison.Ordinal) ||
            name.StartsWith("System.", StringComparison.Ordinal) ||
            name is "netstandard" or "mscorlib");
}
