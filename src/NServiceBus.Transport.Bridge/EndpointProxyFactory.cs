using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Transport;

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
        var transportDefinition = transportConfiguration.TransportDefinition;
        // the only scenario where it makes sense to share transaction is when transaction scopes are being used
        // NOTE: we have validation to make sure that TransportTransactionMode.TransactionScope is only used when all configured transports can support it
        var shouldPassTransportTransaction = transportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope;

#pragma warning disable CS0618 // Type or member is obsolete
        // the transport seam assumes the error queue address to be a native address so we need to translate
        // unfortunately this method is obsoleted but we can't use the one on TransportInfrastructure since that is too late
        var translatedErrorQueue = transportDefinition.ToTransportAddress(new QueueAddress(transportConfiguration.ErrorQueue));
#pragma warning restore CS0618 // Type or member is obsolete


        var addressParser = DetermineAddressParser(transportConfiguration);

        var transportEndpointConfiguration = RawEndpointConfiguration.Create(
        endpointToProxy.Name,
        transportConfiguration.TransportDefinition,
        (messageContext, _, ct) =>
        {
            var transferContext = new TransferContext(
                transportConfiguration.Name,
                endpointToProxy.Name,
                endpointToProxy.QueueAddress,
                messageContext,
                shouldPassTransportTransaction,
                addressParser);

            return serviceProvider.GetRequiredService<MessageShovel>()
                .TransferMessage(transferContext, ct);
        },
        translatedErrorQueue);

        if (transportConfiguration.AutoCreateQueues)
        {
            transportEndpointConfiguration.AutoCreateQueues();
        }

        transportEndpointConfiguration.LimitMessageProcessingConcurrencyTo(transportConfiguration.Concurrency);

        return RawEndpoint.Create(transportEndpointConfiguration, cancellationToken);
    }

    static ITransportAddressParser DetermineAddressParser(BridgeTransportConfiguration transportConfiguration)
    {
        if (transportConfiguration.CustomAddressParser != null)
        {
            return transportConfiguration.CustomAddressParser;
        }

        if (transportConfiguration.TransportDefinition.GetType().Name.ToLower().Contains("msmq"))
        {
            return new MsmqAddressParser();
        }

        return new NoOpAddressParser();
    }

    readonly IServiceProvider serviceProvider;
}