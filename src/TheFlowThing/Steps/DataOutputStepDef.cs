using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class DataOutputStepDef : StepDef<DataOutputPropertiesDef>
{
    public override DataOutputStepState CreateState()
    {
        return new DataOutputStepState(this)
        {
        };
    }
}

public class DataOutputStepState(DataOutputStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class DataOutputPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("destination")]
    public string Destination { get; set; }
}
