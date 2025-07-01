using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public interface IConfigureBridgeTestExecution
{
    BridgeTransport Configure(PublisherMetadata publisherMetadata);

#pragma warning disable PS0018
    Task Cleanup(BridgeTransport bridgeTransport);
#pragma warning restore PS0018
}