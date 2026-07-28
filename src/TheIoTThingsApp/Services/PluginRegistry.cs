using System.Reflection;

namespace TheIoTThingsApp.Services;

/// <summary>
/// What the host learned from the plugins it loaded at startup (see PluginLoader/Program.cs):
/// which service types have their own specific editor route (Home.razor's "Visual" link), and the
/// set of plugin assemblies Blazor's router needs to know about (Routes.razor's AdditionalAssemblies)
/// so a plugin-shipped @page component (e.g. FlowPage.razor) is actually routable.
/// </summary>
public class PluginRegistry
{
    readonly Dictionary<string, string> editorRouteTemplates = new();
    readonly List<string> types = [];
    readonly List<Assembly> assemblies = [];

    public IReadOnlyList<Assembly> Assemblies => assemblies;

    /// <summary>Every service type key any loaded plugin registered — what the Home page's
    /// "New Service" type picker offers, so it never hardcodes a type name either.</summary>
    public IReadOnlyList<string> Types => types;

    public void Add(LoadedPlugin plugin)
    {
        assemblies.Add(plugin.Assembly);

        foreach (var registration in plugin.Registrations)
        {
            types.Add(registration.Type);
            if (registration.EditorRouteTemplate is not null)
            {
                editorRouteTemplates[registration.Type] = registration.EditorRouteTemplate;
            }
        }
    }

    /// <summary>The specific-editor URL for a service of this type, or null if it only has the common editor.</summary>
    public string? TryGetEditRoute(string type, string name) =>
        editorRouteTemplates.TryGetValue(type, out var template) ? string.Format(template, Uri.EscapeDataString(name)) : null;
}
