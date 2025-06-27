using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public interface IConfigureTransportTestExecution
{
    BridgeTransportDefinition GetBridgeTransport();

    Func<CancellationToken, Task> ConfigureTransportForEndpoint(string endpointName, EndpointConfiguration endpointConfiguration, PublisherMetadata publisherMetadata);
}
