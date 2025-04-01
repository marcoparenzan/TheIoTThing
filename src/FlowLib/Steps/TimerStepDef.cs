using FlowLib.Abstractions;

namespace FlowLib.Steps;

public class TimerStepDef : StepDef<TimerPropertiesDef>
{
    public override TimerStepState CreateState()
    {
        return new TimerStepState(this)
        {
        };
    }
}

public class TimerStepState(TimerStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class TimerPropertiesDef : StepPropertiesDef
{
    public long IntervalMilliseconds { get; set; }
}