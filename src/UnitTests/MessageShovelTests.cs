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
        var transferDetails = await Transfer("SendingEndpointReplyAddress@MyMachine");

        Assert.AreEqual("SendingEndpointReplyAddress", transferDetails.OutgoingOperation.Message.Headers[Headers.ReplyToAddress]);
    }

    [Test]
    public async Task Should_transform_failed_queue_header()
    {
        var transferDetails = await Transfer(failedQueueAddress: "error@MyMachine");

        Assert.AreEqual("error", transferDetails.OutgoingOperation.Message.Headers[FaultsHeaderKeys.FailedQ]);
    }

    [Test]
    public async Task Should_handle_send_only_endpoints()
    {
        //send only endpoints doesn't attach a reply to address
        var transferDetails = await Transfer();

        Assert.NotNull(transferDetails.OutgoingOperation);
    }

    [Test]
    public async Task Should_pass_transport_transaction_if_specified()
    {
        var transportTransaction = new TransportTransaction();

        var transferWithoutTransaction = await Transfer(
            transportTransaction: transportTransaction,
            passTransportTransaction: false);

        Assert.AreNotSame(transportTransaction, transferWithoutTransaction.TransportTransaction);

        var transferWithTransaction = await Transfer(
             transportTransaction: transportTransaction,
             passTransportTransaction: true);

        Assert.AreSame(transportTransaction, transferWithTransaction.TransportTransaction);
    }

    static async Task<TransferDetails> Transfer(
        string replyToAddress = null,
        string failedQueueAddress = null,
        TransportTransaction transportTransaction = null,
        bool passTransportTransaction = false)
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
            transportTransaction ?? new TransportTransaction(),
            "SourceEndpointAddress",
            new NServiceBus.Extensibility.ContextBag());

        var transferContext = new TransferContext(
            "SourceEndpoint",
            new QueueAddress("SourceEndpointAddress"),
            messageContext,
            passTransportTransaction);

        await shovel.TransferMessage(transferContext, CancellationToken.None);

        return targetEndpoint.TransferDetails;
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

        public TransferDetails TransferDetails { get; private set; }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            TransferDetails = new TransferDetails
            {
                OutgoingOperation = outgoingMessages.UnicastTransportOperations.Single(),
                TransportTransaction = transaction
            };

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

public class TransferDetails
{
    public UnicastTransportOperation OutgoingOperation { get; set; }
    public TransportTransaction TransportTransaction { get; set; }
}