using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

public class MessageRouterConfiguration
{
    public MessageRouterConfiguration()
    {
    }

    public ChannelConfiguration AddTransport(TransportDefinition transportDefinition)
    {
        var channelConfiguration = new ChannelConfiguration(transportDefinition);
        channels.Add(channelConfiguration);

        return channelConfiguration;
    }

    public async Task<RunningRouter> Start(CancellationToken cancellationToken = default)
    {
        // Loop through all channels
        foreach (var channelConfiguration in channels)
        {
            // Get all endpoint-names that I need to fake (host)
            // That is all endpoint-names that I don't have on this channel.
            var endpoints = channels.Where(s => s != channelConfiguration).SelectMany(s => s.Endpoints);

            // Go through all endpoints that we need to fake on our channel
            foreach (var endpointToSimulate in endpoints)
            {
                var channelEndpointConfiguration = RawEndpointConfiguration.Create(
                    endpointToSimulate.BaseAddress,
                    channelConfiguration.TransportDefinition,
                    (mt, _, ct) => MoveMessage(endpointToSimulate, mt, ct),
                    "error");

                channelEndpointConfiguration.AutoCreateQueues();
                channelEndpointConfiguration.LimitMessageProcessingConcurrencyTo(1);

                // Create the actual endpoint
                var runningRawEndpoint = await RawEndpoint.Start(channelEndpointConfiguration, cancellationToken)
                    .ConfigureAwait(false);

                // Find the channel that has my TransportDefinition and attach it
                channels.Single(s => s.TransportDefinition == channelConfiguration.TransportDefinition)
                    .RunningEndpoint = runningRawEndpoint;

                runningEndpoints.Add(runningRawEndpoint);
            }
        }

        return new RunningRouter(runningEndpoints);
    }

    async Task MoveMessage(QueueAddress queueAddress, MessageContext messageContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("Moving the message over");

        var rawEndpoint = channels.Single(s => s.Endpoints.Any(q => q == queueAddress)).RunningEndpoint;

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        var address = rawEndpoint.ToTransportAddress(queueAddress);

        var replyToAddress = messageToSend.Headers[Headers.ReplyToAddress];
        var replyToLogicalEndpointName = ParseEndpointAddress(replyToAddress);
        var targetSpecificReplyToAddress = rawEndpoint.ToTransportAddress(new QueueAddress(replyToLogicalEndpointName));
        messageToSend.Headers[Headers.ReplyToAddress] = targetSpecificReplyToAddress;

        var transportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(address));
        await rawEndpoint.Dispatch(new TransportOperations(transportOperation), messageContext.TransportTransaction, cancellationToken)
            .ConfigureAwait(false);
    }

    string ParseEndpointAddress(string replyToAddress)
    {
        return replyToAddress.Split('@').First();
        // TODO: Sql contains schema-name and possibly more
        // TODO: Azure Service Bus can shorten the name
        // ThisIsMyOfficalNameButItsWayTooLong -> ThisIsMyOff
    }

    List<IReceivingRawEndpoint> runningEndpoints = new List<IReceivingRawEndpoint>();
    List<ChannelConfiguration> channels = new List<ChannelConfiguration>();
}
