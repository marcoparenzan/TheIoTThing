using FlowLib.Steps;

namespace FlowLib.Serialization;

public  class DefaultStepDefConverter: StepDefConverter
{
    static DefaultStepDefConverter()
    {
        Instance = new DefaultStepDefConverter();
    }

    public DefaultStepDefConverter()
    {
        Add<TimerStepDef>("timer");
        Add<ProcessorStepDef>("processor");
    }
}
