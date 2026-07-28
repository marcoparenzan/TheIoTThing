using TheIoTThing.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace TheIoTThing.Commands;

public class ListCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var flows = ServiceStore.List().Where(d => d.Type == "flow").Select(d => d.Name).OrderBy(n => n).ToArray();
        if (flows.Length == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No flow services found in[/] [grey]{ServiceStore.ServicesDirectory}[/].");
            return 0;
        }

        var table = new Table().AddColumn("Flow service");
        foreach (var name in flows)
        {
            table.AddRow(name);
        }
        AnsiConsole.Write(table);

        return 0;
    }
}
