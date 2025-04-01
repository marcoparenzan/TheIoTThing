using OpcUaLib;
using RoslynLib;

namespace TheIoTThingsApp.Services;

public class OpcUaService: BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ctx = new ScriptContext()
            .Numeric("boiler1_CC1001", "ns=5;i=1283")
            .Numeric("boiler1_FC1001", "ns=5;i=1274")
        ;

        var code = """
            {
                //MessageStatus(Numeric("boiler1_CC1001") > -0.5,(Numeric("boiler1_FC1001") > -0.1) && (Numeric("boiler1_CC1001") > -0.5));
        
                var boiler1_CC1001 = Numeric("boiler1_CC1001");
                var boiler1_FC1001 = Numeric("boiler1_FC1001");
        
                var power = boiler1_CC1001 > -0.5;
                var running = (boiler1_FC1001 > -0.1) && (boiler1_CC1001 > -0.5);
        

                WriteLine($"boiler1_CC1001={boiler1_CC1001} boiler1_FC1001={boiler1_FC1001} power={power} running={running}");
            }
        """;

        //var json = await File.ReadAllTextAsync("workspace-TEST01.json");
        //var def = JsonSerializer.Deserialize<WorkspaceDef>(json);
        //var tsp = new CSharpTranspiler();
        //var xx = tsp.Transpile(def);
        //code = xx.ToFullString();

        var executive = CSharpExecutive<bool>.New(code, ctx);

        //OpcClient client = new();

        //await client.ConnectAsync(
        //    name: "test",
        //    address: $"opc.tcp://localhost:62541/Quickstarts/ReferenceServer"
        //);

        //var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
        //while (await timer.WaitForNextTickAsync())
        //{
        //    ctx.Variables((name, nodeId) =>
        //    {
        //        var value = client.ReadNodeId(nodeId);
        //        ctx[name] = value;
        //    });

        //    try
        //    {
        //        var state = await executive.RunAsync();
        //    }
        //    catch(Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        //client.Disconnect();
    }
}
