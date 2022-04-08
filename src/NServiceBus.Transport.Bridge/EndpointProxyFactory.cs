using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Raw;

class EndpointProxyFactory
{
    public EndpointProxyFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public Task<IStartableRawEndpoint> CreateProxy(
        BridgeEndpoint endpointToProxy,
        BridgeTransportConfiguration transportConfiguration,
        CancellationToken cancellationToken = default)
    {
        var transportEndpointConfiguration = RawEndpointConfiguration.Create(
        endpointToProxy.Name,
        transportConfiguration.TransportDefinition,
        (mt, _, ct) => serviceProvider.GetRequiredService<MessageShovel>().TransferMessage(endpointToProxy.Name, endpointToProxy.QueueAddress, mt, ct),
        transportConfiguration.ErrorQueue);

        if (transportConfiguration.AutoCreateQueues)
        {
            transportEndpointConfiguration.AutoCreateQueues();
        }

        transportEndpointConfiguration.LimitMessageProcessingConcurrencyTo(transportConfiguration.Concurrency);

        return RawEndpoint.Create(transportEndpointConfiguration, cancellationToken);
    }

    readonly IServiceProvider serviceProvider;
}