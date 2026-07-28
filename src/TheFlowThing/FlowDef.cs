using TheFlowThing.Abstractions;
using TheFlowThing.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheFlowThing;


public partial class FlowDef
{
    // The orchestrator-level discriminator (ServiceStore/ServiceTypeRegistry) that says "this whole
    // file is a flow-type service" — unrelated to each step's own "type" one level down (e.g.
    // "data-source"), easy to conflate but a different concept at a different level.
    [JsonPropertyName("type")]
    public string Type { get; set; } = "flow";

    [JsonPropertyName("steps")]
    public StepDef[] Steps { get; set; }

    [JsonPropertyName("pipes")]
    public PipeDef[] Pipes { get; set; }

    [JsonPropertyName("scale")]
    public long Scale { get; set; }

    [JsonPropertyName("autostart")]
    public bool Autostart { get; set; }

    public FlowState CreateState()
    {
        return new FlowState
        {
            Steps = Steps.Select(s => s.CreateState()).ToArray(),
            Pipes = Pipes.Select(p => p.CreateState()).ToArray()
        };
    }

    public static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            Converters = {
                new DefaultStepDefConverter(),
                new DefaultPipeDefConverter()
            },
            PropertyNameCaseInsensitive = true
        };
    }
}

public class FlowState
{
    public StepState[] Steps { get; set; }
    public PipeState[] Pipes { get; set; }
}