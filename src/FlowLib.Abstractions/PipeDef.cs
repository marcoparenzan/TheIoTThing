using System.Text.Json.Serialization;

namespace FlowLib.Abstractions;

public abstract partial class PipeDef: Def
{

    [JsonPropertyName("sourceStep")]
    public string SourceStep { get; set; }

    [JsonPropertyName("sourceOutput")]
    public string SourceOutput { get; set; }

    [JsonPropertyName("targetStep")]
    public string TargetStep { get; set; }

    [JsonPropertyName("targetInput")]
    public string TargetInput { get; set; }

    public override PipeState CreateState()
    {
        throw new NotImplementedException();
    }
}

public abstract class PipeState: State
{
    public override PipeDef Def => throw new NotImplementedException();

    public object Value { get; set; }

    public object NextValue { get; set; }

    public void Update()
    {
        Value = NextValue;
        NextValue = default;
    }
}


public abstract partial class PipeDef<TPropertiesDef>: PipeDef
    where TPropertiesDef : PipePropertiesDef
{
    public TPropertiesDef Properties { get; set; }
}

public abstract class PipePropertiesDef
{
}

