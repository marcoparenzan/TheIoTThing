using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class NotificationStepDef : StepDef<NotificationPropertiesDef>
{
    public override NotificationStepState CreateState()
    {
        return new NotificationStepState(this)
        {
        };
    }
}

public class NotificationStepState(NotificationStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class NotificationPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
