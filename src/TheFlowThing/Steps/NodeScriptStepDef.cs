using TheFlowThing.Abstractions;
using Jint;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class NodeScriptStepDef : StepDef<NodeScriptPropertiesDef>
{
    public override NodeScriptStepState CreateState()
    {
        return new NodeScriptStepState(this)
        {
        };
    }
}

public class NodeScriptStepState(NodeScriptStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        var engine = new Engine(options => options.TimeoutInterval(TimeSpan.FromSeconds(5)));

        // Connector ids (e.g. "in") aren't guaranteed to be valid, non-reserved JS identifiers,
        // so inputs/outputs are exposed as dictionaries rather than bare globals named after the id.
        var outputs = new Dictionary<string, object>();
        engine.SetValue("inputs", inputValues);
        engine.SetValue("outputs", outputs);

        engine.Execute(def.Properties.Code ?? "");

        var result = new Dictionary<string, object>();
        foreach (var output in def.Outputs)
        {
            result[output.Id] = outputs.TryGetValue(output.Id, out var value) ? value : null!;
        }
        return Task.FromResult(result);
    }

    public override StepDef Def => def;
}

public class NodeScriptPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("code")]
    public string Code { get; set; }
}
