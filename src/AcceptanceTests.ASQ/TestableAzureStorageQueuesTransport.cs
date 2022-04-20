using NServiceBus;

public class TestableAzureStorageQueuesTransport : AzureStorageQueueTransport
{
    public TestableAzureStorageQueuesTransport(string connectionString) : base(connectionString)
    {

        MessageWrapperSerializationDefinition = new XmlSerializer();
        QueueNameSanitizer = BackwardsCompatibleQueueNameSanitizerForTests.Sanitize;
        Subscriptions.DisableCaching = true;

        DelayedDelivery.DelayedDeliveryPoisonQueue = "delays-error";
    }
}