using NServiceBus;

public class ReceivingTestServer : DefaultTestServer
{
    const string StorageDirectory = "ReceiverTestingTransport";

    protected override AcceptanceTestingTransport GetTransportDefinition() => GetTransportDefinition(StorageDirectory);

    public static AcceptanceTestingTransport GetReceivingTransportDefinition() => GetTransportDefinition(StorageDirectory);
}
