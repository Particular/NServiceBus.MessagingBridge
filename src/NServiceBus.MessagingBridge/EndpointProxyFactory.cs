using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        BridgeTransport transportConfiguration,
        CancellationToken cancellationToken = default)
    {
        var transportDefinition = transportConfiguration.TransportDefinition;
        // the only scenario where it makes sense to share transaction is when transaction scopes are being used
        // NOTE: we have validation to make sure that TransportTransactionMode.TransactionScope is only used when all configured transports can support it
        var shouldPassTransportTransaction = transportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope;

        // the transport seam assumes the error queue address to be a native address so we need to translate
        // unfortunately this method is obsoleted but we can't use the one on TransportInfrastructure since that is too late
        //TODO: var translatedErrorQueue = transportAddressResolver.ToTransportAddress(new QueueAddress(transportConfiguration.ErrorQueue));

        var transportEndpointConfiguration = RawEndpointConfiguration.Create(
        endpointToProxy.Name,
        transportConfiguration.TransportDefinition,
        (messageContext, _, ct) =>
        {
            if (IsSubscriptionMessage(messageContext.Headers))
            {
                return Task.CompletedTask;
            }

            var transferContext = new TransferContext(
                transportConfiguration.Name,
                endpointToProxy.Name,
                messageContext,
                shouldPassTransportTransaction);

            return serviceProvider.GetRequiredService<IMessageShovel>()
                .TransferMessage(transferContext, cancellationToken: ct);
        },
        transportConfiguration.ErrorQueue);

        if (transportConfiguration.AutoCreateQueues)
        {
            transportEndpointConfiguration.AutoCreateQueues();
        }

        transportEndpointConfiguration.LimitMessageProcessingConcurrencyTo(transportConfiguration.Concurrency);

        transportEndpointConfiguration.CustomErrorHandlingPolicy(new MessageShovelErrorHandlingPolicy(
            serviceProvider.GetRequiredService<ILogger<MessageShovelErrorHandlingPolicy>>(),
            transportConfiguration.ErrorQueue));

        return RawEndpoint.Create(transportEndpointConfiguration, cancellationToken);
    }

    static bool IsSubscriptionMessage(IReadOnlyDictionary<string, string> messageContextHeaders)
    {
        var messageIntent = default(MessageIntent);
        if (messageContextHeaders.TryGetValue(Headers.MessageIntent, out var messageIntentString))
        {
            Enum.TryParse(messageIntentString, true, out messageIntent);
        }

        return messageIntent is MessageIntent.Subscribe or MessageIntent.Unsubscribe;
    }

    readonly IServiceProvider serviceProvider;
}