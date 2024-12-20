using NServiceBus;

public class SendingTestServer : DefaultTestServer
{
    const string StorageDirectory = "SenderTestingTransport";

    protected override AcceptanceTestingTransport GetTransportDefinition() => GetTransportDefinition(StorageDirectory);

    public static AcceptanceTestingTransport GetSendingTransportDefinition() => GetTransportDefinition(StorageDirectory);
}
