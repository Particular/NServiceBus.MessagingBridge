using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

public interface IConfigureTransportTestExecution
{
    TransportDefinition GetTransportDefinition();

    void ApplyCustomEndpointConfiguration(EndpointConfiguration endpointConfiguration);

    Task Cleanup(CancellationToken cancellationToken = default);
}
