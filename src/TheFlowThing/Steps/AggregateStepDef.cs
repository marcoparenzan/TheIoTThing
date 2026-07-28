using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class AggregateStepDef : StepDef<AggregatePropertiesDef>
{
    public override AggregateStepState CreateState()
    {
        return new AggregateStepState(this)
        {
        };
    }
}

public class AggregateStepState(AggregateStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class AggregatePropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("function")]
    public string Function { get; set; }

    [JsonPropertyName("field")]
    public string Field { get; set; }
}
