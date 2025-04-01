namespace FlowLib.Abstractions;

public abstract class StepDef: Def
{

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public InputConnectorDef[] Inputs { get; set; } = [];
    public OutputConnectorDef[] Outputs { get; set; } = [];

    public override StepState CreateState()
    {
        throw new NotImplementedException();
    }
}

public abstract class StepState(): State
{
    public override StepDef Def => throw new NotImplementedException();

}

public abstract class StepDef<TPropertiesDef>: StepDef 
    where TPropertiesDef : StepPropertiesDef
{
    public TPropertiesDef Properties { get; set; }
}

public abstract class StepPropertiesDef
{
}