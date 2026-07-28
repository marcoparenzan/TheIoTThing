using Microsoft.Extensions.Logging;
using System.Text.Json;
using TheFlowThing.Abstractions;
using TheIoTThing.Abstractions;

namespace TheFlowThing;

/// <summary>
/// The "flow" service type: advances a FlowDef's steps/pipes once a tick. The Stopped/Running/Paused
/// state machine and the tick loop itself live in TickingService (TheIoTThing.Abstractions) — this
/// class only owns loading a FlowDef into a FlowState and walking it once per tick.
/// </summary>
public class Executive(ILogger<Executive>? logger = null) : TickingService(logger)
{
    FlowState? flowState;
    OrchestrationContext? context;
    int s_i;
    QuantumOfTime qot = new() { Timestamp = DateTimeOffset.Now };

    protected override TimeSpan TickInterval => TimeSpan.FromSeconds(1);

    protected override bool IsLoaded => flowState is not null;

    public override async Task LoadAsync(string path, OrchestrationContext context)
    {
        this.context = context;
        await LoadFromFileAsync(path);
    }

    /// <summary>Loads a flow file directly, without an OrchestrationContext — used by the console host
    /// and by anything that just wants to run a flow standalone.</summary>
    public async Task LoadFromFileAsync(string path)
    {
        var flowDefJson = await File.ReadAllTextAsync(path);
        var flowDef = JsonSerializer.Deserialize<FlowDef>(flowDefJson, FlowDef.CreateJsonOptions());

        flowState = flowDef!.CreateState();
        SetAutostart(flowDef.Autostart);
    }

    public override object? GetSnapshot() => flowState;

    protected override async Task OnTickAsync()
    {
        var now = DateTimeOffset.Now;
        var state = flowState!;

        try
        {
            while (true)
            {
                if (s_i >= state.Steps.Length) break;
                var step = state.Steps[s_i];
                s_i++;

                var inputValues = new Dictionary<string, object>();
                foreach (var input in step.Def.Inputs)
                {
                    var pipe = state.Pipes.SingleOrDefault(xx => xx.Def.TargetStep == step.Def.Id && xx.Def.TargetInput == input.Id);
                    inputValues.Add(input.Id, pipe.Value);
                }

                var outputValues = await step.AdvanceAsync(qot, inputValues);

                foreach (var output in step.Def.Outputs)
                {
                    var pipe = state.Pipes.SingleOrDefault(xx => xx.Def.SourceStep == step.Def.Id && xx.Def.SourceOutput == output.Id);
                    pipe.NextValue = outputValues[output.Id];
                    pipe.Update();
                }
            }

            s_i = 0;
            qot = new QuantumOfTime { Timestamp = now };

            foreach (var pipe in state.Pipes)
            {
                pipe.Update();
            }
        }
        catch
        {
            // Restart the step walk from the top next tick rather than resuming mid-flow after a failure.
            s_i = 0;
            throw;
        }
    }
}
