namespace MqttLib.Models;

public class TelemetryMessagePayload
{
    public DateTimeOffset Timestamp { get; set; }
    public string MessageType { get; set; }
    public Dictionary<string, TelemetryMessagePayloadItem> Payload { get; set; }
    public string SourceName { get; set; }
    public int SequenceNumber { get; set; }
}

public class TelemetryMessagePayloadItem
{
    public DateTimeOffset? Timestamp { get; set; }
    public object Value { get; set; }
}