using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Raw;

class EndpointProxyFactory(IServiceProvider serviceProvider)
{
    public Task<IStartableRawEndpoint> CreateProxy(
        BridgeEndpoint endpointToProxy,
        BridgeTransport transportConfiguration,
        CancellationToken cancellationToken = default)
    {
        var transportDefinition = transportConfiguration.TransportDefinition;
        // the only scenario where it makes sense to share transaction is when transaction scopes are being used
        // NOTE: we have validation to make sure that TransportTransactionMode.TransactionScope is only used when all configured transports can support it
        var shouldPassTransportTransaction = transportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope;

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
            serviceProvider.GetRequiredService<ILogger<MessageShovelErrorHandlingPolicy>>()));

        return RawEndpoint.Create(transportEndpointConfiguration, cancellationToken);
    }

    public static Task<IStartableRawEndpoint> CreateDispatcher(BridgeTransport transportConfiguration, CancellationToken cancellationToken = default)
    {
        var endpointConfiguration = RawEndpointConfiguration.CreateSendOnly($"bridge-dispatcher-{transportConfiguration.Name}", transportConfiguration.TransportDefinition);

        return RawEndpoint.Create(endpointConfiguration, cancellationToken);
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
}