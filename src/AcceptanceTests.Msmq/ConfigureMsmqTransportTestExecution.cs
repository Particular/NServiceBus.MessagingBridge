using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class ConfigureMsmqTransportTestExecution : IConfigureTransportTestExecution
{
    public TransportDefinition GetTransportDefinition()
    {
        //TODO: need to add a way to configure persistence
        return new MsmqTransport();
    }

    public Task Cleanup(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}