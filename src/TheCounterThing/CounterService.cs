using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheIoTThing.Abstractions;

namespace TheCounterThing;

/// <summary>
/// A minimal, deliberately Flow-unrelated service type: ticks on an interval and increments a
/// counter. Exists to prove the orchestrator's plugin story end-to-end — see the "how to build an
/// orchestrable service" doc, of which this is the worked example.
/// </summary>
public class CounterService(ILogger<CounterService>? logger = null) : TickingService(logger)
{
    CounterConfig? config;
    int count;

    protected override TimeSpan TickInterval => TimeSpan.FromMilliseconds(config?.IntervalMilliseconds ?? 1000);

    protected override bool IsLoaded => config is not null;

    public override async Task LoadAsync(string path, OrchestrationContext context)
    {
        var json = await File.ReadAllTextAsync(path);
        config = JsonSerializer.Deserialize<CounterConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new CounterConfig();
        count = 0;
        SetAutostart(config.Autostart);
    }

    public override object? GetSnapshot() => count;

    protected override Task OnTickAsync()
    {
        count++;
        return Task.CompletedTask;
    }
}

public class CounterConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "counter";

    [JsonPropertyName("intervalMilliseconds")]
    public int IntervalMilliseconds { get; set; } = 1000;

    [JsonPropertyName("autostart")]
    public bool Autostart { get; set; }
}
