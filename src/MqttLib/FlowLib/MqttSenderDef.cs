using FlowLib.Abstractions;

namespace MqttLib.FlowLib;

public class MqttSenderStepDef : StepDef<MqttSenderPropertiesDef>
{
    public override MqttSenderStepState CreateState()
    {
        return new MqttSenderStepState(this)
        {
        };
    }
}

public class MqttSenderStepState(MqttSenderStepDef def) : StepState
{
    private MqttSender client;

    public override async Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        if (this.client is null)
        {
            this.client = new MqttSender();
            await client.ConnectAsync();
        }

        var outputValues = new Dictionary<string, object>();
        try
        {
            var argIn = inputValues.First().Value;
            await client.SendMessageAsync(def.Properties.Topic, argIn);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading node: {ex.Message}");
        }

        return outputValues;
    }

    public override StepDef Def => def;

}

public class MqttSenderPropertiesDef: StepPropertiesDef
{
    public string TargetUri { get; set; }
    public string Topic { get; set; }
}