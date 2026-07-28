using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class DisplayStepDef : StepDef<DisplayPropertiesDef>
{
    public override DisplayStepState CreateState()
    {
        return new DisplayStepState(this)
        {
        };
    }
}

public class DisplayStepState(DisplayStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class DisplayPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("template")]
    public string Template { get; set; }
}
