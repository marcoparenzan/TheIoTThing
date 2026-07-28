using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class SensorStepDef : StepDef<SensorPropertiesDef>
{
    public override SensorStepState CreateState()
    {
        return new SensorStepState(this)
        {
        };
    }
}

public class SensorStepState(SensorStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class SensorPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("sensorId")]
    public string SensorId { get; set; }

    [JsonPropertyName("interval")]
    public long Interval { get; set; }
}
