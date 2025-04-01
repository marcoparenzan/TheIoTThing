using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkspaceLib.Models;

public partial class VarRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
