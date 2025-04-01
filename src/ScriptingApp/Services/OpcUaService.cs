using OpcUaLib.Client;
using RoslynLib;
using System.Xml.Linq;

namespace ScriptingApp.Services;

public class OpcUaService: BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ctx = new ScriptContext()
            .Double("Boiler1", "ns=5;i=1283")
        ;

        var executive = CSharpExecutive<bool>.New("""

            var v1 = Get<double>("Boiler1");

            var v2 = v1*2;

            WriteLine($"v2={v2}");

        """, ctx);

        OpcClient client = new();

        await client.ConnectAsync(
            name: "test",
            address: $"opc.tcp://localhost:62541/Quickstarts/ReferenceServer"
            //Username = configuration.OpcUA.Username,
            //Password = configuration.OpcUA.Password,
            //SecurityPolicy = SecurityPolicy.Basic256,
            //MessageSecurity = MessageSecurity.SignAndEncrypt
        );

        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
        while (await timer.WaitForNextTickAsync())
        {
            ctx.Variables((name, nodeId) =>
            {
                var value = client.ReadNodeId(nodeId);
                ctx[name] = value;
            });

            var state = await executive.RunAsync();
        }

        client.Disconnect();
    }
}
