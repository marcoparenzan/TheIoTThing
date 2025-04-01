using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLib.Abstractions;

public abstract class StepConnectorDef
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class InputConnectorDef : StepConnectorDef
{
}

public class OutputConnectorDef : StepConnectorDef
{
}
