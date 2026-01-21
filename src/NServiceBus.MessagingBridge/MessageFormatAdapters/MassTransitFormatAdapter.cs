namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Metadata;
using NServiceBus.Faults;
using NsbHeaders = NServiceBus.Headers;

/// <summary>
/// Adapter for transforming messages between MassTransit format and NServiceBus format
/// </summary>
public sealed class MassTransitFormatAdapter : IMessageFormatAdapter
{
    /// <inheritdoc />
    public string Name => "MassTransit";

    /// <inheritdoc />
    public Task TransformIncoming(Transport.MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var headers = messageContext.Headers;

        // Validate this is a MassTransit failure message
        if (!headers.TryGetValue(MessageHeaders.Reason, out var reason) || reason != "fault")
        {
            throw new InvalidOperationException("Can only forward MassTransit failure messages. Expected MT-Reason header with value 'fault'.");
        }

        // Detect if message uses envelope format (no MT-MessageId header means it's enveloped)
        var hasEnvelope = !headers.ContainsKey(MessageHeaders.MessageId);

        // Set content type based on format
        var contentType = hasEnvelope
            ? "application/vnd.masstransit+json"
            : "application/json";

        headers[NsbHeaders.ContentType] = contentType;

        if (hasEnvelope)
        {
            TransformEnvelopedMessage(messageContext);
        }
        else
        {
            TransformHeaderOnlyMessage(messageContext);
        }

        // Map MassTransit fault headers to NServiceBus fault headers
        TransformFaultHeaders(messageContext);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TransformOutgoing(Transport.MessageContext messageContext, string targetTransportName, CancellationToken cancellationToken = default)
    {
        var headers = messageContext.Headers;

        // Remove all NServiceBus headers
        var keysToRemove = new List<string>();
        foreach (var key in headers.Keys)
        {
            if (key.StartsWith("NServiceBus."))
            {
                keysToRemove.Add(key);
            }
            if (key.StartsWith("MT-Fault"))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            headers.Remove(key);
        }

        headers.Remove("MT-Reason");

        // Apply transport-specific transformations
        ApplyTransportSpecificTransformations(messageContext, targetTransportName);

        return Task.CompletedTask;
    }

    void TransformEnvelopedMessage(Transport.MessageContext messageContext)
    {
        var messageEnvelope = DeserializeEnvelope(messageContext);
        var headers = messageContext.Headers;

        // Map envelope properties to NServiceBus headers
        headers[NsbHeaders.MessageId] = messageEnvelope.MessageId;
        headers[NsbHeaders.EnclosedMessageTypes] = string.Join(",", messageEnvelope.MessageType!);
        headers[NsbHeaders.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(messageEnvelope.SentTime!.Value);
        headers[NsbHeaders.ConversationId] = messageEnvelope.ConversationId;

        if (messageEnvelope.CorrelationId != null)
        {
            headers[NsbHeaders.CorrelationId] = messageEnvelope.CorrelationId;
        }

        if (messageEnvelope.ExpirationTime.HasValue)
        {
            headers[NsbHeaders.TimeToBeReceived] = DateTimeOffsetHelper.ToWireFormattedString(messageEnvelope.ExpirationTime.Value);
        }

        headers[NsbHeaders.OriginatingEndpoint] = messageEnvelope.SourceAddress;

        if (messageEnvelope.Host?.MachineName != null)
        {
            headers[NsbHeaders.OriginatingMachine] = messageEnvelope.Host.MachineName;
        }
    }

    void TransformHeaderOnlyMessage(Transport.MessageContext messageContext)
    {
        var headers = messageContext.Headers;

        // Map MassTransit headers to NServiceBus headers
        headers[NsbHeaders.MessageId] = headers[MessageHeaders.MessageId];
        headers[NsbHeaders.EnclosedMessageTypes] = headers[MessageHeaders.MessageType];

        if (headers.TryGetValue(MessageHeaders.TransportSentTime, out var sent))
        {
            headers[NsbHeaders.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.Parse(sent));
        }
        else
        {
            // Use time of failure as fallback
            var faultTimestampFallback = headers[MessageHeaders.FaultTimestamp];
            headers[NsbHeaders.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.Parse(faultTimestampFallback));
        }

        headers[NsbHeaders.ConversationId] = headers[MessageHeaders.ConversationId];

        if (headers.TryGetValue(MessageHeaders.CorrelationId, out var correlationId))
        {
            headers[NsbHeaders.CorrelationId] = correlationId;
        }

        headers[NsbHeaders.OriginatingEndpoint] = headers[MessageHeaders.SourceAddress];

        if (headers.TryGetValue(MessageHeaders.Host.Info, out var hostInfo))
        {
            var busHostInfo = JsonSerializer.Deserialize<BusHostInfo>(hostInfo)
                ?? throw new InvalidOperationException("Failed to deserialize MassTransit host info");
            headers[NsbHeaders.OriginatingMachine] = busHostInfo.MachineName;
        }
    }

    void TransformFaultHeaders(Transport.MessageContext messageContext)
    {
        var headers = messageContext.Headers;

        // Map fault retry count
        if (headers.TryGetValue(MessageHeaders.FaultRetryCount, out var faultRetryCount))
        {
            headers[NsbHeaders.DelayedRetries] = faultRetryCount;
        }

        // Map fault input address (original processing queue)
        if (headers.TryGetValue(MessageHeaders.FaultInputAddress, out var faultInputAddress))
        {
            headers[NsbHeaders.ProcessingEndpoint] = faultInputAddress;
            headers[FaultsHeaderKeys.FailedQ] = faultInputAddress;
        }
        else
        {
            // Fallback: use the receive address as the fault input address
            faultInputAddress = messageContext.ReceiveAddress;
            headers[NsbHeaders.ProcessingEndpoint] = faultInputAddress;
            headers[MessageHeaders.FaultInputAddress] = "queue:" + faultInputAddress;
            headers[FaultsHeaderKeys.FailedQ] = faultInputAddress;
        }

        // Map exception information
        if (headers.TryGetValue(MessageHeaders.FaultExceptionType, out var faultExceptionType))
        {
            headers[FaultsHeaderKeys.ExceptionType] = faultExceptionType;
        }

        headers[FaultsHeaderKeys.Message] = headers[MessageHeaders.FaultMessage];

        if (headers.TryGetValue(MessageHeaders.FaultStackTrace, out var faultStackTrace))
        {
            headers[FaultsHeaderKeys.StackTrace] = faultStackTrace;
        }

        // Map time of failure
        if (headers.TryGetValue(MessageHeaders.FaultTimestamp, out var faultTimestamp))
        {
            headers[FaultsHeaderKeys.TimeOfFailure] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.Parse(faultTimestamp));
        }
        else
        {
            // Fallback to current time
            headers[FaultsHeaderKeys.TimeOfFailure] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
        }

        // Map processing machine (MassTransit doesn't have exact equivalent, use host machine name)
        headers[NsbHeaders.ProcessingMachine] = headers.GetValueOrDefault(MessageHeaders.Host.MachineName, "Unknown");
    }

    void ApplyTransportSpecificTransformations(Transport.MessageContext messageContext, string targetTransportName)
    {
        // RabbitMQ uses "exchange:" prefix for acknowledgment queue
        if (targetTransportName.Contains("RabbitMQ", StringComparison.OrdinalIgnoreCase))
        {
            const string RetryConfirmationQueueHeaderKey = "ServiceControl.Retry.AcknowledgementQueue";
            if (messageContext.Headers.TryGetValue(RetryConfirmationQueueHeaderKey, out var ackQueue))
            {
                messageContext.Headers[RetryConfirmationQueueHeaderKey] = "exchange:" + ackQueue;
            }
        }
        else
        {
            // Other transports (AzureServiceBus, AmazonSQS) use "queue:" prefix
            const string RetryConfirmationQueueHeaderKey = "ServiceControl.Retry.AcknowledgementQueue";
            if (messageContext.Headers.TryGetValue(RetryConfirmationQueueHeaderKey, out var ackQueue))
            {
                messageContext.Headers[RetryConfirmationQueueHeaderKey] = "queue:" + ackQueue;
            }
        }
    }

    static MessageEnvelope DeserializeEnvelope(Transport.MessageContext messageContext)
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        options.Converters.Add(new InterfaceConverterFactory<BusHostInfo, HostInfo>());

        return JsonSerializer.Deserialize<MessageEnvelope>(messageContext.Body.Span, options)
            ?? throw new InvalidOperationException("Failed to deserialize MassTransit message envelope");
    }

    class InterfaceConverterFactory<TImplementation, TInterface> : JsonConverterFactory
    {
        public Type ImplementationType { get; }
        public Type InterfaceType { get; }

        public InterfaceConverterFactory()
        {
            ImplementationType = typeof(TImplementation);
            InterfaceType = typeof(TInterface);
        }

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == InterfaceType;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(InterfaceConverter<,>).MakeGenericType(ImplementationType, InterfaceType);
            return Activator.CreateInstance(converterType) as JsonConverter;
        }
    }

    class InterfaceConverter<TImplementation, TInterface> : JsonConverter<TInterface>
        where TImplementation : class, TInterface
    {
        public override TInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<TImplementation>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
        {
        }
    }

    // Local class for deserializing MassTransit message envelopes
    class MessageEnvelope
    {
        public string MessageId { get; set; }
        public string[] MessageType { get; set; }
        public DateTimeOffset? SentTime { get; set; }
        public string ConversationId { get; set; }
        public string CorrelationId { get; set; }
        public DateTimeOffset? ExpirationTime { get; set; }
        public string SourceAddress { get; set; }
        public HostInfo Host { get; set; }
    }
}
