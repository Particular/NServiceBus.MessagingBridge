using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureAzureServiceBusTransportTestExecution : IConfigureTransportTestExecution
{
    readonly string connectionString = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
    TestableAzureServiceBusTransport transportDefinition;

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        var topology = TopicTopology.Default;
        topology.OverrideSubscriptionNameFor(endpointName, endpointName.Shorten());

        foreach (var eventType in publisherMetadata.Publishers.SelectMany(p => p.Events))
        {
            topology.PublishTo(eventType, eventType.ToTopicName());
            topology.SubscribeTo(eventType, eventType.ToTopicName());
        }

        transportDefinition = new TestableAzureServiceBusTransport(connectionString, topology);
        endpointConfiguration.UseTransport(transportDefinition);

        endpointConfiguration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        return Task.CompletedTask;
    }

    public Task Cleanup() => Cleanup(transportDefinition, CancellationToken.None);

    public BridgeTransport Configure(PublisherMetadata publisherMetadata)
    {
        var topology = TopicTopology.Default;

        foreach (var publisher in publisherMetadata.Publishers.Select(p => p.PublisherName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            topology.OverrideSubscriptionNameFor(publisher, publisher.Shorten());
        }

        foreach (var eventType in publisherMetadata.Publishers.SelectMany(p => p.Events))
        {
            topology.PublishTo(eventType, eventType.ToTopicName());
            topology.SubscribeTo(eventType, eventType.ToTopicName());
        }

        return new TestableAzureServiceBusTransport(connectionString, topology)
        {
            TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
        }.ToTestableBridge();
    }

    public Task Cleanup(BridgeTransport bridgeTransport) => Cleanup(bridgeTransport.FromTestableBridge<TestableAzureServiceBusTransport>(), CancellationToken.None);

    static Task Cleanup(TestableAzureServiceBusTransport transport, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            transport.EnablePartitioning = true;
        }

        return Task.CompletedTask;
    }
}