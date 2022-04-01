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

public class MessageRouterConfiguration
{
    public TransportConfiguration AddTransport(TransportDefinition transportDefinition)
    {
        var transportConfiguration = new TransportConfiguration(transportDefinition);
        transports.Add(transportConfiguration);

        return transportConfiguration;
    }

    public async Task<RunningRouter> Start(CancellationToken cancellationToken = default)
    {
        // Loop through all configured transports
        foreach (var transportConfiguration in transports)
        {
            // Get all endpoint-names that I need to fake (host)
            // That is all endpoint-names that I don't have on this transport.
            var endpoints = transports.Where(s => s != transportConfiguration).SelectMany(s => s.Endpoints);

            // Go through all endpoints that we need to fake on our transport
            foreach (var endpointToSimulate in endpoints)
            {
                var transportEndpointConfiguration = RawEndpointConfiguration.Create(
                    endpointToSimulate.QueueAddress.BaseAddress,
                    transportConfiguration.TransportDefinition,
                    (mt, _, ct) => MoveMessage(endpointToSimulate.QueueAddress, mt, ct),
                    "error");

                transportEndpointConfiguration.AutoCreateQueues();
                transportEndpointConfiguration.LimitMessageProcessingConcurrencyTo(1);

                // Create the actual endpoint
                var runningRawEndpoint = await RawEndpoint.Start(transportEndpointConfiguration, cancellationToken)
                    .ConfigureAwait(false);

                // Find the transport that has my TransportDefinition and attach it
                transports.Single(s => s.TransportDefinition == transportConfiguration.TransportDefinition)
                    .RunningEndpoint = runningRawEndpoint;

                await SubscribeToEvents(runningRawEndpoint, endpointToSimulate.Subscriptions, cancellationToken)
                    .ConfigureAwait(false);

                runningEndpoints.Add(runningRawEndpoint);
            }
        }

        return new RunningRouter(runningEndpoints);
    }

    async Task SubscribeToEvents(
        IReceivingRawEndpoint runningRawEndpoint,
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
        var rawEndpoint = transports.Single(s => s.Endpoints.Any(q => q.QueueAddress == queueAddress)).RunningEndpoint;

        var messageToSend =
            new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        var address = rawEndpoint.ToTransportAddress(queueAddress);

        var replyToAddress = messageToSend.Headers[Headers.ReplyToAddress];
        var replyToLogicalEndpointName = ParseEndpointAddress(replyToAddress);
        var targetSpecificReplyToAddress = rawEndpoint.ToTransportAddress(new QueueAddress(replyToLogicalEndpointName));
        messageToSend.Headers[Headers.ReplyToAddress] = targetSpecificReplyToAddress;

        Console.WriteLine("Moving the message over to: {0} with a reply to {1}", address,
            messageToSend.Headers[Headers.ReplyToAddress]);
        var transportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(address));
        await rawEndpoint.Dispatch(new TransportOperations(transportOperation), messageContext.TransportTransaction,
                cancellationToken)
            .ConfigureAwait(false);
    }

    string ParseEndpointAddress(string replyToAddress)
    {
        return replyToAddress.Split('@').First();
        // TODO: Sql contains schema-name and possibly more
        // Sql format is like - Billing@[dbo]@[databaseName]
        // TODO: Azure Service Bus can shorten the name
        // ThisIsMyOfficalNameButItsWayTooLong -> ThisIsMyOff
    }

    static RuntimeTypeGenerator typeGenerator = new RuntimeTypeGenerator();
    List<IReceivingRawEndpoint> runningEndpoints = new List<IReceivingRawEndpoint>();
    List<TransportConfiguration> transports = new List<TransportConfiguration>();
}