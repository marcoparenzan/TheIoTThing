using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkspaceLib.Models;

public partial class Ref : Dictionary<string, BlockValue>
{
    //[JsonPropertyName("block")]
    //public BlockValue Block { get; set; }
    //[JsonPropertyName("shadow")]
    //public BlockValue Shadow { get; set; }
}
