using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheIoTThing.Abstractions;

namespace TheCounterThing;

public class CounterServicePlugin : IServicePlugin
{
    public IEnumerable<ServicePluginRegistration> GetRegistrations()
    {
        // No EditorRouteTemplate: counter has no specific UI, so it only ever gets the common
        // Config (YAML) editor every service type gets for free.
        yield return new ServicePluginRegistration(
            "counter",
            services => new CounterService(services.GetService<ILogger<CounterService>>()));
    }
}
