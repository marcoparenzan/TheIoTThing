using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class ManualInputStepDef : StepDef<ManualInputPropertiesDef>
{
    public override ManualInputStepState CreateState()
    {
        return new ManualInputStepState(this)
        {
        };
    }
}

public class ManualInputStepState(ManualInputStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class ManualInputPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("fields")]
    public string[] Fields { get; set; }
}
