using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Raw;
using NServiceBus.Transport;
using NUnit.Framework;

public class MessageShovelTests
{
    [Test]
    public async Task Should_transform_reply_to_address()
    {
        var transferedMessages = await Transfer("SendingEndpointReplyAddress@MyMachine");
        var transferedMessage = transferedMessages.Single();

        Assert.AreEqual("SendingEndpointReplyAddress", transferedMessage.Message.Headers[Headers.ReplyToAddress]);
    }

    [Test]
    public async Task Should_transform_failed_queue_header()
    {
        var transferedMessages = await Transfer(failedQueueAddress: "error@MyMachine");
        var transferedMessage = transferedMessages.Single();

        Assert.AreEqual("error", transferedMessage.Message.Headers[FaultsHeaderKeys.FailedQ]);
    }

    [Test]
    public async Task Should_handle_send_only_endpoints()
    {
        //send only endpoints doesn't attach a reply to address
        var transferedMessages = await Transfer(null);

        CollectionAssert.IsNotEmpty(transferedMessages);
    }

    static async Task<List<UnicastTransportOperation>> Transfer(
        string replyToAddress = null,
        string failedQueueAddress = null
        )
    {
        var logger = new NullLogger<MessageShovel>();
        var targetEndpoint = new FakeRawEndpoint("TargetEndpoint");
        var endpointProxyRegistry = new FakeTargetEndpointProxyRegistry(targetEndpoint);

        var headers = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(replyToAddress))
        {
            headers.Add(Headers.ReplyToAddress, replyToAddress);
        }

        if (!string.IsNullOrEmpty(failedQueueAddress))
        {
            headers.Add(FaultsHeaderKeys.FailedQ, failedQueueAddress);
        }

        var shovel = new MessageShovel(logger, endpointProxyRegistry);
        var messageContext = new MessageContext(
            "some-id",
            headers,
            ReadOnlyMemory<byte>.Empty,
            new TransportTransaction(),
            "SourceEndpointAddress",
            new NServiceBus.Extensibility.ContextBag());

        await shovel.TransferMessage("SourceEndpoint", new QueueAddress("SourceEndpointAddress"), messageContext, CancellationToken.None);

        return targetEndpoint.OutgoingMessages;
    }

    class FakeRawEndpoint : IStoppableRawEndpoint, IRawEndpoint
    {
        public FakeRawEndpoint(string endpointName)
        {
            EndpointName = endpointName;
        }

        public string TransportAddress => EndpointName;

        public string EndpointName { get; }

        public ISubscriptionManager SubscriptionManager => null;

        public List<UnicastTransportOperation> OutgoingMessages { get; private set; }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            OutgoingMessages = outgoingMessages.UnicastTransportOperations;

            return Task.CompletedTask;
        }

        public Task Stop(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public string ToTransportAddress(QueueAddress logicalAddress) => logicalAddress.ToString();
    }

    class FakeTargetEndpointProxyRegistry : ITargetEndpointProxyRegistry
    {
        public FakeTargetEndpointProxyRegistry(FakeRawEndpoint targetEndpoint)
        {
            this.targetEndpoint = targetEndpoint;
        }

        public IRawEndpoint GetTargetEndpointProxy(string sourceEndpointName)
        {
            return targetEndpoint;
        }

        readonly FakeRawEndpoint targetEndpoint;
    }
}

