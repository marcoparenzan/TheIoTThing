using FlowLib.Abstractions;

namespace FlowLib.Steps;

public class ProcessorStepDef : StepDef<ProcessorPropertiesDef>
{
    public override ProcessorStepState CreateState()
    {
        return new ProcessorStepState(this)
        {
        };
    }
}

public class ProcessorStepState(ProcessorStepDef def) : StepState
{
    public override async Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        var outputValues = new Dictionary<string, object>();
        try
        {
            var value = inputValues.First().Value;
            outputValues.Add(def.Outputs.First().Id, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading node: {ex.Message}");
        }

        return outputValues;
    }

    public override StepDef Def => def;
}

public class ProcessorPropertiesDef : StepPropertiesDef
{
}