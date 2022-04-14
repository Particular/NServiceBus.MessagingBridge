using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvancedExtensibility;
using Testing;

public class ConfigureAzureStorageQueuesTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("AzureStorageQueueTransport_ConnectionString");
    public BridgeTransportDefinition GetBridgeTransport()
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No connectionstring for found in environment variable 'AzureStorageQueueTransport_ConnectionString'");
        }

        var transportDefinition = new TestableAzureStorageQueuesTransport(connectionString)
        {
            MessageWrapperSerializationDefinition = new XmlSerializer(),
            QueueNameSanitizer = BackwardsCompatibleQueueNameSanitizerForTests.Sanitize,
            Subscriptions = { DisableCaching = true }
        };
        transportDefinition.DelayedDelivery.DelayedDeliveryPoisonQueue = "error";

        return new BridgeTransportDefinition()
        {
            TransportDefinition = transportDefinition,
            // new BridgeTransportConfiguration(transportDefinition),
            Cleanup = (ct) => Cleanup(ct)
        };
    }

    public Func<CancellationToken, Task> ConfigureTransportForEndpoint(EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata)
    {
        var transportDefinition = new TestableAzureStorageQueuesTransport(connectionString);
        var errorQueue = endpointConfiguration.GetSettings().GetOrDefault<string>(ErrorQueueSettings.SettingsKey);

        transportDefinition = Utilities.CreateTransportWithDefaultTestsConfiguration(connectionString, delayedDeliveryPoisonQueue: errorQueue);
        transportDefinition.Subscriptions.DisableCaching = true;

        var routingConfig = endpointConfiguration.UseTransport(transportDefinition);

        foreach (var publisher in publisherMetadata.Publishers)
        {
            foreach (var eventType in publisher.Events)
            {
                routingConfig.RegisterPublisher(eventType, publisher.PublisherName);
            }
        }

        endpointConfiguration.UseSerialization<XmlSerializer>();

        return ct => Cleanup(ct);
    }

    Task Cleanup(CancellationToken cancellationToken) => Task.CompletedTask;
}
