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
using NServiceBus.Unicast.Transport;

class EndpointProxy
{
    public EndpointProxy(
        BridgeConfiguration configuration,
        ILogger<EndpointProxy> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task Start(
        BridgeEndpoint endpointToProxy,
        BridgeTransportConfiguration transportConfiguration,
        CancellationToken cancellationToken = default)
    {
        var transportEndpointConfiguration = RawEndpointConfiguration.Create(
        endpointToProxy.Name,
        transportConfiguration.TransportDefinition,
        (mt, _, ct) => MoveMessage(endpointToProxy.QueueAddress, mt, ct),
        transportConfiguration.ErrorQueue);

        if (transportConfiguration.AutoCreateQueues)
        {
            transportEndpointConfiguration.AutoCreateQueues();
        }

        transportEndpointConfiguration.LimitMessageProcessingConcurrencyTo(transportConfiguration.Concurrency);

        // Create the actual endpoint
        runningRawEndpoint = await NServiceBus.Raw.RawEndpoint.Start(transportEndpointConfiguration, cancellationToken)
            .ConfigureAwait(false);

        await SubscribeToEvents(endpointToProxy.Subscriptions, cancellationToken).ConfigureAwait(false);
    }

    public Task Stop(CancellationToken cancellationToken = default) => runningRawEndpoint.Stop(cancellationToken);

    async Task SubscribeToEvents(
        IList<BridgeEndpointSubscription> subscriptions,
        CancellationToken cancellationToken)
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

        if (runningRawEndpoint.SubscriptionManager != null)
        {
            await runningRawEndpoint.SubscriptionManager.SubscribeAll(eventTypes.ToArray(), new ContextBag(), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            foreach (var subscription in subscriptions)
            {
                var localAddress = runningRawEndpoint.TransportAddress;
                var subscriptionMessage = ControlMessageFactory.Create(MessageIntent.Subscribe);
                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = subscription.EventTypeFullName + ",Version=1.0.0";
                subscriptionMessage.Headers[Headers.ReplyToAddress] = localAddress;
                if (localAddress != null)
                {
                    subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = localAddress;
                }
                subscriptionMessage.Headers[Headers.SubscriberEndpoint] = runningRawEndpoint.EndpointName;
                subscriptionMessage.Headers[Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
                subscriptionMessage.Headers[Headers.NServiceBusVersion] = "7.0.0";

                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(subscription.Publisher));
                var transportOperations = new TransportOperations(transportOperation);
                await runningRawEndpoint.Dispatch(transportOperations, new TransportTransaction(), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    async Task MoveMessage(QueueAddress queueAddress, MessageContext messageContext,
        CancellationToken cancellationToken)
    {
        var transports = configuration.TransportConfigurations;

        var rawEndpoint = transports.Single(s => s.Endpoints.Any(q => q.QueueAddress == queueAddress))
            .Proxy.RawEndpoint;

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        var address = rawEndpoint.ToTransportAddress(queueAddress);

        var replyToAddress = messageToSend.Headers[Headers.ReplyToAddress];
        var replyToLogicalEndpointName = ParseEndpointAddress(replyToAddress);
        var targetSpecificReplyToAddress = rawEndpoint.ToTransportAddress(new QueueAddress(replyToLogicalEndpointName));
        messageToSend.Headers[Headers.ReplyToAddress] = targetSpecificReplyToAddress;

        var transportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(address));
        await rawEndpoint.Dispatch(new TransportOperations(transportOperation), messageContext.TransportTransaction,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Moving the message over to: {0} with a reply to {1}", address, targetSpecificReplyToAddress);
    }

    string ParseEndpointAddress(string replyToAddress)
    {
        return replyToAddress.Split('@').First();
        // TODO: Sql contains schema-name and possibly more
        // Sql format is like - Billing@[dbo]@[databaseName]
        // TODO: Azure Service Bus can shorten the name
        // ThisIsMyOfficialNameButItsWayTooLong -> ThisIsMyOff
    }

    IReceivingRawEndpoint runningRawEndpoint;

    readonly BridgeConfiguration configuration;
    readonly ILogger<EndpointProxy> logger;

    static readonly RuntimeTypeGenerator TypeGenerator = new RuntimeTypeGenerator();

    IRawEndpoint RawEndpoint => runningRawEndpoint;
}