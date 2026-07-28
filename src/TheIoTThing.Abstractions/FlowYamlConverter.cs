using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace TheIoTThing.Abstractions;

public class FlowYamlException(string message, Exception inner) : Exception(message, inner);

/// <summary>
/// Converts between JSON text and an equivalent YAML text for hand editing — fully generic (round-trips
/// any JSON, not just a FlowDef shape), which is why the orchestrator's common Config editor can use it
/// for any service type. JSON strings are always emitted double-quoted and everything else (numbers,
/// booleans, null) is emitted as a plain scalar, so the reverse conversion can tell "" (empty string)
/// apart from a bare null without ambiguity.
/// </summary>
public static class FlowYamlConverter
{
    public static string ToYaml(string json)
    {
        var node = JsonNode.Parse(json) ?? new JsonObject();
        var yamlNode = JsonToYaml(node);
        var document = new YamlDocument(yamlNode);
        var stream = new YamlStream(document);

        using var writer = new StringWriter();
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    public static string ToJson(string yaml)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlException ex)
        {
            throw new FlowYamlException($"Invalid YAML: {ex.Message}", ex);
        }

        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is null)
            return "{}";

        var jsonNode = YamlToJson(stream.Documents[0].RootNode);
        return jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    static YamlNode JsonToYaml(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return new YamlScalarNode("null") { Style = ScalarStyle.Plain };

            case JsonObject obj:
                var mapping = new YamlMappingNode();
                foreach (var (key, value) in obj)
                {
                    mapping.Add(new YamlScalarNode(key), JsonToYaml(value));
                }
                return mapping;

            case JsonArray arr:
                var sequence = new YamlSequenceNode();
                foreach (var item in arr)
                {
                    sequence.Add(JsonToYaml(item));
                }
                return sequence;

            case JsonValue val:
                var element = val.GetValue<JsonElement>();
                return element.ValueKind switch
                {
                    JsonValueKind.String => new YamlScalarNode(element.GetString() ?? "") { Style = ScalarStyle.DoubleQuoted },
                    JsonValueKind.True => new YamlScalarNode("true") { Style = ScalarStyle.Plain },
                    JsonValueKind.False => new YamlScalarNode("false") { Style = ScalarStyle.Plain },
                    JsonValueKind.Number => new YamlScalarNode(element.GetRawText()) { Style = ScalarStyle.Plain },
                    _ => new YamlScalarNode("null") { Style = ScalarStyle.Plain }
                };

            default:
                return new YamlScalarNode("null") { Style = ScalarStyle.Plain };
        }
    }

    static JsonNode? YamlToJson(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                var obj = new JsonObject();
                foreach (var (key, value) in mapping)
                {
                    obj[((YamlScalarNode)key).Value ?? ""] = YamlToJson(value);
                }
                return obj;

            case YamlSequenceNode sequence:
                var arr = new JsonArray();
                foreach (var item in sequence)
                {
                    arr.Add(YamlToJson(item));
                }
                return arr;

            case YamlScalarNode scalar:
                return ScalarToJson(scalar);

            default:
                return null;
        }
    }

    static JsonNode? ScalarToJson(YamlScalarNode scalar)
    {
        var text = scalar.Value ?? "";

        // Quoted scalars are always literal strings, whatever they look like.
        if (scalar.Style is ScalarStyle.DoubleQuoted or ScalarStyle.SingleQuoted)
        {
            return JsonValue.Create(text);
        }

        if (text is "null" or "~" or "")
            return null;
        if (text == "true")
            return JsonValue.Create(true);
        if (text == "false")
            return JsonValue.Create(false);
        if (long.TryParse(text, out var longValue))
            return JsonValue.Create(longValue);
        if (double.TryParse(text, out var doubleValue))
            return JsonValue.Create(doubleValue);

        return JsonValue.Create(text);
    }
}
