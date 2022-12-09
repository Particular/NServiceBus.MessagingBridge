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

            if (IsErrorMessage(messageToSend))
            {
                //This is a failed message forwarded to ServiceControl. We need to transform the FailedQ header so that ServiceControl returns the message
                //to the correct queue/transport on the other side

                //We _do not_ transform the ReplyToAddress header
                TransformAddressHeader(messageToSend, targetEndpointRegistry, FaultsHeaderKeys.FailedQ);
            }
            else if (IsRetryMessage(messageToSend))
            {
                //This is a message retried from ServiceControl. Its ReplyToAddress header has been preserved (as stated above) so we don't need to transform it back
            }
            else if (IsAuditMessage(messageToSend))
            {
                //This is a message sent to the audit queue. We _do not_ transform its ReplyToAddress header
            }
            else
            {
                // This is a regular message sent between the endpoints on different sides of the bridge.
                // The ReplyToAddress is transformed to allow for replies to be delivered
                messageToSend.Headers[BridgeHeaders.Transfer] = transferDetails;
                TransformAddressHeader(messageToSend, targetEndpointRegistry, Headers.ReplyToAddress);
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
    static bool IsAuditMessage(OutgoingMessage messageToSend) => messageToSend.Headers.ContainsKey(Headers.ProcessingEnded);

    static bool IsErrorMessage(OutgoingMessage messageToSend) => messageToSend.Headers.ContainsKey(FaultsHeaderKeys.FailedQ);

    static bool IsRetryMessage(OutgoingMessage messageToSend) => messageToSend.Headers.ContainsKey("ServiceControl.Retry.UniqueMessageId");

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
