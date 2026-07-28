using Spectre.Console.Cli;
using TheIoTThing.Commands;

var app = new CommandApp();
app.SetDefaultCommand<RunCommand>();
app.Configure(config =>
{
    config.SetApplicationName("TheIoTThing");

    config.AddCommand<RunCommand>("run")
        .WithDescription("Load and run a flow file until a key is pressed.");

    config.AddCommand<ListCommand>("list")
        .WithDescription("List the flow files available in the configuration folder.");
});

return await app.RunAsync(args);
