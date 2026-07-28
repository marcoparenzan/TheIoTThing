using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class FilterStepDef : StepDef<FilterPropertiesDef>
{
    public override FilterStepState CreateState()
    {
        return new FilterStepState(this)
        {
        };
    }
}

public class FilterStepState(FilterStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class FilterPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("condition")]
    public string Condition { get; set; }
}
