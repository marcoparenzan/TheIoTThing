using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkspaceLib.Models;

public partial class WorkspaceDef
{
    [JsonPropertyName("blocks")]
    public BlocksDef Blocks { get; set; }

    [JsonPropertyName("variables")]
    public List<VariableDef> Variables { get; set; }
}