using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class ApiInputStepDef : StepDef<ApiInputPropertiesDef>
{
    public override ApiInputStepState CreateState()
    {
        return new ApiInputStepState(this)
        {
        };
    }
}

public class ApiInputStepState(ApiInputStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class ApiInputPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; }
}
