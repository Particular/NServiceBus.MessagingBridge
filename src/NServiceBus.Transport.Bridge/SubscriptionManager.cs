using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Unicast.Messages;
using NServiceBus.Unicast.Transport;

class SubscriptionManager
{
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
        }
        else
        {
            foreach (var subscription in subscriptions)
            {
                var localAddress = endpointProxy.TransportAddress;
                var subscriptionMessage = ControlMessageFactory.Create(MessageIntent.Subscribe);
                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = subscription.EventTypeFullName + ",Version=1.0.0";
                subscriptionMessage.Headers[Headers.ReplyToAddress] = localAddress;
                if (localAddress != null)
                {
                    subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = localAddress;
                }
                subscriptionMessage.Headers[Headers.SubscriberEndpoint] = endpointProxy.EndpointName;
                subscriptionMessage.Headers[Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
                subscriptionMessage.Headers[Headers.NServiceBusVersion] = "7.0.0";

                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(subscription.Publisher));
                var transportOperations = new TransportOperations(transportOperation);
                await endpointProxy.Dispatch(transportOperations, new TransportTransaction(), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    static readonly RuntimeTypeGenerator TypeGenerator = new RuntimeTypeGenerator();
}