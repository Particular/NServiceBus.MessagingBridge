using System;
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
        TargetEndpointDispatcher targetEndpointDispatcher = null;
        try
        {
            targetEndpointDispatcher = targetEndpointRegistry.GetTargetEndpointDispatcher(transferContext.SourceEndpointName);

            var messageContext = transferContext.MessageToTransfer;

            var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

            var transferDetails = $"{transferContext.SourceTransport}->{targetEndpointDispatcher.TransportName}";

            // Audit messages contain all the original fields. Transforming them would destroy this.  
            if (!IsAuditMessage(messageToSend))
            {
                messageToSend.Headers[BridgeHeaders.Transfer] = transferDetails;

                TransformAddressHeader(messageToSend, targetEndpointRegistry, Headers.ReplyToAddress);
                TransformAddressHeader(messageToSend, targetEndpointRegistry, FaultsHeaderKeys.FailedQ);
            }

            await targetEndpointDispatcher.Dispatch(
                messageToSend,
                transferContext.PassTransportTransaction ? messageContext.TransportTransaction : new TransportTransaction(),
                cancellationToken).ConfigureAwait(false);

            logger.LogDebug("{TransferDetails}: Transferred message {MessageId}", transferDetails, messageToSend.MessageId);
        }
        catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to shovel message for endpoint {transferContext.SourceEndpointName} with id {transferContext.MessageToTransfer.NativeMessageId} from {transferContext.SourceTransport} to {targetEndpointDispatcher?.TransportName}", ex);
        }
    }

    // Assuming that a message is an audit message if a ProcessingMachine is known
    static bool IsAuditMessage(OutgoingMessage messageToSend) => messageToSend.Headers.ContainsKey(Headers.ProcessingMachine);

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
