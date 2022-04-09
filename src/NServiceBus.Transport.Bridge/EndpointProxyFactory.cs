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
        // the only scenario where it makes sense to share transaction is when transaction scopes are being used
        // NOTE: we have validation to make sure that TransportTransactionMode.TransactionScope is only used when all configured transports can support it
        var shouldPassTransportTransaction = transportConfiguration.TransportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope;

        var transportEndpointConfiguration = RawEndpointConfiguration.Create(
        endpointToProxy.Name,
        transportConfiguration.TransportDefinition,
        (messageContext, _, ct) =>
        {
            var transferContext = new TransferContext(
                endpointToProxy.Name,
                endpointToProxy.QueueAddress,
                messageContext,
                shouldPassTransportTransaction);

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