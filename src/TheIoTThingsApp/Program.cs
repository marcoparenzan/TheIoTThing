using MudBlazor.Services;
using TheIoTThingsApp.Components;

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
builder.Services.AddHostedService<TheIoTThingsApp.Services.DeviceIoTHubService>();

builder.Services.AddMudServices();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddProblemDetails();

var app = builder.Build();

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
    .AddInteractiveServerRenderMode();

app.Run();
