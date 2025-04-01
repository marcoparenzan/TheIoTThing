using FlowLib.Abstractions;

namespace FlowLib.Pipes;

public class DefaultPipeDef: PipeDef<DefaultPipeProperties>
{
    public override DefaultPipeState CreateState()
    {
        return new DefaultPipeState(this)
        {
        };
    }
}

public class DefaultPipeState(DefaultPipeDef def) : PipeState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        throw new NotImplementedException();
    }

    public override PipeDef Def => def;
}

public class DefaultPipeProperties: PipePropertiesDef
{
}