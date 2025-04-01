using FlowLib.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowLib.Serialization;

public delegate PipeDef PipeDefFactory(string text, JsonSerializerOptions options);

public abstract class PipeDefConverter : JsonConverter<PipeDef>
{
    public static PipeDefConverter Instance { get; internal protected set; }
    public static PipeDefFactory DefaultFactory { get; internal protected set; }

    Dictionary<string, PipeDefFactory> factories = new();

    protected PipeDefFactory Factory(string key)
    {
        if (factories.TryGetValue(key, out var f))
        {
            return f;
        }
        return null;
    }

    public PipeDefConverter Add<TDef>(string key)
        where TDef : PipeDef
    {
        factories.Add(key, CreateInstance<TDef>);
        return this;
    }

    private PipeDef CreateInstance<TDef>(string text, JsonSerializerOptions options)
         where TDef : PipeDef
    {
        return JsonSerializer.Deserialize<TDef>(text, options);
    }

    public override PipeDef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        return selected(root.GetRawText(), options);
    }

    public override void Write(Utf8JsonWriter writer, PipeDef value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}