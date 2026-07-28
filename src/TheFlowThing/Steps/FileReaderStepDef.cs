using TheFlowThing.Abstractions;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class FileReaderStepDef : StepDef<FileReaderPropertiesDef>
{
    public override FileReaderStepState CreateState()
    {
        return new FileReaderStepState(this)
        {
        };
    }
}

public class FileReaderStepState(FileReaderStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override StepDef Def => def;
}

public class FileReaderPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }
}
