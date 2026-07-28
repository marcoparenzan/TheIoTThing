namespace TheIoTThingsApp.Services;

public record PluginConfigEntry(string Assembly, string? StaticAssetsPath, string? StaticAssetsRequestPath);

public record PluginsConfig(List<PluginConfigEntry> Plugins);
