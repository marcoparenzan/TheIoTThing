using TheFlowThing.Pipes;

namespace TheFlowThing.Serialization;

public  class DefaultPipeDefConverter: PipeDefConverter
{
    static DefaultPipeDefConverter()
    {
        Instance = new DefaultPipeDefConverter();
    }

    public DefaultPipeDefConverter()
    {
        Add<DefaultPipeDef>("default");
        DefaultFactory = Factory("default");
    }
}
