using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheIoTThing.Abstractions;

namespace TheFlowThing.Plugin;

public class FlowServicePlugin : IServicePlugin
{
    public IEnumerable<ServicePluginRegistration> GetRegistrations()
    {
        yield return new ServicePluginRegistration(
            "flow",
            services => new Executive(services.GetService<ILogger<Executive>>()),
            "/flow/{0}");
    }

    // FlowCanvas (FlowUILib) needs this for its JS interop — the host can't register it itself
    // without referencing FlowUILib's concrete type, so the plugin does it here instead.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<FlowUILib.FlowUIInterop>();
    }
}
