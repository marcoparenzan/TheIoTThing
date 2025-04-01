using FlowLib.Abstractions;
using System.Text.Json.Serialization;

namespace FlowLib;


public partial class FlowDef
{
    [JsonPropertyName("steps")]
    public StepDef[] Steps { get; set; }

    [JsonPropertyName("pipes")]
    public PipeDef[] Pipes { get; set; }

    [JsonPropertyName("scale")]
    public long Scale { get; set; }

    public FlowState CreateState()
    {
        return new FlowState
        {
            Steps = Steps.Select(s => s.CreateState()).ToArray(),
            Pipes = Pipes.Select(p => p.CreateState()).ToArray()
        };
    }
}

public class FlowState
{
    public StepState[] Steps { get; set; }
    public PipeState[] Pipes { get; set; }
}