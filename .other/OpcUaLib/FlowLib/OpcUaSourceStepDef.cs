using FlowLib.Abstractions;
using Opc.Ua;
using System.Text.Json.Serialization;

namespace OpcUaLib.FlowLib;

public class OpcUaSourceStepDef : StepDef<OpcUaSourcePropertiesDef>
{
    public override OpcUaSourceStepState CreateState()
    {
        return new OpcUaSourceStepState(this)
        {
        };
    }
}

public class OpcUaSourceStepState(OpcUaSourceStepDef def) : StepState
{
    private OpcUaClient client;

    public override async Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        if (this.client is null)
        {
            this.client = new OpcUaClient();
            await client.ConnectAsync();
        }

        var outputValues = new Dictionary<string, object>();
        try
        {
            var argIn = def.Properties.NodeIds.First().ToString(); // "ns=4;i=15013"
            var nodeId = NodeId.Parse(argIn);
            var (node, parsedValue) = await client.ParseAsync(nodeId);
            outputValues.Add(def.Outputs.First().Id, parsedValue);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading node: {ex.Message}");
        }

        return outputValues;
    }

    public override StepDef Def => def;
}

public class OpcUaSourcePropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("sourceUri")]
    public string SourceUri { get; set; }
    [JsonPropertyName("nodeIds")]
    public string[] NodeIds { get; set; }
}