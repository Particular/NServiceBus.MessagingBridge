using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Transport;

sealed class MessageShovel : IMessageShovel
{
    public MessageShovel(
        ILogger<MessageShovel> logger,
        IEndpointRegistry targetEndpointRegistry,
        FinalizedBridgeConfiguration finalizedBridgeConfiguration)
    {
        this.logger = logger;
        this.targetEndpointRegistry = targetEndpointRegistry;
        translateReplyToAddressForFailedMessages = finalizedBridgeConfiguration.TranslateReplyToAddressForFailedMessages;
    }

    [Transaction()]
    public async Task TransferMessage(TransferContext transferContext, CancellationToken cancellationToken = default)
    {
        TargetEndpointDispatcher targetEndpointDispatcher = null;
        try
        {
            targetEndpointDispatcher = targetEndpointRegistry.GetTargetEndpointDispatcher(transferContext.SourceEndpointName);

            SetNewRelicTransaction(
                transferContext.MessageToTransfer.Headers,
                transferContext.SourceTransport,
                targetEndpointDispatcher.TransportName);

            var messageContext = transferContext.MessageToTransfer;

            var messageToSend = new OutgoingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);
            messageToSend.Headers.Remove(BridgeHeaders.FailedQ);

            var length = transferContext.SourceTransport.Length + targetEndpointDispatcher.TransportName.Length + 2 /* ->*/;
            var transferDetails = string.Create(length,
                (Source: transferContext.SourceTransport, Target: targetEndpointDispatcher.TransportName),
                static (chars, context) =>
                {
                    var position = 0;
                    context.Source.AsSpan().CopyTo(chars);
                    position += context.Source.Length;
                    chars[position++] = '-';
                    chars[position++] = '>';
                    context.Target.AsSpan().CopyTo(chars.Slice(position));
                });

            if (IsErrorMessage(messageToSend))
            {
                //This is a failed message forwarded to ServiceControl. We need to transform the FailedQ header so that ServiceControl returns the message
                //to the correct queue/transport on the other side

                TransformAddressHeader(messageToSend, targetEndpointRegistry, FaultsHeaderKeys.FailedQ);

                if (translateReplyToAddressForFailedMessages)
                {
                    //Try to translate the ReplyToAddress, this is needed when an endpoint is migrated to the ServiceControl side before this message is retried
                    TransformAddressHeader(messageToSend, targetEndpointRegistry, Headers.ReplyToAddress);
                }
            }
            else if (IsAuditMessage(messageToSend))
            {
                //This is a message sent to the audit queue. We _do not_ transform any headers.
                //This check needs to be done _before_ the retry message check because we don't want to treat audited retry messages as retry messages.
            }
            else if (IsRetryMessage(messageToSend))
            {
                //Transform the retry ack queue address
                TransformAddressHeader(messageToSend, targetEndpointRegistry, "ServiceControl.Retry.AcknowledgementQueue");

                if (translateReplyToAddressForFailedMessages)
                {
                    //This is a message retried from ServiceControl. We try to translate its ReplyToAddress.
                    TransformAddressHeader(messageToSend, targetEndpointRegistry, Headers.ReplyToAddress);
                }
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

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("{TransferDetails}: Transferred message {MessageId}", transferDetails, messageToSend.MessageId);
            }
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
        IEndpointRegistry endpointRegistry,
        string addressHeaderKey)
    {
        if (!messageToSend.Headers.TryGetValue(addressHeaderKey, out var sourceAddress))
        {
            return;
        }

        if (endpointRegistry.TryTranslateToTargetAddress(sourceAddress, out string bestMatch))
        {
            messageToSend.Headers[addressHeaderKey] = bestMatch;
        }
        else
        {
            throw new Exception($"No target address mapping could be found for source address: {sourceAddress}. Ensure names have correct casing as mappings are case-sensitive. Nearest configured match: {bestMatch}");
        }
    }

    readonly ILogger<MessageShovel> logger;
    readonly IEndpointRegistry targetEndpointRegistry;
    readonly bool translateReplyToAddressForFailedMessages;

    static void SetNewRelicTransaction(
        IDictionary<string, string> messageHeaders,
        string sourceTransport,
        string targetTransport)
    {
        var transactionName = "Message/NServiceBus/Queue/Unknown";
        if (messageHeaders.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageTypes))
        {
            var messageType = enclosedMessageTypes.Split(',', 2)[0];
            transactionName = "Message/NServiceBus/Queue/Named/" + messageType;
        }
        NewRelic.Api.Agent.NewRelic.SetTransactionName(null, transactionName);

        messageHeaders.TryGetValue(Headers.OriginatingEndpoint, out var originatingEndpoint);

        var newRelicAgent = NewRelic.Api.Agent.NewRelic.GetAgent();
        newRelicAgent.CurrentTransaction
            .AddCustomAttribute("NServiceBus.Bridge.SourceTransport", sourceTransport)
            .AddCustomAttribute("NServiceBus.Bridge.TargetTransport", targetTransport)
            .AddCustomAttribute(Headers.OriginatingEndpoint, originatingEndpoint);
    }
}