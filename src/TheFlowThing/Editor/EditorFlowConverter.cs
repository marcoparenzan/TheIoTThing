using System.Text.Json;
using System.Text.Json.Nodes;

namespace TheFlowThing.Editor;

/// <summary>
/// Translates between the visual editor's document shape ({ name, version, blocks, connections })
/// and the FlowDef shape ({ steps, pipes, scale }) that Executive actually runs. The two are
/// different schemas: a block has no notion of named input/output connectors (the canvas only ever
/// draws a single "in" and a single "out" dot per block), so a fixed pair of connector ids is used.
/// </summary>
public static class EditorFlowConverter
{
    const int DefaultWidth = 160;
    const int DefaultHeight = 90;
    const string InputConnectorId = "in";
    const string OutputConnectorId = "out";

    static readonly JsonSerializerOptions writeOptions = new() { WriteIndented = true };

    public static string ToFlowDefJson(string editorJson, string? existingFlowDefJson = null)
    {
        var doc = JsonNode.Parse(editorJson)?.AsObject() ?? new JsonObject();
        var blocks = doc["blocks"]?.AsArray() ?? new JsonArray();
        var connections = doc["connections"]?.AsArray() ?? new JsonArray();

        var steps = new JsonArray();
        foreach (var blockNode in blocks)
        {
            if (blockNode is null) continue;
            var block = blockNode.AsObject();

            var icon = GetString(block, "icon");
            var blockType = GetString(block, "type"); // "input" | "process" | "output"
            var entry = icon is not null && EditorStepCatalog.ByIcon.TryGetValue(icon, out var e) ? e : null;

            var category = entry?.Category ?? blockType ?? "process";
            var stepType = entry?.StepType ?? category switch
            {
                "input" => "data-source",
                "output" => "data-output",
                _ => "transform"
            };

            var inputs = new JsonArray();
            if (category is "process" or "output")
                inputs.Add(new JsonObject { ["id"] = InputConnectorId, ["name"] = InputConnectorId });

            var outputs = new JsonArray();
            if (category is "process" or "input")
                outputs.Add(new JsonObject { ["id"] = OutputConnectorId, ["name"] = OutputConnectorId });

            steps.Add(new JsonObject
            {
                ["id"] = GetString(block, "id"),
                ["type"] = stepType,
                ["name"] = GetString(block, "name") ?? entry?.Name ?? stepType,
                ["x"] = GetInt(block, "x"),
                ["y"] = GetInt(block, "y"),
                ["width"] = DefaultWidth,
                ["height"] = DefaultHeight,
                ["color"] = GetString(block, "color") ?? entry?.Color,
                ["inputs"] = inputs,
                ["outputs"] = outputs,
                ["properties"] = block["config"]?.DeepClone() ?? new JsonObject()
            });
        }

        var pipes = new JsonArray();
        foreach (var connNode in connections)
        {
            if (connNode is null) continue;
            var conn = connNode.AsObject();

            pipes.Add(new JsonObject
            {
                ["id"] = GetString(conn, "id"),
                ["sourceStep"] = GetString(conn, "from"),
                ["sourceOutput"] = OutputConnectorId,
                ["targetStep"] = GetString(conn, "to"),
                ["targetInput"] = InputConnectorId,
                ["properties"] = new JsonObject()
            });
        }

        // The editor doesn't know about scale/autostart (it only edits blocks/connections), so those
        // are carried over from whatever was on disk before this save instead of being reset/dropped.
        var existing = existingFlowDefJson is not null ? JsonNode.Parse(existingFlowDefJson)?.AsObject() : null;

        var flowDef = new JsonObject
        {
            ["steps"] = steps,
            ["pipes"] = pipes,
            ["scale"] = existing?["scale"]?.DeepClone() ?? 1,
            ["autostart"] = existing?["autostart"]?.DeepClone() ?? false
        };

        return flowDef.ToJsonString(writeOptions);
    }

    public static string ToEditorJson(string flowDefJson)
    {
        var doc = JsonNode.Parse(flowDefJson)?.AsObject() ?? new JsonObject();
        var steps = doc["steps"]?.AsArray() ?? new JsonArray();
        var pipes = doc["pipes"]?.AsArray() ?? new JsonArray();

        var blocks = new JsonArray();
        foreach (var stepNode in steps)
        {
            if (stepNode is null) continue;
            var step = stepNode.AsObject();

            var stepType = GetString(step, "type");
            var entry = stepType is not null && EditorStepCatalog.ByStepType.TryGetValue(stepType, out var e) ? e : null;

            blocks.Add(new JsonObject
            {
                ["id"] = GetString(step, "id"),
                ["type"] = entry?.Category ?? "process",
                ["name"] = GetString(step, "name") ?? entry?.Name ?? stepType,
                ["icon"] = entry?.Icon ?? "❔",
                ["x"] = GetInt(step, "x"),
                ["y"] = GetInt(step, "y"),
                ["config"] = step["properties"]?.DeepClone() ?? new JsonObject(),
                ["color"] = GetString(step, "color") ?? entry?.Color ?? "#94a3b8"
            });
        }

        var connections = new JsonArray();
        foreach (var pipeNode in pipes)
        {
            if (pipeNode is null) continue;
            var pipe = pipeNode.AsObject();

            connections.Add(new JsonObject
            {
                ["id"] = GetString(pipe, "id"),
                ["from"] = GetString(pipe, "sourceStep"),
                ["to"] = GetString(pipe, "targetStep")
            });
        }

        var editorDoc = new JsonObject
        {
            ["name"] = GetString(doc, "name") ?? "Flow",
            ["version"] = "1.0",
            ["blocks"] = blocks,
            ["connections"] = connections
        };

        return editorDoc.ToJsonString(writeOptions);
    }

    static string? GetString(JsonObject obj, string property)
    {
        var node = obj[property];
        return node is null ? null : node.GetValue<string>();
    }

    static int GetInt(JsonObject obj, string property)
    {
        var node = obj[property];
        if (node is null) return 0;
        return (int)Math.Round(node.GetValue<double>());
    }
}
