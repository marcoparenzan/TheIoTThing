using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkspaceLib.Models;

public partial class BlockValue
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, JsonElement> Fields { get; set; }

    [JsonPropertyName("inputs")]
    public Dictionary<string, Dictionary<string, BlockValue>> Inputs { get; set; }

    [JsonPropertyName("next")]
    public Ref Next { get; set; }
}
