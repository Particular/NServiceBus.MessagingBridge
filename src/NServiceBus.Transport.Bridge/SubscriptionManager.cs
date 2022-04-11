using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Unicast.Messages;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport;

class SubscriptionManager
{
    public SubscriptionManager(ILogger<SubscriptionManager> logger)
    {
        this.logger = logger;
    }

    public async Task SubscribeToEvents(IRawEndpoint endpointProxy,
            IList<BridgeEndpointSubscription> subscriptions,
            CancellationToken cancellationToken = default)
    {
        if (!subscriptions.Any())
        {
            return;
        }

        var eventTypes = new List<MessageMetadata>();
        foreach (var subscription in subscriptions)
        {
            var eventType = TypeGenerator.GetType(subscription.EventTypeFullName);

            eventTypes.Add(new MessageMetadata(eventType));
        }

        if (endpointProxy.SubscriptionManager != null)
        {
            await endpointProxy.SubscriptionManager.SubscribeAll(eventTypes.ToArray(), new ContextBag(), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        foreach (var subscription in subscriptions)
        {
            await SendSubscriptionMessage(endpointProxy, subscription, cancellationToken).ConfigureAwait(false);
        }
    }

    async Task SendSubscriptionMessage(IRawEndpoint endpointProxy,
            BridgeEndpointSubscription subscription,
            CancellationToken cancellationToken)
    {
        var localAddress = endpointProxy.TransportAddress;
        var subscriptionMessage = ControlMessageFactory.Create(MessageIntent.Subscribe);

        subscriptionMessage.Headers[Headers.SubscriptionMessageType] = subscription.EventTypeFullName + ",Version=1.0.0";
        subscriptionMessage.Headers[Headers.ReplyToAddress] = localAddress;
        subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = localAddress;

        subscriptionMessage.Headers[Headers.SubscriberEndpoint] = endpointProxy.EndpointName;
        subscriptionMessage.Headers[Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        subscriptionMessage.Headers[Headers.NServiceBusVersion] = "7.0.0";

        var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(subscription.Publisher));
        var transportOperations = new TransportOperations(transportOperation);

        try
        {
            await DispatchWithRetries(endpointProxy, transportOperations, 5, cancellationToken).ConfigureAwait(false);
        }
        catch (QueueNotFoundException ex)
        {
            var message = $"Failed to subscribe to {subscription.EventTypeFullName} at publisher queue {subscription.Publisher}, reason {ex.Message}";
            throw new QueueNotFoundException(subscription.Publisher, message, ex);
        }
    }


    async Task DispatchWithRetries(IRawEndpoint endpointProxy,
            TransportOperations transportOperations,
            int retriesLeft,
            CancellationToken cancellationToken)
    {
        try
        {
            await endpointProxy.Dispatch(transportOperations, new TransportTransaction(), cancellationToken).ConfigureAwait(false);
        }
        catch (QueueNotFoundException ex) when (retriesLeft > 0)
        {
            logger.LogWarning("Failed to send subscription message, retrying", ex);

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            await DispatchWithRetries(endpointProxy, transportOperations, --retriesLeft, cancellationToken).ConfigureAwait(false);
        }
    }

    static readonly RuntimeTypeGenerator TypeGenerator = new RuntimeTypeGenerator();
    readonly ILogger<SubscriptionManager> logger;
}