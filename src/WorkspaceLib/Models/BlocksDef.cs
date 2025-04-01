using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkspaceLib.Models;

public partial class BlocksDef
{
    [JsonPropertyName("languageVersion")]
    public long LanguageVersion { get; set; }

    [JsonPropertyName("blocks")]
    public List<BlockElement> Blocks { get; set; }
}