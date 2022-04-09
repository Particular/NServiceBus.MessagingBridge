﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

class MessageShovel
{
    public MessageShovel(ILogger<MessageShovel> logger, EndpointProxyRegistry endpointProxyRegistry)
    {
        this.logger = logger;
        this.endpointProxyRegistry = endpointProxyRegistry;
    }

    public async Task TransferMessage(
        string localEndpointName,
        QueueAddress queueAddress,
        MessageContext messageContext,
        CancellationToken cancellationToken = default)
    {
        var targetEndpointProxies = endpointProxyRegistry.GetTargetEndpointProxies(localEndpointName);

        foreach (var targetEndpointProxy in targetEndpointProxies)
        {
            var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers,
                messageContext.Body);

            var address = targetEndpointProxy.ToTransportAddress(queueAddress);

            TransformAddressHeader(messageToSend, targetEndpointProxy, Headers.ReplyToAddress);
            TransformAddressHeader(messageToSend, targetEndpointProxy, FaultsHeaderKeys.FailedQ);

            var transportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(address));
            await targetEndpointProxy.Dispatch(new TransportOperations(transportOperation),
                    messageContext.TransportTransaction,
                    cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Moving message over to: [{0}] with a reply address of [{1}]", address);
        }
    }

    void TransformAddressHeader(OutgoingMessage messageToSend, IRawEndpoint targetEndpointProxy, string headerKey)
    {
        if (!messageToSend.Headers.TryGetValue(headerKey, out var headerValue))
        {
            return;
        }

        var replyToLogicalEndpointName = ParseEndpointAddress(headerValue);
        var targetSpecificReplyToAddress =
            targetEndpointProxy.ToTransportAddress(new QueueAddress(replyToLogicalEndpointName));

        messageToSend.Headers[headerKey] = targetSpecificReplyToAddress;
    }

    string ParseEndpointAddress(string replyToAddress)
    {
        return replyToAddress.Split('@').First();
        // TODO: Sql contains schema-name and possibly more
        // Sql format is like - Billing@[dbo]@[databaseName]
        // TODO: Azure Service Bus can shorten the name
        // ThisIsMyOfficialNameButItsWayTooLong -> ThisIsMyOff
    }

    readonly ILogger<MessageShovel> logger;
    readonly EndpointProxyRegistry endpointProxyRegistry;
}