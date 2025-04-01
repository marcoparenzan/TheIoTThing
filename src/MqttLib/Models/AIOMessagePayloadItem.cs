namespace MqttLib.Models;

public class AIOMessagePayloadItem
{
    public DateTimeOffset SourceTimestamp { get; set; }
    public object Value { get; set; }
}