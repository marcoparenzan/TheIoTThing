using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using TheIoTThing.Abstractions;
using TheIoTThingsApp.Components;
using TheIoTThingsApp.Services;

var builder = WebApplication.CreateBuilder(args);
IConfigurationBuilder configBuilder = builder.Configuration;
var tenantConfigurationFile = Environment.GetEnvironmentVariable("TenantConfigurationFile");
var configurationReady = false;
if (!string.IsNullOrWhiteSpace(tenantConfigurationFile))
{
    try
    {
        if (tenantConfigurationFile.StartsWith("http"))
        {
            Console.WriteLine($"Loading configuration from web {tenantConfigurationFile}");
            using var configHttpClient = new HttpClient();
            using var stream = await configHttpClient.GetStreamAsync(tenantConfigurationFile);
            configBuilder = configBuilder.AddJsonStream(stream);
            stream.Close();
        }
        else
        {
            Console.WriteLine($"Loading configuration from file system {tenantConfigurationFile}");
            configBuilder = configBuilder.AddJsonFile(tenantConfigurationFile);
        }
        configurationReady = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed loading configuration {tenantConfigurationFile}");
    }
}

if (!configurationReady)
{
    Console.WriteLine($"Failed to load configuration. Startup interrupted.");
    return;
}

//var config = configBuilder.Build();

//builder.Services.AddScoped<BlocklyLib.BlocklyJsInterop>();
//builder.Services.AddHostedService<OpcUaService>();

//builder.Services.AddHostedService<TheIoTThingsApp.Services.DeviceEventGridService>();
//builder.Services.AddHostedService<TheIoTThingsApp.Services.DeviceIoTHubService>();

builder.Services.AddSingleton<IApiKeyValidation, ApiKeyValidation>();

builder.Services.AddSingleton<ServiceOrchestrator>();
builder.Services.AddSingleton<IOrchestratorAccess>(sp => sp.GetRequiredService<ServiceOrchestrator>());
builder.Services.AddHostedService<ServiceOrchestratorHostedService>();
builder.Services.AddSingleton<PluginRegistry>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddProblemDetails();

// Composition root: the ONLY place any concrete service-type plugin is even touched, and only by
// reflection — which plugins exist comes entirely from plugins.json, not from a ProjectReference or a
// hardcoded type name anywhere in this project. Loading happens before Build() so a plugin's own
// ConfigureServices (e.g. FlowUILib's FlowUIInterop, needed by its FlowCanvas component) can still
// register into the container — after Build() it's too late to add anything.
var pluginsConfigPath = Path.Combine(ServiceStore.ConfigDirectory, "plugins.json");
var loadedPlugins = new List<(LoadedPlugin Plugin, PluginConfigEntry Entry)>();
if (File.Exists(pluginsConfigPath))
{
    var pluginsConfigJson = await File.ReadAllTextAsync(pluginsConfigPath);
    var pluginsConfig = JsonSerializer.Deserialize<PluginsConfig>(pluginsConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    foreach (var entry in pluginsConfig?.Plugins ?? [])
    {
        var loaded = PluginLoader.Load(entry.Assembly, builder.Services);
        loadedPlugins.Add((loaded, entry));
    }
}

var app = builder.Build();

var pluginRegistry = app.Services.GetRequiredService<PluginRegistry>();
foreach (var (plugin, entry) in loadedPlugins)
{
    foreach (var registration in plugin.Registrations)
    {
        ServiceTypeRegistry.Register(registration.Type, registration.Factory);
    }
    pluginRegistry.Add(plugin);

    if (entry.StaticAssetsPath is not null && entry.StaticAssetsRequestPath is not null)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(entry.StaticAssetsPath),
            RequestPath = entry.StaticAssetsRequestPath
        });
    }
}

app.MapOrchestratorApi();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(pluginRegistry.Assemblies.ToArray());

app.Run();
