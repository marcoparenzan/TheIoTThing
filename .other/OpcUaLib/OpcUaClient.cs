using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Schema.Binary;
using System.Xml.Serialization;

namespace OpcUaLib;

public  class OpcUaClient
{
    private ApplicationConfiguration config;
    private ApplicationInstance application;
    private Session session;

    List<OpcUaSubscription> subscriptions = new();

    public async Task ConnectAsync()
    {
        this.config = new ApplicationConfiguration()
        {
            ApplicationName = "OpcUaClient",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "X509Store",
                    StorePath = "CurrentUser\\My",
                    SubjectName = "OpcUaClient"
                },
                AutoAcceptUntrustedCertificates = true
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
        };

        // Validate the configuration
        await config.Validate(ApplicationType.Client);

        // Instantiate the OPC UA application
        this.application = new ApplicationInstance
        {
            ApplicationConfiguration = config
        };

        _ = await application.CheckApplicationInstanceCertificate(false, 0);

        // Create a session with the OPC UA server
        var endpointURL = "opc.tcp://localhost:50000"; // Change to your server's endpoint
        var endpoint = CoreClientUtils.SelectEndpoint(endpointURL, false);
        this.session = await Session.Create(
            config,
            new ConfiguredEndpoint(null, endpoint, null), 
            false, 
            "OpcUaClient", 
            60000,
            new UserIdentity("user", "password"), 
            null
        );

        Console.WriteLine("Connected to OPC UA server!");
    }

    public async Task CloseAsync()
    {
        foreach (var sub in subscriptions)
        {
            sub.Delete();
        }
        subscriptions.Clear();
        session.Close();
        session.Dispose();
        session = null;
    }

    public async Task<OpcUaSubscription> SubscribeAsync(Action<NodeId, DataValue> handler, params string[] nodeIds)
    {
        var newSub = new OpcUaSubscription(session, handler, nodeIds);

        subscriptions.Add(newSub);

        return newSub;
    }

    public async Task<Node> ReadNodeAsync(NodeId nodeId)
    {
        return await this.session.ReadNodeAsync(nodeId);
    }

    public async Task<DataValue> ReadValueAsync(NodeId nodeId)
    {
        return await this.session.ReadValueAsync(nodeId);
    }

    public async Task<(Node node, object value)> ParseAsync(NodeId nodeId)
    {
        var aaa = await session.ReadValueAsync(nodeId);
        return await ParseAsync(nodeId, aaa);
    }

    public async Task<(Node node, object value)> ParseAsync(NodeId nodeId, DataValue aaa)
    {
        var node = await session.ReadNodeAsync(nodeId);
        if (node is VariableNode vnode)
        {
            var instance = await ParseComplexTypeAsync(vnode, aaa);
            return (node, instance);
        }
        else
        {
            return (node, aaa.Value);
        }
    }

    async Task<Dictionary<string, object>> ParseComplexTypeAsync(VariableNode vnode, DataValue aaa)
    {
        var byteArrayBody = (byte[])((ExtensionObject)aaa.Value).Body;
        var decoder = new BinaryDecoder((byte[])byteArrayBody, null);

        var dataType = await session.ReadNodeAsync(vnode.DataType);

        var jsonEncodingId = new NodeId(vnode.JsonEncodingId.Identifier, vnode.NodeId.NamespaceIndex);
        var jsonEncoding = await session.ReadNodeAsync(jsonEncodingId);
        var jsonEncodedValue = await session.ReadValueAsync(jsonEncodingId);
        var jsonEncodedBytes = (byte[])jsonEncodedValue.Value;
        var jsonEncodedStream = new MemoryStream(jsonEncodedBytes);

        var typeDictionarySerializer = new XmlSerializer(typeof(Opc.Ua.Schema.Binary.TypeDictionary));
        var typeDictionaryDeserialized = (Opc.Ua.Schema.Binary.TypeDictionary)typeDictionarySerializer.Deserialize(jsonEncodedStream);

        var instance = new Dictionary<string, object>();
        var rootType = (StructuredType)typeDictionaryDeserialized.Items.SingleOrDefault(xx => xx.Name == dataType.DisplayName);
        StructuredType(typeDictionaryDeserialized, decoder, instance, rootType);

        return instance;
    }

    void StructuredType(TypeDictionary typeDictionary, BinaryDecoder reader, Dictionary<string, object> item, StructuredType? root)
    {
        foreach (var field in root.Field)
        {
            if (field.TypeName.Namespace == "http://opcfoundation.org/BinarySchema/")
            {
                switch (field.TypeName.Name)
                {
                    case "Int32":
                        var int32Value = reader.ReadInt32(field.Name);
                        item.Add(field.Name, int32Value);
                        break;
                    default:
                        throw new NotSupportedException($"{field.TypeName.Name} not found - http://opcfoundation.org/BinarySchema/");
                }
            }
            else
            {
                var child = typeDictionary.Items.SingleOrDefault(xx => xx.Name == field.TypeName.Name);
                if (child is StructuredType subType)
                {
                    var subItem = new Dictionary<string, object>();
                    StructuredType(typeDictionary, reader, subItem, subType);
                    item.Add(field.Name, subItem);
                }
                else if (child is EnumeratedType et)
                {
                    var value = EnumeratedType(typeDictionary, reader, et);
                    item.Add(field.Name, value);
                }
            }
        }
    }

    string EnumeratedType(TypeDictionary typeDictionary, BinaryDecoder reader, EnumeratedType? root)
    {
        var vvv = reader.ReadInt32(root.Name);
        var v = root.EnumeratedValue.SingleOrDefault(xx => xx.Value == vvv);
        return v.Name;
    }
}
