using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkspaceLib.Models;

public class Field
{
    [JsonPropertyName("NUM")]
    public decimal? NUM { get; set; }
    [JsonPropertyName("OP")]
    public string? OP { get; set; }
    [JsonPropertyName("VAR")]
    public VarRef? VAR { get; set; }
}
