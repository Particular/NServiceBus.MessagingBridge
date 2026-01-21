using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Transport;
using NUnit.Framework;
using NsbHeaders = NServiceBus.Headers;

public class MassTransitFormatAdapterTests
{
    [Test]
    public async Task Should_transform_incoming_plain_header_message()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "fault",
            [MessageHeaders.MessageId] = "mt-message-id-123",
            [MessageHeaders.MessageType] = "MyNamespace.MyMessage, MyAssembly",
            [MessageHeaders.TransportSentTime] = "2024-01-15T10:30:00Z",
            [MessageHeaders.ConversationId] = "conv-123",
            [MessageHeaders.CorrelationId] = "corr-456",
            [MessageHeaders.SourceAddress] = "rabbitmq://localhost/source-queue",
            [MessageHeaders.Host.Info] = "{\"MachineName\":\"test-machine\"}",
            [MessageHeaders.FaultRetryCount] = "3",
            [MessageHeaders.FaultInputAddress] = "rabbitmq://localhost/input-queue",
            [MessageHeaders.FaultExceptionType] = "System.InvalidOperationException",
            [MessageHeaders.FaultMessage] = "Test error message",
            [MessageHeaders.FaultStackTrace] = "at MyClass.MyMethod()",
            [MessageHeaders.FaultTimestamp] = "2024-01-15T10:35:00Z",
            [MessageHeaders.Host.MachineName] = "processing-machine"
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformIncoming(messageContext, CancellationToken.None);

