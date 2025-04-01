using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLib.Abstractions;
public abstract class State
{
    public abstract Def Def { get; }
    public abstract Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues);
}

public struct QuantumOfTime
{
    public DateTimeOffset Timestamp { get; set; }
}