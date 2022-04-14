using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Transport;

class MessageShovel
{
    public MessageShovel(
        ILogger<MessageShovel> logger,
        ITargetEndpointDispatcherRegistry targetEndpointDispatcherRegistry)
    {
        this.logger = logger;
        this.targetEndpointDispatcherRegistry = targetEndpointDispatcherRegistry;
    }

    public async Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default)
    {
        var targetEndpointDispatcher = targetEndpointDispatcherRegistry.GetTargetEndpointDispatcher(transferContext.SourceEndpointName);

        var messageContext = transferContext.MessageToTransfer;

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        var transferDetails = $"{transferContext.SourceTransport}->{targetEndpointDispatcher.TransportName}";
        messageToSend.Headers[BridgeHeaders.Transfer] = transferDetails;

        TransformAddressHeader(messageToSend, targetEndpointDispatcher, Headers.ReplyToAddress);
        TransformAddressHeader(messageToSend, targetEndpointDispatcher, FaultsHeaderKeys.FailedQ);

        await targetEndpointDispatcher.Dispatch(
            messageToSend,
            transferContext.PassTransportTransaction ? messageContext.TransportTransaction : new TransportTransaction(),
            cancellationToken).ConfigureAwait(false);

        logger.LogDebug($"{transferDetails}: Transfered message {messageToSend.MessageId}");
    }

    void TransformAddressHeader(
        OutgoingMessage messageToSend,
        TargetEndpointDispatcher targetEndpointDispatcher,
        string headerKey)
    {
        if (!messageToSend.Headers.TryGetValue(headerKey, out var headerValue))
        {
            return;
        }

        var replyToLogicalEndpointName = ParseEndpointAddress(headerValue);
        var targetSpecificReplyToAddress = targetEndpointDispatcher.ToTransportAddress(replyToLogicalEndpointName);

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
    readonly ITargetEndpointDispatcherRegistry targetEndpointDispatcherRegistry;
}
