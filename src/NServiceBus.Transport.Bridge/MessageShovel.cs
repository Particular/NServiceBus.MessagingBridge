using System.Linq;
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
    public MessageShovel(
        ILogger<MessageShovel> logger,
        ITargetEndpointProxyRegistry targetEndpointProxyRegistry)
    {
        this.logger = logger;
        this.targetEndpointProxyRegistry = targetEndpointProxyRegistry;
    }

    public async Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default)
    {
        var targetEndpointProxy = targetEndpointProxyRegistry.GetTargetEndpointProxy(transferContext.ProxyEndpointName);
        var rawEndpoint = targetEndpointProxy.RawEndpoint;

        var messageContext = transferContext.MessageToTransfer;

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        var targetEndpointAddress = rawEndpoint.ToTransportAddress(transferContext.ProxyQueueAddress);

        messageToSend.Headers[BridgeHeaders.Transfer] = $"{transferContext.SourceTransport}->{targetEndpointProxy.TransportName}";

        TransformAddressHeader(messageToSend, rawEndpoint, Headers.ReplyToAddress);
        TransformAddressHeader(messageToSend, rawEndpoint, FaultsHeaderKeys.FailedQ);

        var transportOperation = new TransportOperation(messageToSend, new UnicastAddressTag(targetEndpointAddress));

        await rawEndpoint.Dispatch(
            new TransportOperations(transportOperation),
            transferContext.PassTransportTransaction ? messageContext.TransportTransaction : new TransportTransaction(),
            cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Moving message over target endpoint: [{0}]", targetEndpointAddress);
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
    readonly ITargetEndpointProxyRegistry targetEndpointProxyRegistry;
}
