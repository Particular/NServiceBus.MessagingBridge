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
        (messageContext, _, ct) =>
        {
            var transferContext = new TransferContext(endpointToProxy.Name, endpointToProxy.QueueAddress, messageContext);

            return serviceProvider.GetRequiredService<MessageShovel>()
                .TransferMessage(transferContext, ct);
        },
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