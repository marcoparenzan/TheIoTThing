using FlowLib.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowLib.Serialization;

public delegate StepDef StepDefFactory(string text, JsonSerializerOptions options);

public class StepDefConverter : JsonConverter<StepDef>
{
    public static StepDefConverter Instance { get; internal protected set; }
    public static StepDefFactory DefaultFactory { get; internal protected set; }

    Dictionary<string, StepDefFactory> factories = new();

    protected StepDefFactory Factory(string key)
    {
        if (factories.TryGetValue(key, out var f))
        {
            return f;
        }
        return null;
    }

    public StepDefConverter Add<TDef>(string key)
        where TDef : StepDef
    {
        factories.Add(key, CreateInstance<TDef>);
        return this;
    }

    private StepDef CreateInstance<TDef>(string text, JsonSerializerOptions options)
         where TDef : StepDef
    {
        return JsonSerializer.Deserialize<TDef>(text, options);
    }

    public override StepDef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var selected = DefaultFactory;
        var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        if (root.TryGetProperty("type", out var el))
        {
            var type = el.GetString();
            if (factories.TryGetValue(type, out var f))
            {
                selected = f;
            }
        }
        if (selected is not null)
        {
            return selected(root.GetRawText(), options);
        }
        else
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, StepDef value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}