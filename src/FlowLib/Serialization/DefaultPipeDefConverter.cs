using FlowLib.Pipes;

namespace FlowLib.Serialization;

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
