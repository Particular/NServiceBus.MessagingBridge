using System;
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
        transportDefinition = new TestableAzureServiceBusTransport(connectionString);
        endpointConfiguration.UseTransport(transportDefinition);

        endpointConfiguration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        return Task.CompletedTask;
    }

    public Task Cleanup() => Cleanup(transportDefinition, CancellationToken.None);

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new TestableAzureServiceBusTransport(connectionString)
    {
        TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive
    }.ToTestableBridge();

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