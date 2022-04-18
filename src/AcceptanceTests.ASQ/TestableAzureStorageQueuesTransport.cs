using NServiceBus;

public class TestableAzureStorageQueuesTransport : AzureStorageQueueTransport
{
    public TestableAzureStorageQueuesTransport(string connectionString) : base(connectionString)
    {
    }
}