using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class TransformStepDef : StepDef<TransformPropertiesDef>
{
    public override TransformStepState CreateState()
    {
        return new TransformStepState(this)
        {
        };
    }
}

public class TransformStepState(TransformStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class TransformPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("mapping")]
    public Dictionary<string, object> Mapping { get; set; }
}
