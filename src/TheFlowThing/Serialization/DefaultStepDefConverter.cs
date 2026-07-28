using TheFlowThing.Steps;

namespace TheFlowThing.Serialization;

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

        // Ingresso
        Add<DataSourceStepDef>("data-source");
        Add<ApiInputStepDef>("api-input");
        Add<FileReaderStepDef>("file-reader");
        Add<SensorStepDef>("sensor");
        Add<ManualInputStepDef>("manual-input");

        // Elaborazione
        Add<TransformStepDef>("transform");
        Add<FilterStepDef>("filter");
        Add<AggregateStepDef>("aggregate");
        Add<ConditionStepDef>("condition");
        Add<NodeScriptStepDef>("node-script");
        Add<PySharpStepDef>("pysharp");

        // Uscita
        Add<DataOutputStepDef>("data-output");
        Add<ApiOutputStepDef>("api-output");
        Add<FileWriterStepDef>("file-writer");
        Add<DisplayStepDef>("display");
        Add<NotificationStepDef>("notification");
    }
}
