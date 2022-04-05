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

public class EndpointProxy
{
    public EndpointProxy(
        FinalizedRouterConfiguration configuration,
        ILogger<EndpointProxy> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task Start(
        Endpoint endpointToProxy,
        TransportConfiguration transportConfiguration,
        CancellationToken cancellationToken = default)
    {
        var transportEndpointConfiguration = RawEndpointConfiguration.Create(
        endpointToProxy.QueueAddress.BaseAddress,
        transportConfiguration.TransportDefinition,
        (mt, _, ct) => MoveMessage(endpointToProxy.QueueAddress, mt, ct),
        "error");

        transportEndpointConfiguration.AutoCreateQueues();
        transportEndpointConfiguration.LimitMessageProcessingConcurrencyTo(1);

        // Create the actual endpoint
        runningRawEndpoint = await NServiceBus.Raw.RawEndpoint.Start(transportEndpointConfiguration, cancellationToken)
            .ConfigureAwait(false);

        await SubscribeToEvents(endpointToProxy.Subscriptions, cancellationToken).ConfigureAwait(false);

    }

    public Task Stop(CancellationToken cancellationToken = default)
    {
        return runningRawEndpoint.Stop(cancellationToken);
    }

    async Task SubscribeToEvents(
        IList<Subscription> subscriptions,
        CancellationToken cancellationToken)
    {
        if (!subscriptions.Any())
        {
            return;
        }

        var eventTypes = new List<MessageMetadata>();
        foreach (var subscription in subscriptions)
        {
            var eventType = typeGenerator.GetType(subscription.EventTypeFullName);

            eventTypes.Add(new MessageMetadata(eventType));
        }

        if (runningRawEndpoint.SubscriptionManager != null)
        {
            await runningRawEndpoint.SubscriptionManager.SubscribeAll(eventTypes.ToArray(), new ContextBag(), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            //TODO: Send a subscription message
            //runningRawEndpoint.Dispatch(); https://github.com/SzymonPobiega/NServiceBus.Router/blob/master/src/NServiceBus.Router/MessageDrivenPubSub.cs
        }
    }

    async Task MoveMessage(QueueAddress queueAddress, MessageContext messageContext,
        CancellationToken cancellationToken)
    {
        var transports = configuration.Transports;

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
        // ThisIsMyOfficalNameButItsWayTooLong -> ThisIsMyOff
    }

    IReceivingRawEndpoint runningRawEndpoint;

    readonly FinalizedRouterConfiguration configuration;
    readonly ILogger<EndpointProxy> logger;

    static RuntimeTypeGenerator typeGenerator = new RuntimeTypeGenerator();

    public IRawEndpoint RawEndpoint => runningRawEndpoint;
}