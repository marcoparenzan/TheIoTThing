using TheIoTThing.Abstractions;

namespace TheIoTThingsApp.Services;

public record CreateServiceRequest(string Name, string Type);

/// <summary>
/// Everything the Blazor pages can do against ServiceOrchestrator, exposed over HTTP too — so a
/// future dedicated orchestration CLI (or any other external tool) can drive the same orchestrator
/// the web UI does. Blazor pages call ServiceOrchestrator in-process directly; they don't loop back
/// through these endpoints.
/// </summary>
public static class OrchestratorEndpoints
{
    public static void MapOrchestratorApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/services").AddEndpointFilter<ApiKeyEndpointFilter>();

        group.MapGet("/", async (ServiceOrchestrator orchestrator) =>
            Results.Ok(await orchestrator.GetAllAsync()));

        group.MapPost("/", async (CreateServiceRequest request, ServiceOrchestrator orchestrator) =>
        {
            try
            {
                var name = await orchestrator.CreateAsync(request.Name, request.Type);
                return Results.Ok(new { name });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{name}", async (string name, ServiceOrchestrator orchestrator) =>
        {
            await orchestrator.DeleteAsync(name);
            return Results.Ok();
        });

        group.MapPost("/{name}/start", async (string name, ServiceOrchestrator orchestrator) =>
        {
            var service = await orchestrator.GetOrCreateAsync(name);
            await service.StartAsync();
            return Results.Ok();
        });

        group.MapPost("/{name}/pause", async (string name, ServiceOrchestrator orchestrator) =>
        {
            var service = await orchestrator.GetOrCreateAsync(name);
            await service.PauseAsync();
            return Results.Ok();
        });

        group.MapPost("/{name}/stop", async (string name, ServiceOrchestrator orchestrator) =>
        {
            var service = await orchestrator.GetOrCreateAsync(name);
            await service.StopAsync();
            return Results.Ok();
        });

        group.MapGet("/{name}/snapshot", async (string name, ServiceOrchestrator orchestrator) =>
        {
            var service = await orchestrator.GetOrCreateAsync(name);
            return Results.Ok(service.GetSnapshot());
        });

        group.MapGet("/{name}/config", async (string name) =>
        {
            var path = ServiceStore.Resolve(name);
            return File.Exists(path) ? Results.Text(await File.ReadAllTextAsync(path), "application/json") : Results.NotFound();
        });

        group.MapPut("/{name}/config", async (string name, HttpRequest request, ServiceOrchestrator orchestrator) =>
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            await File.WriteAllTextAsync(ServiceStore.Resolve(name), json);
            await orchestrator.ReloadAsync(name);
            return Results.Ok();
        });
    }
}
