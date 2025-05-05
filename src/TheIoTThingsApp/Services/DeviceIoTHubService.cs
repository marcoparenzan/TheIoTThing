using MqttLib.Models;
using MQTTnet;
using MQTTnet.Client;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheIoTThingsApp.Services;

public class DeviceIoTHubService(IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hashids = new HashidsNet.Hashids();

        var hostName = config["HostName"];

        var customerName = config["CustomerName"];
        var deviceName = config["DeviceName"];
        var cn = $"{deviceName}";

        var certificateUrl = config["CertificateUrl"];
        var certificatePassword = config["CertificatePassword"];

        var clientId = deviceName;
        var resourceId = $"{hostName}/devices/{deviceName}";
        var username = $"{hostName}/{deviceName}/api-version=2021-04-12";

        var mqttClient = new MqttFactory().CreateMqttClient();

        var httpClient = new HttpClient();

        try
        {
            var certificateBytes = await httpClient.GetByteArrayAsync(certificateUrl);

            var certificate = X509CertificateLoader.LoadPkcs12(certificateBytes, certificatePassword);
            //var intermediate = X509CertificateLoader.LoadCertificateFromFile(@"D:\Downloads\27-IntermediateCACert.pem");
            var root = X509CertificateLoader.LoadCertificateFromFile(@"D:\Downloads\31-RootCACert.pem");

            var opts = new MqttClientOptionsBuilder()
                .WithTcpServer(hostName, 8883)
                .WithClientId(clientId)
                .WithCredentials(username, "")  //use client authentication name in the username
                .WithTlsOptions(configure =>
                    configure
                        .UseTls()
                        .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12)
                        .WithClientCertificates([certificate])
                )
                .WithCleanSession()
                .Build();

            var connAck = await mqttClient!.ConnectAsync(opts);
            Console.WriteLine($"Client Connected: {mqttClient.IsConnected} with CONNACK: {connAck.ResultCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
            return;
        }

        //mqttClient.ApplicationMessageReceivedAsync += async m => await Console.Out.WriteAsync($"Received message on topic: '{m.ApplicationMessage.Topic}' with content: '{m.ApplicationMessage.ConvertPayloadToString()}'\n\n");

        //var suback = await mqttClient.SubscribeAsync("test1/topic1");
        //suback.Items.ToList().ForEach(s => Console.WriteLine($"subscribed to '{s.TopicFilter.Topic}' with '{s.ResultCode}'"));

        while (true)
        {
            var now = DateTimeOffset.Now;

            var message = new AIOMessagePayload
            {
                Timestamp = now,
                MessageType = "ua-deltaframe",
                Payload = new Dictionary<string, AIOMessagePayloadItem>
                {
                    ["State"] = new() { SourceTimestamp = now, Value = "running" },
                    ["AlarmSubsystem1"] = new() { SourceTimestamp = now, Value = true },
                    ["AlarmSubsystem2"] = new() { SourceTimestamp = now, Value = false },
                    ["AbsorbedEnergy"] = new() { SourceTimestamp = now, Value = Random.Shared.NextDouble() * 100 }
                },
                DataSetWriterName = cn,
                SequenceNumber = Random.Shared.Next(0, 100000)
            };

            var messageJson = JsonSerializer.Serialize(message);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("devicetelemetry/topic1")
                //.WithTopic("azure-iot-operations/data/opc.tcp/opc.tcp-1/solarcamp-north")
                .WithPayload(JsonSerializer.Serialize(message))
                .Build();

            var puback = await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            Console.WriteLine(puback.ReasonString);
            await Task.Delay(5000);
        }

        await mqttClient.DisconnectAsync();
    }
}