        // Assert
        Assert.That(headers[NsbHeaders.ContentType], Is.EqualTo("application/json"));
        Assert.That(headers[NsbHeaders.MessageId], Is.EqualTo("mt-message-id-123"));
        Assert.That(headers[NsbHeaders.EnclosedMessageTypes], Is.EqualTo("MyNamespace.MyMessage, MyAssembly"));
        Assert.That(headers[NsbHeaders.ConversationId], Is.EqualTo("conv-123"));
        Assert.That(headers[NsbHeaders.CorrelationId], Is.EqualTo("corr-456"));
        Assert.That(headers[NsbHeaders.OriginatingEndpoint], Is.EqualTo("rabbitmq://localhost/source-queue"));
        Assert.That(headers[NsbHeaders.OriginatingMachine], Is.EqualTo("test-machine"));
        Assert.That(headers[NsbHeaders.DelayedRetries], Is.EqualTo("3"));
        Assert.That(headers[NsbHeaders.ProcessingEndpoint], Is.EqualTo("rabbitmq://localhost/input-queue"));
        Assert.That(headers[FaultsHeaderKeys.FailedQ], Is.EqualTo("rabbitmq://localhost/input-queue"));
        Assert.That(headers[FaultsHeaderKeys.ExceptionType], Is.EqualTo("System.InvalidOperationException"));
        Assert.That(headers[FaultsHeaderKeys.Message], Is.EqualTo("Test error message"));
        Assert.That(headers[FaultsHeaderKeys.StackTrace], Is.EqualTo("at MyClass.MyMethod()"));
        Assert.That(headers[NsbHeaders.ProcessingMachine], Is.EqualTo("processing-machine"));
    }

    [Test]
    public async Task Should_transform_incoming_enveloped_message()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();

        var envelope = new
        {
            messageId = "envelope-msg-id",
            messageType = new[] { "MyNamespace.MyMessage, MyAssembly" },
            sentTime = DateTimeOffset.Parse("2024-01-15T10:30:00Z"),
            conversationId = "env-conv-123",
            correlationId = "env-corr-456",
            expirationTime = DateTimeOffset.Parse("2024-01-15T11:30:00Z"),
            sourceAddress = "rabbitmq://localhost/envelope-source",
            host = new { machineName = "envelope-machine" }
        };

        var envelopeJson = JsonSerializer.Serialize(envelope);
        var envelopeBytes = Encoding.UTF8.GetBytes(envelopeJson);

        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "fault",
            // No MT-MessageId header indicates envelope format
            [MessageHeaders.FaultInputAddress] = "rabbitmq://localhost/fault-queue",
            [MessageHeaders.FaultMessage] = "Envelope error",
            [MessageHeaders.FaultTimestamp] = "2024-01-15T10:35:00Z"
        };

        var messageContext = CreateMessageContext(headers, envelopeBytes);

        // Act
        await adapter.TransformIncoming(messageContext, CancellationToken.None);

        // Assert
        Assert.That(headers[NsbHeaders.ContentType], Is.EqualTo("application/vnd.masstransit+json"));
        Assert.That(headers[NsbHeaders.MessageId], Is.EqualTo("envelope-msg-id"));
        Assert.That(headers[NsbHeaders.EnclosedMessageTypes], Is.EqualTo("MyNamespace.MyMessage, MyAssembly"));
        Assert.That(headers[NsbHeaders.ConversationId], Is.EqualTo("env-conv-123"));
        Assert.That(headers[NsbHeaders.CorrelationId], Is.EqualTo("env-corr-456"));
        Assert.That(headers[NsbHeaders.OriginatingEndpoint], Is.EqualTo("rabbitmq://localhost/envelope-source"));
        Assert.That(headers[NsbHeaders.OriginatingMachine], Is.EqualTo("envelope-machine"));
        Assert.That(headers[NsbHeaders.TimeToBeReceived], Does.Contain("2024-01-15"));
    }

    [Test]
    public async Task Should_use_fallback_values_when_optional_headers_missing()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "fault",
            [MessageHeaders.MessageId] = "msg-id",
            [MessageHeaders.MessageType] = "MyMessage",
            [MessageHeaders.ConversationId] = "conv",
            [MessageHeaders.SourceAddress] = "source",
            [MessageHeaders.Host.Info] = "{\"MachineName\":\"machine\"}",
            [MessageHeaders.FaultTimestamp] = "2024-01-15T10:35:00Z",
            [MessageHeaders.FaultMessage] = "Error"
        };

        var messageContext = CreateMessageContext(headers, receiveAddress: "fallback-queue");

        // Act
        await adapter.TransformIncoming(messageContext, CancellationToken.None);

        // Assert - should use receive address as fallback
        Assert.That(headers[NsbHeaders.ProcessingEndpoint], Is.EqualTo("fallback-queue"));
        Assert.That(headers[FaultsHeaderKeys.FailedQ], Is.EqualTo("fallback-queue"));
        Assert.That(headers[MessageHeaders.FaultInputAddress], Is.EqualTo("queue:fallback-queue"));
    }

    [Test]
    public void Should_throw_when_not_a_fault_message()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "not-a-fault",
            [MessageHeaders.MessageId] = "msg-id"
        };

        var messageContext = CreateMessageContext(headers);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.TransformIncoming(messageContext, CancellationToken.None));

        Assert.That(ex.Message, Does.Contain("Can only forward MassTransit failure messages"));
    }

    [Test]
    public void Should_throw_when_reason_header_missing()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.MessageId] = "msg-id"
        };

        var messageContext = CreateMessageContext(headers);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.TransformIncoming(messageContext, CancellationToken.None));

        Assert.That(ex.Message, Does.Contain("Can only forward MassTransit failure messages"));
    }

    [Test]
    public async Task Should_remove_nservicebus_headers_on_outgoing_transformation()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            ["NServiceBus.MessageId"] = "nsb-msg-id",
            ["NServiceBus.ConversationId"] = "nsb-conv",
            ["NServiceBus.CorrelationId"] = "nsb-corr",
            ["NServiceBus.EnclosedMessageTypes"] = "MyMessage",
            ["MT-Fault-ExceptionType"] = "Exception",
            ["MT-Reason"] = "fault",
            ["CustomHeader"] = "should-remain"
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformOutgoing(messageContext, "RabbitMQ", CancellationToken.None);

        // Assert
        Assert.That(headers.ContainsKey("NServiceBus.MessageId"), Is.False);
        Assert.That(headers.ContainsKey("NServiceBus.ConversationId"), Is.False);
        Assert.That(headers.ContainsKey("NServiceBus.CorrelationId"), Is.False);
        Assert.That(headers.ContainsKey("NServiceBus.EnclosedMessageTypes"), Is.False);
        Assert.That(headers.ContainsKey("MT-Fault-ExceptionType"), Is.False);
        Assert.That(headers.ContainsKey("MT-Reason"), Is.False);
        Assert.That(headers["CustomHeader"], Is.EqualTo("should-remain"));
    }

    [Test]
    public async Task Should_apply_rabbitmq_exchange_prefix_for_ack_queue()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            ["ServiceControl.Retry.AcknowledgementQueue"] = "ack-queue"
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformOutgoing(messageContext, "RabbitMQ", CancellationToken.None);

        // Assert
        Assert.That(headers["ServiceControl.Retry.AcknowledgementQueue"], Is.EqualTo("exchange:ack-queue"));
    }

    [Test]
    public async Task Should_apply_queue_prefix_for_azure_service_bus()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            ["ServiceControl.Retry.AcknowledgementQueue"] = "ack-queue"
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformOutgoing(messageContext, "AzureServiceBus", CancellationToken.None);

        // Assert
        Assert.That(headers["ServiceControl.Retry.AcknowledgementQueue"], Is.EqualTo("queue:ack-queue"));
    }

    [Test]
    public async Task Should_apply_queue_prefix_for_amazon_sqs()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            ["ServiceControl.Retry.AcknowledgementQueue"] = "ack-queue"
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformOutgoing(messageContext, "AmazonSQS", CancellationToken.None);

        // Assert
        Assert.That(headers["ServiceControl.Retry.AcknowledgementQueue"], Is.EqualTo("queue:ack-queue"));
    }

    [Test]
    public async Task Should_not_apply_prefix_when_ack_queue_header_missing()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            ["CustomHeader"] = "value"
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformOutgoing(messageContext, "RabbitMQ", CancellationToken.None);

        // Assert - should not throw, just skip the transformation
        Assert.That(headers.ContainsKey("ServiceControl.Retry.AcknowledgementQueue"), Is.False);
    }

    [Test]
    public void Should_have_correct_adapter_name()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();

        // Assert
        Assert.That(adapter.Name, Is.EqualTo("MassTransit"));
    }

    [Test]
    public async Task Should_handle_missing_correlation_id()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "fault",
            [MessageHeaders.MessageId] = "msg-id",
            [MessageHeaders.MessageType] = "MyMessage",
            [MessageHeaders.ConversationId] = "conv",
            [MessageHeaders.SourceAddress] = "source",
            [MessageHeaders.Host.Info] = "{\"MachineName\":\"machine\"}",
            [MessageHeaders.FaultInputAddress] = "fault-queue",
            [MessageHeaders.FaultMessage] = "Error",
            [MessageHeaders.FaultTimestamp] = "2024-01-15T10:35:00Z"
            // No CorrelationId
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformIncoming(messageContext, CancellationToken.None);

        // Assert - should not have CorrelationId
        Assert.That(headers.ContainsKey(NsbHeaders.CorrelationId), Is.False);
    }

    [Test]
    public async Task Should_handle_missing_fault_stack_trace()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "fault",
            [MessageHeaders.MessageId] = "msg-id",
            [MessageHeaders.MessageType] = "MyMessage",
            [MessageHeaders.ConversationId] = "conv",
            [MessageHeaders.SourceAddress] = "source",
            [MessageHeaders.Host.Info] = "{\"MachineName\":\"machine\"}",
            [MessageHeaders.FaultInputAddress] = "fault-queue",
            [MessageHeaders.FaultMessage] = "Error",
            [MessageHeaders.FaultTimestamp] = "2024-01-15T10:35:00Z"
            // No FaultStackTrace
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformIncoming(messageContext, CancellationToken.None);

        // Assert - should not throw
        Assert.That(headers.ContainsKey(FaultsHeaderKeys.StackTrace), Is.False);
    }

    [Test]
    public async Task Should_use_fault_timestamp_as_time_sent_fallback()
    {
        // Arrange
        var adapter = new MassTransitFormatAdapter();
        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.Reason] = "fault",
            [MessageHeaders.MessageId] = "msg-id",
            [MessageHeaders.MessageType] = "MyMessage",
            [MessageHeaders.ConversationId] = "conv",
            [MessageHeaders.SourceAddress] = "source",
            [MessageHeaders.Host.Info] = "{\"MachineName\":\"machine\"}",
            [MessageHeaders.FaultInputAddress] = "fault-queue",
            [MessageHeaders.FaultMessage] = "Error",
            [MessageHeaders.FaultTimestamp] = "2024-01-15T10:35:00Z"
            // No TransportSentTime
        };

        var messageContext = CreateMessageContext(headers);

        // Act
        await adapter.TransformIncoming(messageContext, CancellationToken.None);

        // Assert - should use FaultTimestamp as fallback
        Assert.That(headers[NsbHeaders.TimeSent], Does.Contain("2024-01-15"));
    }

    static NServiceBus.Transport.MessageContext CreateMessageContext(
        Dictionary<string, string> headers,
        byte[] body = null,
        string receiveAddress = "test-queue")
    {
        return new NServiceBus.Transport.MessageContext(
            "native-msg-id",
            headers,
            body ?? ReadOnlyMemory<byte>.Empty,
            new TransportTransaction(),
            receiveAddress,
            new NServiceBus.Extensibility.ContextBag());
    }
}
