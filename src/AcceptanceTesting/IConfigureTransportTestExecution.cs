using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

public interface IConfigureTransportTestExecution
{
    TransportDefinition GetTransportDefinition();

    Task Cleanup(CancellationToken cancellationToken = default);
}
