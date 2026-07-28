using TheFlowThing;
using TheIoTThing.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace TheIoTThing.Commands;

public class RunCommand : AsyncCommand<RunCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Flow service to run. A bare filename is resolved against D:\\Configurations\\TheIoTThing\\Services. Omit to pick one interactively.")]
        [CommandArgument(0, "[file]")]
        public string? File { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Write(new FigletText("TheIoTThing").Color(Color.Blue));

        var name = settings.File ?? SelectFlow();
        if (name is null)
        {
            AnsiConsole.MarkupLine($"[red]No flow services found in[/] [grey]{ServiceStore.ServicesDirectory}[/].");
            return 1;
        }

        var path = ServiceStore.Resolve(name);
        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]File not found:[/] {path}");
            return 1;
        }

        var executive = new Executive();

        AnsiConsole.MarkupLine($"[grey]Loading flow from[/] [yellow]{path}[/]...");
        await executive.LoadFromFileAsync(path);
        await executive.StartAsync();
        AnsiConsole.MarkupLine("[green]Flow running.[/]");

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Running — press any key to stop", async _ =>
            {
                await Task.Run(() => Console.ReadKey(intercept: true));
            });

        AnsiConsole.MarkupLine("[grey]Stopping...[/]");
        await executive.StopAsync();
        AnsiConsole.MarkupLine("[red]Stopped.[/]");

        return 0;
    }

    static string? SelectFlow()
    {
        var flows = ServiceStore.List().Where(d => d.Type == "flow").Select(d => d.Name).OrderBy(n => n).ToArray();
        if (flows.Length == 0) return null;
        if (flows.Length == 1) return flows[0];

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a flow to run:")
                .AddChoices(flows));
    }
}
