using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

class ConfigureMsmqTransportTestExecution : IConfigureTransportTestExecution
{
    public TransportDefinition GetTransportDefinition()
    {
        return new MsmqTransport();
    }

    public void ApplyCustomEndpointConfiguration(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.UsePersistence<MsmqPersistence, StorageType.Subscriptions>();
    }

    public Task Cleanup(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}