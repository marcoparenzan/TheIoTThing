using FlowLib;
using FlowLib.Abstractions;
using FlowLib.Serialization;
using MqttLib.FlowLib;
using OpcUaLib.FlowLib;
using System.Text.Json;

var flowDefJsonOptions = new JsonSerializerOptions
{
    Converters = { 
        new DefaultStepDefConverter()
            .Add<MqttSenderStepDef>("mqttsender")
            .Add<OpcUaSourceStepDef>("opcuasource"), 
        new DefaultPipeDefConverter() 
    },
    PropertyNameCaseInsensitive = true
};

var flowDefJson = File.ReadAllText("flowdef1.json");
var flowDef = JsonSerializer.Deserialize<FlowDef>(flowDefJson, flowDefJsonOptions);

var flowState = flowDef.CreateState();

var s_i = 0;
var p_i = 0;
var qot = new QuantumOfTime
{
    Timestamp = DateTimeOffset.Now
};

var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
while (true)
{
    //await periodicTimer.WaitForNextTickAsync();

    var stay = true;
    while(stay)
    {
        try
        {
            while (true)
            {
                if (s_i >= flowState.Steps.Length) break;
                var step = flowState.Steps[s_i];
                s_i++;

                var inputValues = new Dictionary<string, object>();
                foreach (var input in step.Def.Inputs)
                {
                    var pipe = flowState.Pipes.SingleOrDefault(xx => xx.Def.TargetStep == step.Def.Id && xx.Def.TargetInput == input.Id);
                    inputValues.Add(input.Id, pipe.Value);
                }

                var outputValues = await step.AdvanceAsync(qot, inputValues); // IT CAN FAIL

                foreach (var output in step.Def.Outputs)
                {
                    var pipe = flowState.Pipes.SingleOrDefault(xx => xx.Def.SourceStep == step.Def.Id && xx.Def.SourceOutput == output.Id);
                    pipe.NextValue = outputValues[output.Id];
                    pipe.Update();
                }
            }

            // advance
            s_i = 0;
            p_i = 0;
            qot = new QuantumOfTime
            {
                Timestamp = DateTimeOffset.Now
            };

            foreach(var pipe in flowState.Pipes)
            {
                pipe.Update();
            }
        }
        catch (Exception ex)
        {
            // log anything
        }
        stay = false;
    }

}
