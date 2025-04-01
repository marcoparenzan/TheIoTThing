using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkspaceLib.Models;

public partial class BlockElement : BlockValue
{
    [JsonPropertyName("x")]
    public long X { get; set; }

    [JsonPropertyName("y")]
    public long Y { get; set; }
}
