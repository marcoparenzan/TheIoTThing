using MQTTnet;
using MQTTnet.Client;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace MqttLib;

public class MqttSender
{
    private MqttClientOptions clientOptions;
    private IMqttClient mqttClient;

    public async Task ConnectAsync()
    {
        if (mqttClient is null)
        {
            var hostname = "localhost";

            var hashids = new HashidsNet.Hashids();

            this.clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(hostname, 1883)
                .WithClientId("dadsad")
                .WithCredentials("username", "password")  //use client authentication name in the username
                .Build();

            this.mqttClient = new MqttFactory().CreateMqttClient();
        }

        if (!mqttClient.IsConnected)
        {
            var connAck = await mqttClient!.ConnectAsync(clientOptions);

            var suback = await mqttClient.SubscribeAsync("timers/5s");
            //suback.Items.ToList().ForEach(s => Console.WriteLine($"subscribed to '{s.TopicFilter.Topic}' with '{s.ResultCode}'"));
        }
    }

    public async Task DisconnectAsync()
    {
        if (mqttClient is not null)
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }
        }
    }

    void GenerateCertificates(string cn, string password)
    {
        using RSA rsa = RSA.Create(2048);

        // Define certificate details
        var request = new CertificateRequest(
            $"CN={cn}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        Directory.CreateDirectory("certs");
        var path = $"certs\\{cn}.pfx";

        // Create a self-signed certificate that is valid for one year
        var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        // Export the certificate and private key to a PFX file
        var bytes = cert.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(path, bytes);

        // Export the certificate to PEM format
        var certPemBuilder = new StringBuilder();
        certPemBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
        var cert1 = cert.Export(X509ContentType.Cert);
        certPemBuilder.AppendLine(Convert.ToBase64String(cert1, Base64FormattingOptions.InsertLineBreaks));
        certPemBuilder.AppendLine("-----END CERTIFICATE-----");
        File.WriteAllText(path.Replace(".pfx", ".pem"), certPemBuilder.ToString());

        var keyPemBuilder = new StringBuilder();
        keyPemBuilder.AppendLine("-----BEGIN PRIVATE KEY-----");
        keyPemBuilder.AppendLine(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks));
        keyPemBuilder.AppendLine("-----END PRIVATE KEY-----");
        File.WriteAllText(path.Replace(".pfx", ".key"), keyPemBuilder.ToString());
    }

    public async Task SendMessageAsync<TMessage>(string topic, TMessage message)
    {
        var now = DateTimeOffset.Now;

        //var message = new AIOMessagePayload
        //{
        //    Timestamp = now,
        //    MessageType = "ua-deltaframe",
        //    Payload = new Dictionary<string, AIOMessagePayloadItem>
        //    {
        //        ["State"] = new() { SourceTimestamp = now, Value = "running" },
        //        ["AlarmSubsystem1"] = new() { SourceTimestamp = now, Value = true },
        //        ["AlarmSubsystem2"] = new() { SourceTimestamp = now, Value = false },
        //        ["AbsorbedEnergy"] = new() { SourceTimestamp = now, Value = Random.Shared.NextDouble() * 100 }
        //    },
        //    DataSetWriterName = "", // cn,
        //    SequenceNumber = Random.Shared.Next(0, 100000)
        //};

        var messageJson = JsonSerializer.Serialize(message);

        var payload = JsonSerializer.Serialize(message);

        var applicationMessage = new MqttApplicationMessageBuilder()
            //.WithTopic("devicetelemetry/device24")
            .WithTopic(topic)
            //.WithTopic("azure-iot-operations/data/opc.tcp/opc.tcp-1/solarcamp-north")
            .WithPayload(payload)
            .Build();

        try
        {
            var puback = await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            Console.WriteLine(payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
