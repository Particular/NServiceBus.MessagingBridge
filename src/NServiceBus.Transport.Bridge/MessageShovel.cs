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
        IEndpointRegistry targetEndpointRegistry)
    {
        this.logger = logger;
        this.targetEndpointRegistry = targetEndpointRegistry;
    }

    public async Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default)
    {
        var targetEndpointDispatcher = targetEndpointRegistry.GetTargetEndpointDispatcher(transferContext.SourceEndpointName);

        var messageContext = transferContext.MessageToTransfer;

        var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

        var transferDetails = $"{transferContext.SourceTransport}->{targetEndpointDispatcher.TransportName}";
        messageToSend.Headers[BridgeHeaders.Transfer] = transferDetails;

        TransformAddressHeader(messageToSend, targetEndpointRegistry, Headers.ReplyToAddress);
        TransformAddressHeader(messageToSend, targetEndpointRegistry, FaultsHeaderKeys.FailedQ);

        await targetEndpointDispatcher.Dispatch(
            messageToSend,
            transferContext.PassTransportTransaction ? messageContext.TransportTransaction : new TransportTransaction(),
            cancellationToken).ConfigureAwait(false);

        logger.LogDebug($"{transferDetails}: Transfered message {messageToSend.MessageId}");
    }

    void TransformAddressHeader(
        OutgoingMessage messageToSend,
        IEndpointRegistry targetEndpointRegistry,
        string headerKey)
    {
        if (!messageToSend.Headers.TryGetValue(headerKey, out var headerValue))
        {
            return;
        }

        var targetSpecificReplyToAddress = targetEndpointRegistry.TranslateToTargetAddress(headerValue);

        messageToSend.Headers[headerKey] = targetSpecificReplyToAddress;
    }

    readonly ILogger<MessageShovel> logger;
    readonly IEndpointRegistry targetEndpointRegistry;
}
