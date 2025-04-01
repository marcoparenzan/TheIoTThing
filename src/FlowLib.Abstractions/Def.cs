using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlowLib.Abstractions;
public abstract class Def
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }
    [JsonPropertyName("script")]
    public string Script { get; set; }

    public abstract State CreateState();
}
