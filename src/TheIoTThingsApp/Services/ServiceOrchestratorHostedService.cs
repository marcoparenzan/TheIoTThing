using TheIoTThing.Abstractions;

namespace TheIoTThingsApp.Services;

public class ServiceOrchestratorHostedService(
    ServiceOrchestrator orchestrator,
    ILogger<ServiceOrchestratorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Discovering services in {ServicesDirectory}", ServiceStore.ServicesDirectory);
        await orchestrator.InitializeAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await orchestrator.StopAllAsync();
        await base.StopAsync(cancellationToken);
    }
}
