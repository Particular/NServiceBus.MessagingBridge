using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureNonDurableTransportTestExecution : IConfigureTransportTestExecution
{
    NonDurableTransport transportDefinition;

    public Task Configure(string endpointName, EndpointConfiguration endpointConfiguration, RunSettings runSettings, PublisherMetadata publisherMetadata)
    {
        transportDefinition = new NonDurableTransport();
        endpointConfiguration.UseTransport(transportDefinition);

        endpointConfiguration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        return Task.CompletedTask;
    }

    public Task Cleanup() => Task.CompletedTask;

    public BridgeTransport Configure(PublisherMetadata publisherMetadata) => new NonDurableTransport().ToTestableBridge();

    public Task Cleanup(BridgeTransport bridgeTransport) => Task.CompletedTask;
}