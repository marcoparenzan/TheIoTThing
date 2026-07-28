using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class DataSourceStepDef : StepDef<DataSourcePropertiesDef>
{
    public override DataSourceStepState CreateState()
    {
        return new DataSourceStepState(this)
        {
        };
    }
}

public class DataSourceStepState(DataSourceStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        var result = new Dictionary<string, object>();
        foreach (var output in def.Outputs)
        {
            result[output.Id] = def.Properties.Source;
        }
        return Task.FromResult(result);
    }

    public override StepDef Def => def;
}

public class DataSourcePropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("source")]
    public string Source { get; set; }
}
