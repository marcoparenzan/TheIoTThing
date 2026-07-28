using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class FileWriterStepDef : StepDef<FileWriterPropertiesDef>
{
    public override FileWriterStepState CreateState()
    {
        return new FileWriterStepState(this)
        {
        };
    }
}

public class FileWriterStepState(FileWriterStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class FileWriterPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }
}
