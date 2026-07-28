using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class ConditionStepDef : StepDef<ConditionPropertiesDef>
{
    public override ConditionStepState CreateState()
    {
        return new ConditionStepState(this)
        {
        };
    }
}

public class ConditionStepState(ConditionStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class ConditionPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("if")]
    public string If { get; set; }

    [JsonPropertyName("then")]
    public string Then { get; set; }

    [JsonPropertyName("else")]
    public string Else { get; set; }
}
